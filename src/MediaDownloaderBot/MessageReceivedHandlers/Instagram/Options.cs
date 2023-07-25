namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal sealed class Options
    {
        public TimeSpan OpenPostTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
