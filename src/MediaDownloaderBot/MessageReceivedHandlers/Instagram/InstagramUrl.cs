namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal class InstagramUrl
    {
        public readonly Uri Url;

        public bool IsStories => Url.Segments.Length >= 4 && Url.Segments[1] == "stories/";
        public string? Pk => IsStories ? Url.Segments[3].TrimEnd('/') : null;

        public InstagramUrl(Uri url)
        {
            Url = url;
        }
    }
}
