using MediaDownloaderBot.Commons;
using MediatR;

namespace MediaDownloaderBot.MessageReceivedHandlers.PingPong
{
    internal sealed class Handler : INotificationHandler<MessageReceived>
    {

        public async Task Handle(MessageReceived notification, CancellationToken cancellationToken)
        {
            if (notification.Message.Equals("ping", StringComparison.InvariantCultureIgnoreCase))
                await notification.Reply.SendMessageAsync("pong", cancellationToken);
            else if (notification.Message.Equals("hi", StringComparison.InvariantCultureIgnoreCase))
                await notification.Reply.SendMessageAsync("Hi! 😎", cancellationToken);
        }
    }
}
