namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal sealed class MediaHandlerFactory(RealsHandler realsHandler, StorieHandler storieHandler)
    {
        readonly RealsHandler _realsHandler = realsHandler;
        readonly StorieHandler _storieHandler = storieHandler;

        public IMediaHandler Create(InstagramUrl originUrl)
        {
            if (originUrl.IsStories)
                return _storieHandler;

            return _realsHandler;
        }
    }
}
