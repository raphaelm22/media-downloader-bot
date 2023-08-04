using CSharpFunctionalExtensions;
using MediaDownloaderBot.Puppeteer;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using PuppeteerSharp;

namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal sealed class StorieHandler : IMediaHandler
    {
        public async Task<IPage> CreatePageAsync(IBrowser browser)
        {
            return await browser.NewPageAsync();
        }

        public async Task<Result<HttpRequestMessage?>> TryGetMediaResourceAsync(IResponse resourceResponse, InstagramUrl originUrl)
        {
            if (!resourceResponse.Url.Contains("instagram.com/api/v1/feed/reels_media/"))
                return Result.Success<HttpRequestMessage?>(null);

            var content = await resourceResponse.TextAsync();
            return GetMp4UrlFromStories(content, originUrl)
                .Bind(url => CreateRequest(resourceResponse, url));
        }

        Result<string> GetMp4UrlFromStories(string content, InstagramUrl originUrl)
        {
            var jsonData = JObject.Parse(content);
            if (jsonData == null)
                return Result.Failure<string>("Error reading the API response.");

            var itemJson = jsonData.SelectToken("$.reels.*.items");
            if (itemJson == null)
                return Result.Failure<string>("No video was found.");

            var items = itemJson.ToArray().Select(t => t.ToObject<ReelItemModel>() ?? new()).ToList();

            var item = items.FirstOrDefault(item => item.Pk == originUrl.Pk);

            if (item == null || !item.Videos.Any()) return Result.Failure<string>("No video was found");

            var video = item.Videos.OrderByDescending(video => video.Height * video.Width).First();

            return video.Url;
        }

        Result<HttpRequestMessage?> CreateRequest(IResponse resourceResponse, string url)
        {
            var request = resourceResponse.CreateHttpRequestMenssage();
            request.RequestUri = new Uri(url);

            return request;
        }

        sealed class ReelItemModel
        {
            public string Pk { get; set; } = "";

            [JsonProperty("video_versions")]
            public List<VideoVersionModel> Videos { get; set; } = new(0);

            internal sealed class VideoVersionModel
            {
                public int Height { get; set; }
                public int Width { get; set; }
                public string Url { get; set; } = "";
            }
        }
    }
}
