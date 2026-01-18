using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Exceptions;
using AdvancedSharpAdbClient.Logs;
using APKInstaller.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace APKInstaller.Common
{
    public partial class AdbCommandClient(string adbPath, bool isForce = false, ILogger<AdbCommandLineClient> logger = null) : AdbCommandLineClient(adbPath, isForce, logger)
    {
        /// <summary>
        /// The <see cref="Array"/> of <see cref="char"/>s that represent a new line.
        /// </summary>
        private static readonly char[] separator = ['\r', '\n'];

        protected override int RunProcess(string filename, string command, ICollection<string> errorOutput, ICollection<string> standardOutput, int timeout)
        {
            using IServerManager manager = Factory.TryCreateServerManager();
            IProcessResult result = manager.RunProcess.RunProcess(filename, command, errorOutput != null, standardOutput != null, timeout);
            errorOutput?.AddRange(result.ErrorOutput.Split(separator, StringSplitOptions.RemoveEmptyEntries));
            standardOutput?.AddRange(result.StandardOutput.Split(separator, StringSplitOptions.RemoveEmptyEntries));
            return result.ExitCode;
        }

        protected override async Task<int> RunProcessAsync(string filename, string command, ICollection<string> errorOutput, ICollection<string> standardOutput, CancellationToken cancellationToken = default)
        {
            using IServerManager manager = Factory.TryCreateServerManager();
            IProcessResult result = await manager.RunProcess.RunProcessAsync(filename, command, errorOutput != null, standardOutput != null).AsTask(cancellationToken).ConfigureAwait(false);
            errorOutput?.AddRange(result.ErrorOutput.Split(separator, StringSplitOptions.RemoveEmptyEntries));
            standardOutput?.AddRange(result.StandardOutput.Split(separator, StringSplitOptions.RemoveEmptyEntries));
            return result.ExitCode;
        }
    }

    public partial class RunProcess : IRunProcess
    {
        public static IRunProcess Instance { get; } = new RunProcess();

        IAsyncOperation<IProcessResult> IRunProcess.RunProcessAsync(string filename, string command, bool errorOutput, bool standardOutput)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                ProcessStartInfo psi = new(filename, command)
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardError = errorOutput,
                    RedirectStandardOutput = standardOutput
                };

                using Process process = Process.Start(psi) ?? throw new AdbException($"The adb process could not be started. The process returned null when starting {filename} {command}");

                ProcessResult result = new();

                using (CancellationTokenRegistration registration = cancellationToken.Register(process.Kill))
                {
                    if (errorOutput)
                    {
                        string standardErrorString = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                        result.ErrorOutput = standardErrorString;
                    }

                    if (standardOutput)
                    {
                        string standardOutputString = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                        result.StandardOutput = standardOutputString;
                    }

                    if (!process.HasExited)
                    {
                        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    }
                }

                // get the return code from the process
                result.ExitCode = process.ExitCode;
                return result as IProcessResult;
            });
        }

        IProcessResult IRunProcess.RunProcess(string filename, string command, bool errorOutput, bool standardOutput, int timeout)
        {
            ProcessStartInfo psi = new(filename, command)
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardError = errorOutput,
                RedirectStandardOutput = standardOutput
            };

            using Process process = Process.Start(psi) ?? throw new AdbException($"The adb process could not be started. The process returned null when starting {filename} {command}");

            // get the return code from the process
            if (!process.WaitForExit(timeout))
            {
                process.Kill();
            }

            ProcessResult result = new();

            if (errorOutput)
            {
                string standardErrorString = process.StandardError.ReadToEnd();
                result.ErrorOutput = standardErrorString;
            }

            if (standardOutput)
            {
                string standardOutputString = process.StandardOutput.ReadToEnd();
                result.StandardOutput = standardOutputString;
            }

            result.ExitCode = process.ExitCode;
            return result;
        }

        private partial class ProcessResult : IProcessResult
        {
            public int ExitCode { get; set; }
            public string ErrorOutput { get; set; } = string.Empty;
            public string StandardOutput { get; set; } = string.Empty;
        }
    }
}
