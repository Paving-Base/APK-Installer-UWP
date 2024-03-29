﻿using Downloader;

namespace APKInstaller.Helpers
{
    public static class DownloadHelper
    {
        public static DownloadConfiguration Configuration { get; }

        static DownloadHelper()
        {
            Configuration = new DownloadConfiguration
            {
                ChunkCount = 8,
                ParallelDownload = true
            };
        }
    }
}
