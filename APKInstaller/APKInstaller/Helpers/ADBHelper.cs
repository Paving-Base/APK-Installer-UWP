using AdvancedSharpAdbClient;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace APKInstaller.Helpers
{
    internal static class ADBHelper
    {
        private static string ADBPath => SettingsHelper.Get<string>(SettingsHelper.ADBPath);
        public static DeviceMonitor Monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdvancedAdbClient.AdbServerPort)));

        public static async Task StartADB()
        {
            AdbServer ADBServer = new AdbServer();
            await CommandHelper.ExecuteShellCommand("start-server", ADBPath);
            CancellationTokenSource TokenSource = new CancellationTokenSource(new TimeSpan(10));
            while (!ADBServer.GetStatus().IsRunning)
            {
                TokenSource.Token.ThrowIfCancellationRequested();
            }
            Monitor.Start();
        }

        public static async void StopADB() => await CommandHelper.ExecuteShellCommand("kill-server", ADBPath);
    }
}
