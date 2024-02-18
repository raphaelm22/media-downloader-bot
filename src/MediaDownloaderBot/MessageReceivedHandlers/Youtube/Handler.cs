using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.ValueTasks;
using MediaDownloaderBot.Commons;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MediaDownloaderBot.MessageReceivedHandlers.Youtube
{
    internal sealed class Handler(
        ILogger<Handler> logger,
        YoutubeUrlParser youtubeUrlParser,
        IFileSystem fileSystem,
        IHttpClientFactory httpClientFactory
    ) : INotificationHandler<MessageReceived>
    {

        readonly ILogger<Handler> _logger = logger;
        readonly YoutubeUrlParser _youtubeUrlParser = youtubeUrlParser;
        readonly IFileSystem _fileSystem = fileSystem;
        readonly HttpClient _httpClient = httpClientFactory.CreateClient("youtube");

        public async Task Handle(MessageReceived notification, CancellationToken cancellationToken)
        {
            try
            {
                if (!_youtubeUrlParser.TryParse(notification.Message, out var videoId)) return;

                await notification.Reply.SendFindingVideoMessageAsync(cancellationToken);

                await GetVideoInfoAsync(videoId, cancellationToken)
                    .Bind(response => ChoeseFormat(response, notification.Reply))
                    .Bind(response => DownloadAsync(response, cancellationToken))
                    .Tap(videoPath => SendVideoAsync(videoPath, videoId, notification.Reply, cancellationToken))
                    .Tap(_fileSystem.SilenceDeleteFile)
                    .Match(OnSuccess, error => OnFailure(error, notification.Reply, cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while handling for message");
                await OnFailure("Try again later", notification.Reply, cancellationToken);
            }
        }

        async Task<Result<PlayerResponseModel>> GetVideoInfoAsync(string videoId, CancellationToken cancellationToken)
        {
            using var request = PlayerRequestModel.Create(videoId);
            var response = await _httpClient.PostAsync("/youtubei/v1/player", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseModel = await JsonSerializer.DeserializeAsync<PlayerResponseModel>(
                    await response.Content.ReadAsStreamAsync(cancellationToken),
                    PlayerResponseModel.GetJsonSerializerOptions(),
                    cancellationToken
                );
                return responseModel!;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<PlayerResponseModel>(
                $"{response.StatusCode}: {payload}");
        }

        Result<Uri> ChoeseFormat(PlayerResponseModel response, IReply reply)
        {
            var format = response.StreamingData.Formats
                .Where(format =>
                    format.ContentLength > 0 &&
                    format.ContentLength < reply.VideoMaxLenght
                )
                .OrderByDescending(format => format.Width * format.Height)
                .ThenBy(format => format.ContentLength)
                .First();

            return format.Url;
        }

        async Task<Result<string>> DownloadAsync(Uri videoUrl, CancellationToken cancellationToken)
        {
            var fileName = _fileSystem.CreateTempFile(".mp4");
            _logger.LogInformation("Starting download: {temp-file}", fileName);

            using var response = await _httpClient.GetAsync(videoUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            using var fileStream = new FileStream(fileName, FileMode.OpenOrCreate);
            await response.Content.CopyToAsync(fileStream, cancellationToken);

            return fileName;
        }

        async Task SendVideoAsync(string path, string videoId, IReply reply, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending video...");
            await reply.SendingVideoMessageAsync(cancellationToken);

            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            await reply.SendVideoAsync(fileStream, $"{videoId}.mp4", cancellationToken);
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
