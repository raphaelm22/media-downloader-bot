namespace MediaDownloaderBot.Puppeteer
{
    internal sealed class Options
    {
        public string ExecutablePath { get; set; } = string.Empty;
        public string[] Args { get; set; } = Array.Empty<string>();
    }
}
