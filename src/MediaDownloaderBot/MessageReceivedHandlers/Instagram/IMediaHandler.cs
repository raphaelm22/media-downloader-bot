using CSharpFunctionalExtensions;
using PuppeteerSharp;

namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal interface IMediaHandler
    {
        Task NavigateOnPageAsync(IPage page);
        Task<Result<HttpRequestMessage?>> TryGetMediaResourceAsync(IResponse resourceResponse, InstagramUrl originUrl);
        Task<IPage> CreatePageAsync(IBrowser browser);
    }
}
