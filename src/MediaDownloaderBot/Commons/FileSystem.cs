using Microsoft.Extensions.Logging;

namespace MediaDownloaderBot.Commons
{
    internal class FileSystem(ILogger<FileSystem> logger) : IFileSystem
    {
        readonly ILogger<FileSystem> _logger = logger;

        public void SilenceDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {path}", path);
            }
        }

        public void SilenceDeleteFile(IEnumerable<string> paths)
        {
            foreach (string path in paths)
                SilenceDeleteFile(path);
        }

        public string CreateTempFile(string extension)
        {
            return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():n}{extension}");
        }
    }
}
