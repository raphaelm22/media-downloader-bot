﻿using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace MediaDownloaderBot.Puppeteer
{
    internal class PuppeteerBrowserFactory(Options options, ILogger<PuppeteerBrowserFactory> logger) : IPuppeteerBrowserFactory
    {
        readonly Options _options = options;
        readonly ILogger _logger = logger;

        public async Task<IBrowser> CreateAsync()
        {
            var launchOptions = new LaunchOptions()
            {
                UserDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "browser_userdata")
            };
#if DEBUG
            launchOptions.Headless = false;
#endif

            if (string.IsNullOrWhiteSpace(_options.ExecutablePath))
            {
                using var browserFetcher = new BrowserFetcher();

                _logger.LogInformation("Starting the download of Puppeteer Browser...");
                await browserFetcher.DownloadAsync();
                _logger.LogInformation("Download finished");
            }
            else
            {
                launchOptions.ExecutablePath = _options.ExecutablePath;
                launchOptions.Args = _options.Args;
            }

            _logger.LogInformation("Opening Browser");
            return await PuppeteerSharp.Puppeteer.LaunchAsync(launchOptions);
        }
    }
}
