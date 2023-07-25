using Microsoft.Extensions.Logging;

namespace MediaDownloaderBot.Commons
{
    internal class FileSystem : IFileSystem
    {
        readonly ILogger<FileSystem> _logger;

        public FileSystem(ILogger<FileSystem> logger)
        {
            _logger = logger;
        }

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

        public string CreateTempFile(string extension)
        {
            return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():n}{extension}");
        }
    }
}
