
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaDownloaderBot.MessageReceivedHandlers.Youtube
{
    internal sealed class PlayerResponseModel
    {
        public required StreamingDataModel StreamingData { get; set; }

        public sealed class StreamingDataModel
        {
            public List<FormatModel> Formats { get; set; } = new(0);

            public sealed class FormatModel
            {
                public required Uri Url { get; set; }
                public int Width { get; set; }
                public int Height { get; set; }
                public long ContentLength { get; set; }
            }
        }

        public static JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
        }
    }
}
