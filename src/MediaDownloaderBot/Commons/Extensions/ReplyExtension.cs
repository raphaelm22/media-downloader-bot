﻿namespace MediaDownloaderBot.Commons.Extensions
{
    internal static class ReplyExtension
    {
        public static Task SendFindingVideoMessageAsync(this IReply reply, CancellationToken cancellationToken)
        {
            return reply.SendMessageAsync("Alright! 🔎 Finding video...", cancellationToken);
        }
    }
}
