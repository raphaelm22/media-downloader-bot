using CSharpFunctionalExtensions;
using MediaDownloaderBot.Commons;
using MediaDownloaderBot.Puppeteer;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MediaDownloaderBot.MessageReceivedHandlers.TwitterVideoDownload
{
    internal sealed class Handler : INotificationHandler<MessageReceived>
    {

        readonly IPuppeteerBrowserFactory _browserFactory;
        readonly Options _options;
        readonly ILogger<Handler> _logger;
        readonly TweetUrlParser _tweetUrlParser;
        readonly IFileSystem _fileSystem;


        public Handler(IPuppeteerBrowserFactory browserFactory,
            Options options,
            ILogger<Handler> logger,
            TweetUrlParser tweetUrlParser,
            IFileSystem fileSystem
        )
        {
            _browserFactory = browserFactory;
            _options = options;
            _logger = logger;
            _tweetUrlParser = tweetUrlParser;
            _fileSystem = fileSystem;
        }

        public async Task Handle(MessageReceived notification, CancellationToken cancellationToken)
        {

            try
            {
                if (!_tweetUrlParser.TryParse(notification.Message, out var postId)) return;

                await notification.Reply.SendFindingVideoMessageAsync(cancellationToken);

                await TryGetPlaybackUrlAsync(postId, cancellationToken)
                    .Bind(mp4Url => DownloadAsync(mp4Url, cancellationToken))
                    .Tap(videoPath => SendVideoAsync(videoPath, postId, notification.Reply, cancellationToken))
                    .Tap(_fileSystem.SilenceDeleteFile)
                    .Match(OnSuccess, error => OnFailure(error, notification.Reply, cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while handling for message");
                await OnFailure("Try again later", notification.Reply, cancellationToken);
            }
        }

        async Task<Result<string>> TryGetPlaybackUrlAsync(string tweetId, CancellationToken cancellationToken)
        {
            string? playbackUrl = null;
            
            using var resetEvent = new AutoResetEvent(false);

            await using var browser = await _browserFactory.CreateAsync();
            var browserContext = await browser.CreateIncognitoBrowserContextAsync();
            using var page = await browserContext.NewPageAsync();
            page.Response += async (sender, e) =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                if (e.Response.Url.StartsWith("https://api.twitter.com/1.1/videos/tweet/config/"))
                {
                    try
                    {
                        var data = await e.Response.JsonAsync();
                        if (data != null)
                        {
                            var track = data.SelectToken("track");
                            if (track != null)
                            {
                                playbackUrl = track.Value<string>("playbackUrl");
                                resetEvent.Set();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogTrace("Error getting the playback {url} - {ex}", e.Response.Url, ex);
                    }
                }
            };

            _logger.LogInformation("Accessing post: {id}", tweetId);

            await page.GoToAsync($"https://twitter.com/i/videos/tweet/{tweetId}");

            var timeoutOccurred = await Task.Run(() => resetEvent.WaitOne(_options.OpenPostTimeout), cancellationToken);

            return playbackUrl != null
                ? Result.Success(playbackUrl)
                : Result.Failure<string>(timeoutOccurred ? "Timeout to find the video, try again later" : "No video was found");
        }

        async Task<Result<string>> DownloadAsync(string playbackUrl, CancellationToken cancellationToken)
        {
            var fileName = _fileSystem.CreateTempFile(".mp4");
            _logger.LogInformation("Starting download: {temp-file}", fileName);

            playbackUrl = playbackUrl.Replace("https", "http");

            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = _options.FFmpegPath,
                Arguments = $"-hide_banner -loglevel error -i {playbackUrl} -c copy -bsf:a aac_adtstoasc {fileName}"
            };

            var cmdProcess = new Process
            {
                StartInfo = startInfo
            };

            cmdProcess.Start();
            await cmdProcess.WaitForExitAsync(cancellationToken);

            return fileName;
        }


        async Task SendVideoAsync(string path, string postId, IReply reply, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending video...");

            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            await reply.SendVideoAsync(fileStream, $"{postId}.mp4", cancellationToken);
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
