using MediaDownloaderBot;

using var tokenSource = new CancellationTokenSource();

AppDomain.CurrentDomain.ProcessExit += (s, e) => tokenSource.Cancel();

await Setup
    .CreateDaemon()
    .StartAsync(tokenSource.Token);

