using Newtonsoft.Json;

namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{

    internal sealed class ReelItemModel
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
