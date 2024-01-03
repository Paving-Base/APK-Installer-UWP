using System.Diagnostics;
using System.IO;
using Windows.Storage;

namespace APKInstaller.Helpers
{
    public static class CachesHelper
    {
        public static readonly string TempPathBase = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Caches");
        public static readonly string TempPath = Path.Combine(TempPathBase, $"{Process.GetCurrentProcess().Id}");
        public static readonly string OldTempPathBase = Path.Combine(Path.GetTempPath(), @"APKInstaller\Caches");
        public static readonly string OldTempPath = Path.Combine(OldTempPathBase, $"{Process.GetCurrentProcess().Id}");

        public static void CleanCaches(bool isAll)
        {
            if (isAll)
            {
                if (Directory.Exists(TempPathBase))
                {
                    try { Directory.Delete(TempPathBase, true); } catch { }
                }
            }
            else
            {
                if (Directory.Exists(TempPath))
                {
                    try { Directory.Delete(TempPath, true); } catch { }
                }
            }
        }

        public static void CleanOldCaches(bool isAll)
        {
            if (isAll)
            {
                if (Directory.Exists(OldTempPathBase))
                {
                    try { Directory.Delete(OldTempPathBase, true); } catch { }
                }
            }
            else
            {
                if (Directory.Exists(OldTempPath))
                {
                    try { Directory.Delete(OldTempPath, true); } catch { }
                }
            }
        }

        public static void CleanAllCaches(bool isAll)
        {
            CleanCaches(isAll);
            CleanOldCaches(isAll);
        }
    }
}
