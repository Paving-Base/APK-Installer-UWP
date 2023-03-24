using ProcessForUWP.UWP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace APKInstaller.Helpers
{
    internal class CommandHelper
    {
        /// <summary>
        /// Executes a shell command on the remote device
        /// </summary>
        /// </param>
        /// <param name="command">The command to execute</param>
        /// <param name="rcvr">The shell output receiver</param>
        public static async Task<List<string>> ExecuteShellCommand(string command, string filename = "powershell.exe")
        {
            try
            {
                return await ExecuteShellCommandAsync(filename, command, CancellationToken.None);
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Count == 1)
                {
                    throw ex.InnerException;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public static async Task<List<string>> ExecuteShellCommandAsync(string filename, string command, CancellationToken cancellationToken)
        {
            CancellationTokenSource Token = new CancellationTokenSource();

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = filename,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                Arguments = command,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using (ProcessEx process = await Task.Run(() => { return ProcessEx.Start(start); }))
            {
                process.BeginOutputReadLine();

                process.EnableRaisingEvents = true;

                List<string> receiver = new List<string>();

                void OnOutputDataReceived(object sender, DataReceivedEventArgsEx e)
                {
                    if (e.Data == null)
                    {
                        Token.Cancel();
                        return;
                    }
                    string line = e.Data ?? string.Empty;

                    receiver?.Add(line);
                }

                try
                {
                    process.OutputDataReceived += OnOutputDataReceived;
                    await Task.Run(() =>
                    {
                        while (!process.IsExited)
                        {
                            if (Token.Token.IsCancellationRequested) { break; }
                            if (cancellationToken.IsCancellationRequested) { break; }
                        }
                    });
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

                return receiver;
            }
        }
    }
}
