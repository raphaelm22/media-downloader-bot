using CSharpFunctionalExtensions;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal sealed class InstagramStorieUrl : InstagramUrl
    {

        public string Pk => Url.Segments[3].TrimEnd('/');

        public InstagramStorieUrl(Uri Url) : base(Url) { }


        public override async Task<Result<string>?> TryGetMediaResource(IResponse resourceResponse)
        {
            if (!resourceResponse.Url.Contains("instagram.com/api/v1/feed/reels_media/")) return null;

            var content = await resourceResponse.TextAsync();
            return GetMp4UrlFromStories(content);
        }

        Result<string> GetMp4UrlFromStories(string content)
        {
            var jsonData = JObject.Parse(content);
            if (jsonData == null)
                return Result.Failure<string>("Error reading the API response.");

            var itemJson = jsonData.SelectToken("$.reels.*.items");
            if (itemJson == null)
                return Result.Failure<string>("No video was found.");

            var items = itemJson.ToArray().Select(t => t.ToObject<ReelItemModel>() ?? new()).ToList();

            var item = items.FirstOrDefault(item => item.Pk == Pk);

            if (item == null || !item.Videos.Any()) return Result.Failure<string>("No video was found");

            var video = item.Videos.OrderByDescending(video => video.Height * video.Width).First();

            return video.Url;
        }

        public override Task<IPage> CreatePageAsync(IBrowser browser)
        {
            return browser.NewPageAsync();
        }

        public static bool IsStorie(Uri url)
        {
            return url.Segments.Length >= 4 && url.Segments[1] == "stories/";
        }
    }
}
