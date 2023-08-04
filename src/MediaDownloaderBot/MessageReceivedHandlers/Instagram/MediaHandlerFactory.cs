namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal sealed class MediaHandlerFactory
    {
        readonly RealsHandler _realsHandler;
        readonly StorieHandler _storieHandler;

        public MediaHandlerFactory(RealsHandler realsHandler, StorieHandler storieHandler)
        {
            _realsHandler = realsHandler;
            _storieHandler = storieHandler;
        }

        public IMediaHandler Create(InstagramUrl originUrl)
        {
            if (originUrl.IsStories)
                return _storieHandler;

            return _realsHandler;
        }
    }
}
