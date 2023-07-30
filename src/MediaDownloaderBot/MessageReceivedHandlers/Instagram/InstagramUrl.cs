using CSharpFunctionalExtensions;
using PuppeteerSharp;

namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal class InstagramUrl
    {
        public readonly Uri Url;

        public InstagramUrl(Uri url)
        {
            Url = url;
        }

        public virtual async Task<IPage> CreatePageAsync(IBrowser browser)
        {
            var session = await browser.CreateIncognitoBrowserContextAsync();
            return await session.NewPageAsync();
        }

        public virtual Task<Result<string>?> TryGetMediaResource(IResponse resourceResponse)
        {
            if (!Uri.TryCreate(resourceResponse.Url, UriKind.Absolute, out var url))
                return Task.FromResult<Result<string>?>(null);

            if (!url.Segments.Last().EndsWith(".mp4"))
                return Task.FromResult<Result<string>?>(null);

            return Task.FromResult<Result<string>?>(resourceResponse.Url);
        }
    }
}
