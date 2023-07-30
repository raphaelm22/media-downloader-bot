namespace MediaDownloaderBot.MessageReceivedHandlers.Instagram
{
    internal sealed class Options
    {
        public TimeSpan OpenPostTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public bool HasAuthenticationData => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }
}
