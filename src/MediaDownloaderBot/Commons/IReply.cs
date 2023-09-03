namespace MediaDownloaderBot.Commons
{
    internal interface IReply
    {

        long VideoMaxLenght { get; }

        Task SendMessageAsync(string message, CancellationToken cancellationToken);

        Task SendVideoAsync(Stream stream, string fileName, CancellationToken cancellationToken);
    }
}
