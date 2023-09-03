using System.Diagnostics.CodeAnalysis;

namespace MediaDownloaderBot.MessageReceivedHandlers.Youtube
{
    internal sealed class YoutubeUrlParser
    {
        public bool TryParse(string youtubeUrl, [NotNullWhen(true)] out string? videoId)
        {
            videoId = null;

            if (!Uri.TryCreate(youtubeUrl, UriKind.Absolute, out var url)) return false;
            if (!url.Host.EndsWith("youtube.com")) return false;

            videoId = url.Segments switch
            {
                [var _, var part, var id] when part.Equals("shorts/") => id,
                _ => null
            };

            return videoId != null;
        }
    }
}
