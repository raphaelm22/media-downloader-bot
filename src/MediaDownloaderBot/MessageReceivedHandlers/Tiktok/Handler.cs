﻿using CSharpFunctionalExtensions;
using MediaDownloaderBot.Commons;
using MediaDownloaderBot.Puppeteer;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace MediaDownloaderBot.MessageReceivedHandlers.Tiktok
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
            IHttpClientFactory httpClientFactory)
        {
            _browserFactory = browserFactory;
            _options = options;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("tiktok");
            _fileSystem = fileSystem;
        }

        public async Task Handle(MessageReceived notification, CancellationToken cancellationToken)
        {
            try
            {
                if (!IsTiktokUrl(notification.Message)) return;

                await GetVideoRequestAsync(notification.Message, cancellationToken)
                    .Bind(request => DownloadAsync(request, cancellationToken))
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

        bool IsTiktokUrl(string message)
        {
            if (!Uri.TryCreate(message, UriKind.Absolute, out var url)) return false;
            return url.Host.EndsWith("tiktok.com");
        }

        async Task<Result<HttpRequestMessage>> GetVideoRequestAsync(string url, CancellationToken cancellationToken)
        {
            using var resetEvent = new AutoResetEvent(false);

            HttpRequestMessage? videoRequest = null;

            await using var browser = await _browserFactory.CreateAsync();
            using var page = await browser.NewPageAsync();
            await page.SetUserAgentAsync("Mozilla/5.0 (Linux; Android 10; SM-A205U) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.5790.136 Mobile Safari/537.36");
            page.Response += (sender, e) =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                if (!Uri.TryCreate(e.Response.Url, UriKind.Absolute, out var url)) return;

                var query = QueryHelpers.ParseQuery(url.Query);
                if (!query.TryGetValue("mime_type", out var mimeType) || mimeType != "video_mp4") return;
                if (!query.TryGetValue("qs", out var qs) || qs != "0") return;

                videoRequest = new HttpRequestMessage(HttpMethod.Get, e.Response.Request.Url);
                foreach (var header in e.Response.Request.Headers)
                    videoRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);

                resetEvent.Set();
            };

            _logger.LogInformation("Opening page: {url}", url);

            await page.GoToAsync(url);

            var timeoutOccurred = await Task.Run(() => resetEvent.WaitOne(_options.OpenPostTimeout), cancellationToken);

            return videoRequest != null
                ? Result.Success(videoRequest)
                : Result.Failure<HttpRequestMessage>(timeoutOccurred ? "Timeout to find the video, try again later" : "No video was found");
        }

        async Task<Result<string>> DownloadAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting download...");
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            var fileName = _fileSystem.CreateTempFile(".mp4");
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
