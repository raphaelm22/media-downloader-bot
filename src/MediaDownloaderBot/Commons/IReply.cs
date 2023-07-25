namespace MediaDownloaderBot.Commons
{
    internal interface IReply
    {
        Task SendMessageAsync(string message, CancellationToken cancellationToken);

        Task SendVideoAsync(Stream stream, string fileName, CancellationToken cancellationToken);
    }
}
