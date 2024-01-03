using AdvancedSharpAdbClient;
using APKInstaller.Metadata;
using APKInstaller.Projection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;

namespace APKInstaller.Helpers
{
    public static class ADBHelper
    {
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

        public static Task DumpAsync(string filename, string command, Func<string, int, bool> callback, IList<string> output, int encode) =>
            APKInstallerProjectionFactory.ServerManager.DumpAsync(filename, command, new DumpDelegate(callback), output, encode).AsTask();

        public static async Task<bool> CheckFileExistsAsync(string path)
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
