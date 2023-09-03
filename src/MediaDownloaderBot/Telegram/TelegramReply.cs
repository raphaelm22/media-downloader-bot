using MediaDownloaderBot.Commons;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MediaDownloaderBot.Telegram
{
    internal class TelegramReply : IReply
    {

        readonly TelegramBotClient _telegramBotClient;
        readonly ChatId _chatId;
        readonly int _messageId;

        public TelegramReply(TelegramBotClient telegramBotClient, ChatId chatId, int messageId)
        {
            _telegramBotClient = telegramBotClient;
            _chatId = chatId;
            _messageId = messageId;
        }

        public long VideoMaxLenght => 50 * 1024 * 1024;

        public Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            return _telegramBotClient.SendTextMessageAsync(
                _chatId,
                message,
                replyToMessageId: _messageId,
                cancellationToken: cancellationToken
            );
        }

        public Task SendVideoAsync(Stream stream, string fileName, CancellationToken cancellationToken)
        {
            if (stream.Length > VideoMaxLenght)
                return _telegramBotClient.SendTextMessageAsync(
                    _chatId,
                    $"😢 Sorry, but the video is to long, the bot just can send video less than 50Mb, this video has {stream.Length / 1024 / 1024}Mb.",
                    replyToMessageId: _messageId,
                    cancellationToken: cancellationToken
                );

            var file = InputFile.FromStream(stream, fileName);
            return _telegramBotClient.SendVideoAsync(
                _chatId,
                file,
                replyToMessageId: _messageId,
                cancellationToken: cancellationToken
            );
        }
    }
}
