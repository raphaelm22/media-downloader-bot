namespace MediaDownloaderBot.Commons
{
    internal interface IDaemon
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
