using System.Diagnostics.CodeAnalysis;

namespace MediaDownloaderBot.MessageReceivedHandlers.TwitterVideoDownload
{
    internal class TweetUrlParser
    {
        public bool TryParse(string postUrl, [NotNullWhen(true)] out string? postId)
        {
            postId = null;

            if (!Uri.TryCreate(postUrl, UriKind.Absolute, out var url)) return false;
            if (url.Host != "twitter.com" && url.Host != "x.com") return false;

            var lastSegment = url.Segments.Last();

            if (!lastSegment.All(char.IsDigit)) return false;

            postId = lastSegment;

            return true;
        }
    }
}
