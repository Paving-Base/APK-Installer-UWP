using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DataReceivedEventArgs = ProcessForUWP.UWP端.DataReceivedEventArgs;
using Process = ProcessForUWP.UWP端.Process;

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
        public static void ExecuteShellCommand(string command)
        {
            try
            {
                ExecuteShellCommandAsync(command, CancellationToken.None);
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
        public static void ExecuteShellCommandAsync(string command, CancellationToken cancellationToken)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = @"C:\Users\qq251\Downloads\Github\APK-Installer-UWP\ApkInstallHost\bin\x64\Debug\AppX\AAPTForUWP\tool\aapt.exe",
                Arguments = @"dump badging ""C:\Users\qq251\Downloads\Programs\Minecraft_1.17.40.06_sign.apk""",
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            Process process = new Process();
            process.StartInfo = start;
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.EnableRaisingEvents = true;

            List<string> receiver = new List<string>();

            void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                string line = e.Data;

                if (line == null)
                {
                    process.Close();
                    return;
                }

                if (receiver != null)
                {
                    receiver.Add(line);
                }
            }

            try
            {
                process.OutputDataReceived += OnOutputDataReceived;
                process.WaitForExit();
                process.Close();
            }
            catch (Exception)
            {
                string message = string.Empty;
                foreach (string line in receiver)
                {
                    message = $"{message}\n{line}";
                }
                process.OutputDataReceived -= OnOutputDataReceived;
            }
        }
    }
}
