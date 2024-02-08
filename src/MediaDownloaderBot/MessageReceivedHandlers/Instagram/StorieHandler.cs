using CSharpFunctionalExtensions;
using MediaDownloaderBot.Puppeteer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal sealed class StorieHandler : IMediaHandler
    {
        public async Task<IPage> CreatePageAsync(IBrowser browser)
        {
            var page = await browser.NewPageAsync();
            return page;
        }

        public async Task NavigateOnPageAsync(IPage page)
        {
            await page.WaitForSelectorAsync("div[id]");
        }

       

        public async Task<Result<HttpRequestMessage?>> TryGetMediaResourceAsync(IResponse resourceResponse, InstagramUrl originUrl)
        {
            if (
                Uri.TryCreate(resourceResponse.Url, UriKind.Absolute, out var url) &&
                url.Segments.Last().Trim('/') == originUrl.Pk &&
                string.IsNullOrWhiteSpace(url.Query)
            )
            {
                var frame = ((Response)resourceResponse).Frame;

                await frame.WaitForSelectorAsync("div[id]");
                var scripts = await frame.EvaluateExpressionAsync<IEnumerable<string>>(
                    "[...document.querySelectorAll('script[type=\"application/json\"][data-sjs]')].map(s => s.innerText)"
                );

                var all = scripts.Select(script => GetMp4UrlFromStories(script, originUrl)).ToList();

                var videoUrlResults = all
                    .OrderByDescending(result => result.IsSuccess)
                    .FirstOrDefault();

                return videoUrlResults.Match(
                    onSuccess: videoUrl => Result.Success(videoUrl)
                        .Bind(url => CreateRequest(resourceResponse, url)),
                    onFailure: Result.Failure<HttpRequestMessage?>
                );
            }

            return Result.Success<HttpRequestMessage?>(null);
        }

        Result<string> GetMp4UrlFromStories(string content, InstagramUrl originUrl)
        {
            var jsonData = JObject.Parse(content);
            if (jsonData == null)
                return Result.Failure<string>("Error reading the API response.");

            var reelsPath = FindReelsMediaPath(jsonData);
            var itemJson = jsonData.SelectToken($"{reelsPath}[*].items");
            if (itemJson == null)
                return Result.Failure<string>("No video was found.");

            var items = itemJson.ToArray().Select(t => t.ToObject<ReelItemModel>() ?? new()).ToList();

            var item = items.FirstOrDefault(item => item.Pk == originUrl.Pk);

            if (item == null || item.Videos.Count == 0) return Result.Failure<string>("No video was found");

            var video = item.Videos
                .OrderByDescending(video => video.Type)
                .First();

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
                public int Type { get; set; }
                public string Url { get; set; } = "";
            }
        }


        static string? FindReelsMediaPath(JToken? token)
        {
            if (token == null) return null;

            var typeFilter = new[] { JTokenType.Array, JTokenType.Object, JTokenType.Property };
            if (!typeFilter.Contains(token.Type)) return null;

            if (token.Type == JTokenType.Object)
            {
                foreach (var child in token.Children())
                {
                    var path = FindReelsMediaPath(child);
                    if (path != null) return path;
                }
            }
            if (token.Type == JTokenType.Array)
            {
                foreach (var item in token)
                {
                    var path = FindReelsMediaPath(item);
                    if (path != null) return path;

                }
            }

            if (token.Type == JTokenType.Property)
            {
                if (token is JProperty jProp && jProp.Name == "reels_media")
                    return jProp.Path;

                return FindReelsMediaPath(token.First);
            }

            return null;
        }
    }

}
