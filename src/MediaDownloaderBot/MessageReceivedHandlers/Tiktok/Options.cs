namespace MediaDownloaderBot.MessageReceivedHandlers.Tiktok
{
    internal sealed class Options
    {
        public TimeSpan OpenPostTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
