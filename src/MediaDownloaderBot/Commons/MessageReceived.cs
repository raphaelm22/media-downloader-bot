using MediatR;

namespace MediaDownloaderBot.Commons
{
    internal sealed record MessageReceived(string Message, IReply Reply) : INotification;
}
