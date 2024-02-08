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
            var page = await session.NewPageAsync();
            await page.SetExtraHttpHeadersAsync(new() { ["Accept-Language"] = "en-us" });

            return page;
        }

        public Task<Result<HttpRequestMessage?>> TryGetMediaResourceAsync(IResponse resourceResponse, InstagramUrl _)
        {
            if (!Uri.TryCreate(resourceResponse.Url, UriKind.Absolute, out var url))
                return Task.FromResult(Result.Success<HttpRequestMessage?>(null));

            if (!url.Segments.Last().EndsWith(".mp4"))
                return Task.FromResult(Result.Success<HttpRequestMessage?>(null));

            return Task.FromResult(Result.Success<HttpRequestMessage?>(resourceResponse.CreateHttpRequestMenssage()));
        }

        public async Task NavigateOnPageAsync(IPage page)
        {
            await page.WaitForSelectorAsync("article[role='presentation']");

            var forwardButtonSelector = "[role='presentation'] button[aria-label='Next']";
            IElementHandle? forwardButtonElement;
            do
            {
                forwardButtonElement = await page.QuerySelectorAsync(forwardButtonSelector);
                if (forwardButtonElement != null)
                    await forwardButtonElement.ClickAsync();

            } while (forwardButtonElement != null);
        }
    }
}
