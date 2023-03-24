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
        /// Executes a command on the <c>powershell.exe</c>.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>A <see cref="Task"/> which return the list of results.</returns>
        public static Task<List<string>> ExecuteShellCommandAsync(string command) =>
            ExecuteShellCommandAsync(command, CancellationToken.None);

        /// <inheritdoc/>
        public static async Task<List<string>> ExecuteShellCommandAsync(string command, CancellationToken cancellationToken)
        {
            CancellationTokenSource Token = new();

            ProcessStartInfo start = new()
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                Arguments = command,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using ProcessEx process = await Task.Run(() => { return ProcessEx.Start(start); });
            process.BeginOutputReadLine();

            process.EnableRaisingEvents = true;

            List<string> receiver = new();

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
