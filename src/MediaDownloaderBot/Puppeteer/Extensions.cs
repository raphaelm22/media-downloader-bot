using PuppeteerSharp;

namespace MediaDownloaderBot.Puppeteer
{
    internal static class Extensions
    {
        public static HttpRequestMessage CreateHttpRequestMenssage(this IResponse response)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, response.Request.Url);
            foreach (var (key,value) in response.Request.Headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }

            return request;
        }
    }
}
