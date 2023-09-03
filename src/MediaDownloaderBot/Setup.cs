using MediaDownloaderBot.Commons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MediaDownloaderBot
{
    internal static class Setup
    {
        public static IDaemon CreateDaemon()
        {
            var services = new ServiceCollection();
            Configure(services);

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IDaemon>();
        }

        static void Configure(ServiceCollection services)
        {
            var configurations = ConfigureOptions();

            services.AddOptions();
            AddLogging(services);
            services.AddMediatR(option => option.RegisterServicesFromAssembly(typeof(Setup).Assembly));

            AddCommons(services);
            AddPuppeteer(services, configurations);
            AddTelegram(services, configurations);
            AddTwitter(services, configurations);
            AddInstagram(services, configurations);
            AddTiktok(services, configurations);
            AddYoutube(services);
        }

        private static void AddLogging(ServiceCollection services)
        {
            services.AddLogging(options =>
            {
                options
                    .AddFilter(null, LogLevel.Warning)
                    .AddFilter("MediaDownloaderBot", LogLevel.Information)
                    .AddSimpleConsole(consoleOptions => consoleOptions.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ");
            });
        }

        private static void AddCommons(ServiceCollection services)
        {
            services.TryAddSingleton<IFileSystem, FileSystem>();
        }

        static IConfiguration ConfigureOptions()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.local.json", optional: false);

            return builder.Build();
        }

        static void AddTelegram(ServiceCollection services, IConfiguration configurations)
        {
            services.TryAddSingleton<IDaemon, Telegram.TelegramDaemon>();

            services.TryAddSingleton(configurations
                .GetSection("Telegram")
                .Get<Telegram.Options>()
                ?? throw new Exception("Could not create a Telegran Options")
            );
        }

        static void AddPuppeteer(ServiceCollection services, IConfiguration configurations)
        {
            services.TryAddSingleton<Puppeteer.IPuppeteerBrowserFactory, Puppeteer.PuppeteerBrowserFactory>();

            services.TryAddSingleton(configurations
                .GetSection("PuppeteerBrowser")
                .Get<Puppeteer.Options>() ?? new Puppeteer.Options()
            );
        }

        static void AddTwitter(ServiceCollection services, IConfiguration configurations)
        {
            services.TryAddSingleton<MessageReceivedHandlers.TwitterVideoDownload.TweetUrlParser>();

            services.TryAddSingleton(configurations
                .GetSection("Twitter")
                .Get<MessageReceivedHandlers.TwitterVideoDownload.Options>()
                ?? throw new Exception("Could not create a Twitter Options")
            );
        }

        static void AddInstagram(ServiceCollection services, IConfiguration configurations)
        {
            services.TryAddSingleton<MessageReceivedHandlers.Instagram.InstagramUrlParser>();
            services.TryAddSingleton<MessageReceivedHandlers.Instagram.MediaHandlerFactory>();
            services.TryAddSingleton<MessageReceivedHandlers.Instagram.RealsHandler>();
            services.TryAddSingleton<MessageReceivedHandlers.Instagram.StorieHandler>();

            services.AddHttpClient("instagram", client =>
            {
                client.BaseAddress = new Uri("https://instagram.com");
                client.DefaultRequestHeaders.TryAddWithoutValidation("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
            });

            services.TryAddSingleton(configurations
                .GetSection("Instagram")
                .Get<MessageReceivedHandlers.Instagram.Options>()
                ?? new()
            );
        }

        static void AddTiktok(ServiceCollection services, IConfiguration configurations)
        {
            services.AddHttpClient("tiktok", c => c.BaseAddress = new Uri("https://tiktok.com"));

            services.TryAddSingleton(configurations
                .GetSection("Tiktok")
                .Get<MessageReceivedHandlers.Tiktok.Options>()
                ?? new()
            );
        }

        static void AddYoutube(ServiceCollection services)
        {
            services.AddHttpClient("youtube", c => c.BaseAddress = new Uri("https://www.youtube.com"));

            services.TryAddScoped<MessageReceivedHandlers.Youtube.YoutubeUrlParser >();
        }

    }
}
