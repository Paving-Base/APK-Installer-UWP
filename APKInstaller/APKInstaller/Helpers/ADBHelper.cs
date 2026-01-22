using AdvancedSharpAdbClient;
using System;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;

namespace APKInstaller.Helpers
{
    public static class ADBHelper
    {
        public static bool IsRunning { get; private set; }

        private static DeviceMonitor _monitor;
        public static DeviceMonitor Monitor
        {
            get
            {
                if (_monitor == null && AdbServer.Instance.GetStatus().IsRunning)
                {
                    _monitor = new(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
                    _ = _monitor.StartAsync();
                }
                return _monitor;
            }
        }

        public static Task<bool> CheckIsRunningAsync() => AdbServer.Instance.GetStatusAsync(default).ContinueWith(x => IsRunning = x.Result.IsRunning);

        public static async ValueTask StartADBAsync(string path)
        {
            await AdbServer.Instance.StartServerAsync(path, restartServerIfNewer: false, default);
            IsRunning = true;
        }

        public static async ValueTask<bool> CheckFileExistsAsync(string path)
        {
            try
            {
                return await StorageFile.GetFileFromPathAsync(path) is StorageFile file && file.IsOfType(StorageItemTypes.File);
            }
            catch
            {
                return false;
            }
        }
    }
}
