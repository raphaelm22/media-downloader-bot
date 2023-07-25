using MediaDownloaderBot.Commons;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MediaDownloaderBot.Telegram
{
    internal class TelegramDaemon : IDaemon
    {
        readonly Options _options;
        readonly ILogger<TelegramDaemon> _logger;
        readonly IMediator _mediator;
        readonly TelegramBotClient _telegramBotClient;

        public TelegramDaemon(Options options, ILogger<TelegramDaemon> logger, IMediator mediator)
        {
            _options = options;
            _logger = logger;
            _mediator = mediator;

            _telegramBotClient = new TelegramBotClient(_options.Token);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var receiverOptions = new ReceiverOptions()
            {
                AllowedUpdates = new[] { UpdateType.Message }
            };
            _telegramBotClient.StartReceiving(HandleUpdateAsync, PollingErrorHandler, receiverOptions, cancellationToken);

            _logger.LogInformation("Daemon started");

            cancellationToken.WaitHandle.WaitOne();
            return Task.CompletedTask;
        }

        async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(update.Message?.Text)) return;

            _logger.LogInformation(
                "New message from {name} on {chatid}",
                update.Message.From?.Username ??
                update.Message.From?.FirstName ??
                update.Message.From?.Id.ToString(),
                update.Message.Chat.Id
            );

            var reply = new TelegramReply(_telegramBotClient, update.Message.Chat.Id, update.Message.MessageId);

            var messageReceived = new MessageReceived(update.Message.Text, reply);
            await _mediator.Publish(messageReceived, cancellationToken);
        }

        Task PollingErrorHandler(ITelegramBotClient bot, Exception ex, CancellationToken _cancellationToken)
        {
            _logger.LogError(ex, "Exception while polling for updates");
            return Task.CompletedTask;
        }
    }
}
