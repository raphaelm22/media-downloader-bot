namespace MediaDownloaderBot.Commons
{
    public interface IFileSystem
    {
        string CreateTempFile(string extension);
        void SilenceDeleteFile(string path);
    }
}
