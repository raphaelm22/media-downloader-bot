namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal class InstagramUrl(Uri url)
    {
        public readonly Uri Url = url;

        public bool IsStories => Url.Segments.Length >= 4 && Url.Segments[1] == "stories/";
        public string? Pk => IsStories ? Url.Segments[3].TrimEnd('/') : null;
    }
}
