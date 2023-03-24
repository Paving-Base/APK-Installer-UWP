using AdvancedSharpAdbClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace APKInstaller.Helpers
{
    public static class ADBHelper
    {
        private static string ADBPath => SettingsHelper.Get<string>(SettingsHelper.ADBPath);
        public static DeviceMonitor Monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdvancedAdbClient.AdbServerPort)));

        public static async Task StartADB()
        {
            AdbServer ADBServer = new AdbServer();
            await CommandHelper.ExecuteShellCommand("start-server", ADBPath);
            CancellationTokenSource TokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!ADBServer.GetStatus().IsRunning)
            {
                TokenSource.Token.ThrowIfCancellationRequested();
            }
            Monitor.Start();
        }

        public static async void StopADB() => await CommandHelper.ExecuteShellCommand("kill-server", ADBPath);

        public static bool CheckFileExists(string path)
        {
            return !string.IsNullOrWhiteSpace(path);
        }

        public static int RunProcess(string filename, string command, List<string> errorOutput, List<string> standardOutput)
        {
            ProcessStartInfo psi = new(filename, command)
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using Process process = Process.Start(psi);
            string standardErrorString = process.StandardError.ReadToEnd();
            string standardOutputString = process.StandardOutput.ReadToEnd();

            errorOutput?.AddRange(standardErrorString.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

            standardOutput?.AddRange(standardOutputString.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

            // get the return code from the process
            if (!process.WaitForExit(5000))
            {
                process.Kill();
            }

            return process.ExitCode;
        }

        public static TResult AwaitByTaskCompleteSource<TResult>(Func<Task<TResult>> func)
        {
            TaskCompletionSource<TResult> taskCompletionSource = new();
            Task<TResult> task1 = taskCompletionSource.Task;
            _ = Task.Run(async () =>
            {
                TResult result = await func.Invoke();
                taskCompletionSource.SetResult(result);
            });
            TResult task1Result = task1.Result;
            return task1Result;
        }
    }
}
