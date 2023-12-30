using AdvancedSharpAdbClient;
using APKInstaller.Projection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
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

        public static bool CheckFileExists(string path)
        {
            try
            {
                return StorageFile.GetFileFromPathAsync(path).AwaitByTaskCompleteSource() is StorageFile file && file.IsOfType(StorageItemTypes.File);
            }
            catch
            {
                return false;
            }
        }

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

        public static int RunProcess(string filename, string command, List<string> errorOutput, List<string> standardOutput) =>
            (int)APKInstallerProjectionFactory.ServerManager.RunProcess(filename, command, errorOutput, standardOutput);

        public static Task<int> RunProcessAsync(string filename, string command, List<string> errorOutput, List<string> standardOutput, CancellationToken cancellationToken) =>
            APKInstallerProjectionFactory.ServerManager.RunProcessAsync(filename, command, errorOutput, standardOutput).AsTask(cancellationToken).ContinueWith(x => (int)x.Result);
    }
}
