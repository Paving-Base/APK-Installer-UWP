using AAPTForNet;
using AAPTForNet.Models;
using APKInstaller.Helpers;
using APKInstaller.Metadata;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace APKInstaller.Common
{
    public class OOPAAPTool : AAPTool
    {
        protected override bool HasDumpOverride => true;

        protected override async Task<DumpModel> DumpByOverrideAsync(
            string path,
            string args,
            DumpTypes type,
            Func<string, int, bool> callback)
        {
            string fileName = Path.Combine(AppPath + @"\Tools\aapt.exe");
            List<string> output = [];    // Messages from output stream
            string arguments;

            switch (type)
            {
                case DumpTypes.Manifest:
                    arguments = $"dump badging \"{path}\"";
                    break;
                case DumpTypes.Resources:
                    arguments = $"dump --values resources \"{path}\"";
                    break;
                case DumpTypes.XmlTree:
                    arguments = $"dump xmltree \"{path}\" {args}";
                    break;
                default:
                    return new DumpModel(path, false, output);
            }

            using (IServerManager manager = Factory.TryCreateServerManager())
            {
                await manager.RunProcess.DumpAsync(fileName, arguments, callback == null ? null : new DumpDelegate(callback), output);
            }

            // Dump xml tree get only 1 message when failed, the others are 2.
            bool isSuccess =
                type != DumpTypes.XmlTree
                    ? output.Count > 2
                    : output.Count > 0;
            return new DumpModel(path, isSuccess, output);
        }

        protected override async ValueTask<StorageFile> CreateHardLinkAsync(StorageFile file)
        {
            try
            {
                string path = Path.Combine(TempPath, file.Name);
                if (File.Exists(file.Path) && Loopback.Instance.CreateFileSymbolic(path, file.Path))
                {
                    goto end;
                }
                using (IServerManager manager = Factory.TryCreateServerManager())
                {
                    if (!manager.Loopback.CreateFileSymbolic(path, file.Path))
                    {
                        return null;
                    }
                }
                end:
                if (File.Exists(path))
                {
                    return await StorageFile.GetFileFromPathAsync(path);
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LoggerFactory.CreateLogger<OOPAAPTool>().LogWarning(ex, "Create hard link failed. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
            }
            return null;
        }
    }

    public partial class RunProcess : IRunProcess
    {
        [AsyncMethodBuilder(typeof(AsyncActionMethodBuilder))]
        async IAsyncAction IRunProcess.DumpAsync(string filename, string command, DumpDelegate callback, IList<string> output)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = filename,
                Arguments = command,
                CreateNoWindow = true,
                UseShellExecute = false, // For read output data
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            int index = 0;
            bool terminated = false;
            using Process aapt = Process.Start(startInfo);
            if (callback == null)
            {
                string _result = await aapt.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                output.AddRange(_result.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                while (!terminated)
                {
                    string msg = await aapt.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                    if (msg == null) { break; }
                    output.Add(msg);
                    if (terminated = callback?.Invoke(msg, index) == true)
                    {
                        try
                        {
                            aapt.Kill();
                        }
                        catch { }
                        return;
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            string result = await aapt.StandardError.ReadToEndAsync().ConfigureAwait(false);
            output.AddRange(result.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
