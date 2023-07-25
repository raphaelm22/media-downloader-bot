using CSharpFunctionalExtensions;
using MediaDownloaderBot.Commons;
using MediaDownloaderBot.Puppeteer;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal sealed class Handler : INotificationHandler<MessageReceived>
    {

        readonly IPuppeteerBrowserFactory _browserFactory;
        readonly Options _options;
        readonly ILogger<Handler> _logger;
        readonly HttpClient _httpClient;
        readonly IFileSystem _fileSystem;

        public Handler(IPuppeteerBrowserFactory browserFactory,
            Options options,
            ILogger<Handler> logger,
            IFileSystem fileSystem, 
            IHttpClientFactory httpClientFactory
        )
        {
            _browserFactory = browserFactory;
            _options = options;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("instagram");
            _fileSystem = fileSystem;
        }

        public async Task Handle(MessageReceived notification, CancellationToken cancellationToken)
        {
            try
            {
                if (!IsIntagramUrl(notification.Message)) return;

                await TryGetMp4UrlAsync(notification.Message, cancellationToken)
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

        bool IsIntagramUrl(string message)
        {
            if (!Uri.TryCreate(message, UriKind.Absolute, out var url)) return false;
            return url.Host.EndsWith("instagram.com");
        }

        async Task<Result<string>> TryGetMp4UrlAsync(string url, CancellationToken cancellationToken)
        {
            string? mp4Url = null;

            using var resetEvent = new AutoResetEvent(false);

            await using var browser = await _browserFactory.CreateAsync();
            var browserContext = await browser.CreateIncognitoBrowserContextAsync();
            using var page = await browserContext.NewPageAsync();
            page.Response += (sender, e) =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                if (!Uri.TryCreate(e.Response.Url, UriKind.Absolute, out var url)) return;
                if (!url.Segments.Last().EndsWith(".mp4")) return;

                mp4Url = e.Response.Url;
                resetEvent.Set();
            };

            _logger.LogInformation("Accessing post: {url}", url);

            await page.GoToAsync(url);

            var timeoutOccurred = await Task.Run(() => resetEvent.WaitOne(_options.OpenPostTimeout), cancellationToken);

            return mp4Url != null
                ? Result.Success(mp4Url)
                : Result.Failure<string>(timeoutOccurred ? "Timeout to find the video, try again later" : "No video was found");
        }

        async Task<Result<string>> DownloadAsync(string url, CancellationToken cancellationToken)
        {
            var fileName = _fileSystem.CreateTempFile(".mp4");
            _logger.LogInformation("Starting download: {temp-file}", fileName);

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            using var fileStream = new FileStream(fileName, FileMode.OpenOrCreate);
            await response.Content.CopyToAsync(fileStream, cancellationToken);

            return fileName;
        }

        async Task SendVideoAsync(string path, IReply reply, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending video...");

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
