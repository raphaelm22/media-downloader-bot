using System.Diagnostics.CodeAnalysis;

namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal sealed class InstagramUrlParser
    {
        public bool TryParse(string postUrl, [NotNullWhen(true)] out InstagramUrl? instagramUrl)
        {
            instagramUrl = null;

            if (!Uri.TryCreate(postUrl, UriKind.Absolute, out var url)) return false;
            if (!url.Host.EndsWith("instagram.com")) return false;

            instagramUrl = new(url);
            return true;
        }
    }
}
