using CSharpFunctionalExtensions;
using MediaDownloaderBot.Puppeteer;
using PuppeteerSharp;

namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{

    internal sealed class RealsHandler : IMediaHandler
    {
        public async Task<IPage> CreatePageAsync(IBrowser browser)
        {
            var session = await browser.CreateIncognitoBrowserContextAsync();
            return await session.NewPageAsync();
        }

        public Task<Result<HttpRequestMessage?>> TryGetMediaResourceAsync(IResponse resourceResponse, InstagramUrl _)
        {
            if (!Uri.TryCreate(resourceResponse.Url, UriKind.Absolute, out var url))
                return Task.FromResult(Result.Success<HttpRequestMessage?>(null));

            if (!url.Segments.Last().EndsWith(".mp4"))
                return Task.FromResult(Result.Success<HttpRequestMessage?>(null));

            return Task.FromResult(Result.Success<HttpRequestMessage?>(resourceResponse.CreateHttpRequestMenssage()));
        }
    }
}
