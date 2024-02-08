namespace MediaDownloaderBot.Commons.Extensions
{
    internal static class ReplyExtension
    {
        public static Task SendFindingVideoMessageAsync(this IReply reply, CancellationToken cancellationToken)
        {
            return reply.SendMessageAsync("🔎 Alright! Finding video...", cancellationToken);
        }

        public static Task SendingVideoMessageAsync(this IReply reply, CancellationToken cancellationToken)
        {
            return reply.SendMessageAsync("📤 Wait! Sending...", cancellationToken);
        }

        public static Task SendVideoCountMessageAsync(this IReply reply, int count, CancellationToken cancellationToken)
        {
            return reply.SendMessageAsync($" {count} videos were found", cancellationToken);
        }
    }
}
