using AdvancedSharpAdbClient;
using ProcessForUWP.UWP;
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
        private static DeviceMonitor _monitor;
        public static DeviceMonitor Monitor
        {
            get
            {
                if (_monitor == null && AdbServer.Instance.GetStatus().IsRunning)
                {
                    _monitor = new(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
                    _monitor.Start();
                }
                return _monitor;
            }
        }

        public static bool CheckFileExists(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && FileEx.Exists(path);
        }

        public static int RunProcess(string filename, string command, List<string> errorOutput, List<string> standardOutput)
        {
            int code = 1;

            ProcessStartInfo psi = new(filename, command)
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (ProcessEx process = ProcessEx.Start(psi))
            {
                CancellationTokenSource Token = new();

                process.BeginOutputReadLine();

                process.EnableRaisingEvents = true;

                void OnOutputDataReceived(object sender, DataReceivedEventArgsEx e)
                {
                    if (e.Data == null)
                    {
                        code = 0;
                        Token.Cancel();
                        return;
                    }
                    string line = e.Data ?? string.Empty;

                    standardOutput?.Add(line);
                }

                void ErrorDataReceived(object sender, DataReceivedEventArgsEx e)
                {
                    string line = e.Data ?? string.Empty;

                    errorOutput?.Add(line);
                }

                try
                {
                    process.OutputDataReceived += OnOutputDataReceived;
                    process.ErrorDataReceived += ErrorDataReceived;
                    while (!process.IsExited)
                    {
                        Token.Token.ThrowIfCancellationRequested();
                    }
                }
                catch (Exception)
                {
                    process.Kill();
                }
                finally
                {
                    process.Close();
                    process.OutputDataReceived -= OnOutputDataReceived;
                }
            }

            return code;
        }

        public static TResult AwaitByTaskCompleteSource<TResult>(Func<Task<TResult>> function)
        {
            TaskCompletionSource<TResult> taskCompletionSource = new();
            Task<TResult> task = taskCompletionSource.Task;
            _ = Task.Run(async () =>
            {
                try
                {
                    TResult result = await function.Invoke().ConfigureAwait(false);
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            });
            TResult taskResult = task.Result;
            return taskResult;
        }
    }
}
