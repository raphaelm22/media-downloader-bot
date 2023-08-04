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

                await TryGetMp4UrlAsync(browser, instagramUrl, cancellationToken)
                    .OnFailureCompensate(error =>
                        error == RequiresAuthenticationError && _options.HasAuthenticationData
                            ? DoLoginAsync(browser, notification.Reply, cancellationToken)
                                .Bind(_ => TryGetMp4UrlAsync(browser, instagramUrl, cancellationToken))
                            : Task.FromResult(Result.Failure<HttpRequestMessage>(error))
                        )
                    .Bind(mp4Url => DownloadAsync(mp4Url, cancellationToken))
                    .Tap(videoPath => SendVideoAsync(videoPath, notification.Reply, cancellationToken))
                    .Tap(_fileSystem.SilenceDeleteFile)
                    .Match(OnSuccess, error => OnFailure(error, notification.Reply, cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while handling for message");
                await OnFailure("Try again later", notification.Reply, cancellationToken);
            }
        }

        async Task<Result<HttpRequestMessage>> TryGetMp4UrlAsync(IBrowser browser, InstagramUrl instagramUrl, CancellationToken cancellationToken)
        {
            using var resetEvent = new AutoResetEvent(false);

            Result<HttpRequestMessage>? requestVideo = null;
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
                        {
                            requestVideo = resource;
                            resetEvent.Set();
                        }
                    },
                    onFailure: error => Result.Failure<HttpRequestMessage>(error)
                );
            };

            _logger.LogInformation("Accessing post: {url}", instagramUrl.Url.ToString());

            await page.GoToAsync(instagramUrl.Url.ToString(), WaitUntilNavigation.DOMContentLoaded);
            if (await RequiresAuthenticationAsync(page))
            {
                _logger.LogInformation(RequiresAuthenticationError);
                return Result.Failure<HttpRequestMessage>(RequiresAuthenticationError);
            }

            var timeoutOccurred = await Task.Run(() => resetEvent.WaitOne(_options.OpenPostTimeout), cancellationToken);

            return requestVideo ?? Result.Failure<HttpRequestMessage>(timeoutOccurred
                ? "Timeout to find the video, try again later"
                : "No video was found"
            );
        }

        async Task<bool> RequiresAuthenticationAsync(IPage page)
        {
            var goBackToInstagramElement = await page.QuerySelectorAsync("[role='main'] a[tabindex='0'][href='/']");
            if (goBackToInstagramElement != null) return true;

            var inputPasswordElement = await page.QuerySelectorAsync("input[type=password]");
            if (inputPasswordElement != null) return true;

            return false;
        }

        async Task<Result<string>> DoLoginAsync(IBrowser browser, IReply reply, CancellationToken cancellation)
        {
            _logger.LogInformation("Logging in...");
            await reply.SendMessageAsync("It requires authentication, logging in...", cancellation);

            await using var page = await browser.NewPageAsync();
            await page.GoToAsync("https://www.instagram.com/accounts/login/", WaitUntilNavigation.DOMContentLoaded);

            var usernameInputSelector = "input[name='username']";
            await page.WaitForSelectorAsync(usernameInputSelector);

            await page.TypeAsync(usernameInputSelector, _options.Username);
            await page.TypeAsync("input[name='password']", _options.Password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForNavigationAsync(new() { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } });

            return "Success!";
        }

        async Task<Result<string>> DownloadAsync(HttpRequestMessage req, CancellationToken cancellationToken)
        {
            var fileName = _fileSystem.CreateTempFile(".mp4");
            _logger.LogInformation("Starting download: {temp-file}", fileName);

            using var response = await _httpClient.SendAsync(req, cancellationToken);
            using var fileStream = new FileStream(fileName, FileMode.OpenOrCreate);
            await response.Content.CopyToAsync(fileStream, cancellationToken);

            return fileName;
        }

        async Task SendVideoAsync(string path, IReply reply, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending video...");
            await reply.SendingVideoMessageAsync(cancellationToken);

            var fileInfo = new FileInfo(path);
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            await reply.SendVideoAsync(fileStream, fileInfo.Name, cancellationToken);
        }

        private async Task OnFailure(string message, IReply reply, CancellationToken cancellationToken)
        {
            _logger.LogWarning("Sending failed: '{message}'", message);
            await reply.SendMessageAsync(message, cancellationToken);
        }

        private Task OnSuccess(string arg)
        {
            _logger.LogInformation("Video sent");
            return Task.CompletedTask;
        }
    }
}
