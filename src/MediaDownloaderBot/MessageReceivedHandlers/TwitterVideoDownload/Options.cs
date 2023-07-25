namespace MediaDownloaderBot.MessageReceivedHandlers.TwitterVideoDownload
{
    internal sealed class Options
    {
        public string FFmpegPath { get; set; } = "";
        public TimeSpan OpenPostTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
