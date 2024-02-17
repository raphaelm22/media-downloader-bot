using CSharpFunctionalExtensions;
using MediaDownloaderBot.Commons;
using MediaDownloaderBot.Puppeteer;
using MediatR;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal sealed class Handler : INotificationHandler<MessageReceived>
    {

        const string RequiresAuthenticationError = "Requires Authentication";

        readonly IPuppeteerBrowserFactory _browserFactory;
        readonly Options _options;
        readonly ILogger<Handler> _logger;
        readonly HttpClient _httpClient;
        readonly IFileSystem _fileSystem;
        readonly MediaHandlerFactory _mediaHandlerFactory;
        readonly InstagramUrlParser _instagramUrlParser;

        public Handler(IPuppeteerBrowserFactory browserFactory,
            Options options,
            ILogger<Handler> logger,
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            InstagramUrlParser instagramUrlParser,
            MediaHandlerFactory mediaHandlerFactory)
        {
            _browserFactory = browserFactory;
            _options = options;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("instagram");
            _fileSystem = fileSystem;
            _instagramUrlParser = instagramUrlParser;
            _mediaHandlerFactory = mediaHandlerFactory;
        }

        public async Task Handle(MessageReceived notification, CancellationToken cancellationToken)
        {
            try
            {
                if (!_instagramUrlParser.TryParse(notification.Message, out var instagramUrl)) return;

                await notification.Reply.SendFindingVideoMessageAsync(cancellationToken);

                await using var browser = await _browserFactory.CreateAsync();

                await TryGetMp4UrlAsync(browser, instagramUrl, notification.Reply, cancellationToken)
                    .OnFailureCompensate(error =>
                        error == RequiresAuthenticationError && _options.HasAuthenticationData
                            ? DoLoginAsync(browser, notification.Reply, cancellationToken)
                                .Bind(_ => TryGetMp4UrlAsync(browser, instagramUrl, notification.Reply, cancellationToken))
                            : Task.FromResult(Result.Failure<IEnumerable<HttpRequestMessage>>(error))
                        )
                    .Bind(mp4Url => DownloadAsync(mp4Url, cancellationToken))
                    .Tap(videoPath => SendVideosAsync(videoPath, notification.Reply, cancellationToken))
                    .Tap(_fileSystem.SilenceDeleteFile)
                    .Match(OnSuccess, error => OnFailure(error, notification.Reply, cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while handling for message");
                await OnFailure("Try again later", notification.Reply, cancellationToken);
            }
        }

        async Task<Result<IEnumerable<HttpRequestMessage>>> TryGetMp4UrlAsync(IBrowser browser, InstagramUrl instagramUrl, IReply reply, CancellationToken cancellationToken)
        {
            var requestVideos = new List<HttpRequestMessage>();
            var mediaHandler = _mediaHandlerFactory.Create(instagramUrl);

            await using var page = await mediaHandler.CreatePageAsync(browser);
            page.Response += async (sender, e) =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                var mediaResource = await mediaHandler.TryGetMediaResourceAsync(e.Response, instagramUrl);
                mediaResource.Match(
                    onSuccess: resource =>
                    {
                        if (resource != null)
                            requestVideos.Add(resource);
                    },
                    onFailure: error => Result.Failure<HttpRequestMessage>(error)
                );
            };

            _logger.LogInformation("Accessing post: {url}", instagramUrl.Url.ToString());

            await page.GoToAsync(instagramUrl.Url.ToString(), WaitUntilNavigation.DOMContentLoaded);

            var timeoutOccurred = false;
            try
            {
                await mediaHandler.NavigateOnPageAsync(page);
                await page.WaitForNetworkIdleAsync(new () { Timeout = (int)_options.OpenPostTimeout.TotalMilliseconds });
            }
            catch (WaitTaskTimeoutException)
            {
                timeoutOccurred = true;
            }
            catch (TimeoutException)
            {
                timeoutOccurred = true;
            }

            if (requestVideos.Count == 0)
            {
                if (await RequiresAuthenticationAsync(page))
                {
                    _logger.LogInformation(RequiresAuthenticationError);
                    return Result.Failure<IEnumerable<HttpRequestMessage>>(RequiresAuthenticationError);
                }

                await reply.SendScreenshotMessageAsync(page, cancellationToken);

                return Result.Failure<IEnumerable<HttpRequestMessage>>(
                    timeoutOccurred
                        ? "Timeout to find the video. Try again later"
                        : "No video was found"
                    );
            }

            return requestVideos;
        }

        async Task<bool> RequiresAuthenticationAsync(IPage page)
        {
            var url = new Uri(page.Url);
            if (url.AbsolutePath == "/accounts/login/") return true;

            try
            {
                var requiresAuthenticationEvidencies = new[] {
                    "input[type='password']", 
                    "a[href^='/accounts/login']" 
                };

                var result = await Task.WhenAny(
                    requiresAuthenticationEvidencies.Select(selector =>
                        page.WaitForSelectorAsync(selector, new () { Timeout = 10 * 1000 })
                    )
                );

                return result.Status != TaskStatus.Faulted;
            }
            catch (WaitTaskTimeoutException)
            {
                return false;
            }
        }

        async Task<Result<string>> DoLoginAsync(IBrowser browser, IReply reply, CancellationToken cancellation)
        {
            _logger.LogInformation("Authenticating...");
            await reply.SendMessageAsync("🔐 It requires authentication. Authenticating...", cancellation);

            await using var page = await browser.NewPageAsync();
            await page.GoToAsync("https://www.instagram.com/accounts/login/", WaitUntilNavigation.DOMContentLoaded);

            var usernameInputSelector = "input[name='username']";
            await page.WaitForSelectorAsync(usernameInputSelector);

            await page.TypeAsync(usernameInputSelector, _options.Username);
            await page.TypeAsync("input[name='password']", _options.Password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForNavigationAsync(new() { WaitUntil = [WaitUntilNavigation.DOMContentLoaded] });

            return "Success!";
        }

        async Task<Result<IEnumerable<string>>> DownloadAsync(IEnumerable<HttpRequestMessage> requests, CancellationToken cancellationToken)
        {
            var filePaths = new List<string>();

            foreach (var request in requests)
            {
                var fileName = _fileSystem.CreateTempFile(".mp4");
                _logger.LogInformation("Starting download: {temp-file}", fileName);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                using var fileStream = new FileStream(fileName, FileMode.OpenOrCreate);
                await response.Content.CopyToAsync(fileStream, cancellationToken);

                filePaths.Add(fileName);
            }

            return filePaths;
        }

        async Task SendVideosAsync(IEnumerable<string> filePaths, IReply reply, CancellationToken cancellationToken)
        {
            if (filePaths.Count() > 1)
                await reply.SendVideoCountMessageAsync(filePaths.Count(), cancellationToken);

            foreach (string path in filePaths)
            {
                _logger.LogInformation("Sending video...");
                await reply.SendingVideoMessageAsync(cancellationToken);

                var fileInfo = new FileInfo(path);
                using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                await reply.SendVideoAsync(fileStream, fileInfo.Name, cancellationToken);
            }
        }

        private async Task OnFailure(string message, IReply reply, CancellationToken cancellationToken)
        {
            _logger.LogWarning("Sending failed: '{message}'", message);
            await reply.SendMessageAsync(message, cancellationToken);
        }

        private Task OnSuccess(IEnumerable<string> videos)
        {
            _logger.LogInformation("{count} videos were sent", videos.Count());
            return Task.CompletedTask;
        }
    }
}
