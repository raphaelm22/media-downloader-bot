using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace MediaDownloaderBot.Puppeteer
{
    internal class PuppeteerBrowserFactory : IPuppeteerBrowserFactory
    {
        readonly Options _options;
        readonly ILogger _logger;


        public PuppeteerBrowserFactory(Options options, ILogger<PuppeteerBrowserFactory> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<IBrowser> CreateAsync()
        {
            LaunchOptions launchOptions;
            if (string.IsNullOrWhiteSpace(_options.ExecutablePath))
            {
                using var browserFetcher = new BrowserFetcher();

                _logger.LogInformation("Starting the download of Puppeteer Browser...");
                await browserFetcher.DownloadAsync();
                _logger.LogInformation("Download finished");

                launchOptions = new();
            }
            else
            {
                launchOptions = new()
                {
                    ExecutablePath = _options.ExecutablePath,
                    Args = _options.Args
                };
            }

            _logger.LogInformation("Opening Browser");
            return await PuppeteerSharp.Puppeteer.LaunchAsync(launchOptions);
        }
    }
}
