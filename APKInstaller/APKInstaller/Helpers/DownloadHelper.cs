using Downloader;

namespace APKInstaller.Helpers
{
    public static class DownloadHelper
    {
        public static DownloadConfiguration Configuration { get; } = new DownloadConfiguration
        {
            CheckDiskSizeBeforeDownload = false,
            ChunkCount = 8,
            ParallelDownload = true
        };
    }
}
