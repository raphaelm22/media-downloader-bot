using PuppeteerSharp;

namespace MediaDownloaderBot.Puppeteer
{
    internal interface IPuppeteerBrowserFactory
    {
        Task<IBrowser> CreateAsync();
    }
}