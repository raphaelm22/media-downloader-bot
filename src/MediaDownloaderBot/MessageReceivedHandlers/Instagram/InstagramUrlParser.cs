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

            if (InstagramStorieUrl.IsStorie(url))
            {
                instagramUrl = new InstagramStorieUrl(url);
                return true;
            }
            instagramUrl = new(url);
            return true;
        }
    }
}
