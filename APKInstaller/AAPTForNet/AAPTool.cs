using AAPTForNet.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace AAPTForNet
{
    /// <summary>
    /// Android Assert Packing Tool for NET
    /// </summary>
    public class AAPTool
    {
        protected enum DumpTypes
        {
            Manifest = 0,
            Resources = 1,
            XmlTree = 2
        }

        protected static string AppPath { get; } = Package.Current.InstalledLocation.Path;
        protected static string TempPath { get; } = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Caches", $"{Process.GetCurrentProcess().Id}", "AppPackages");
        protected static string LocalPath { get; } = ApplicationData.Current.LocalFolder.Path is string path ? path.Substring(0, path.LastIndexOf('\\')) : string.Empty;

        protected virtual bool HasDumpOverride { get; } = false;

        protected virtual Task<DumpModel> DumpAsync(
            string path,
            string args,
            DumpTypes type,
            Func<string, int, bool> callback)
        {
            return !HasDumpOverride || path.StartsWith(LocalPath, StringComparison.OrdinalIgnoreCase)
                ? DumpByProcessAsync(path, args, type, callback)
                : DumpByOverrideAsync(path, args, type, callback);
        }

        protected virtual async Task<DumpModel> DumpByProcessAsync(
            string path,
            string args,
            DumpTypes type,
            Func<string, int, bool> callback)
        {

            int index = 0;
            bool terminated = false;
            ProcessStartInfo startInfo = new()
            {
                FileName = Path.Combine(AppPath + @"\Tools\aapt.exe"),
                CreateNoWindow = true,
                UseShellExecute = false, // For read output data
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            List<string> output = [];    // Messages from output stream

            switch (type)
            {
                case DumpTypes.Manifest:
                    startInfo.Arguments = $"dump badging \"{path}\"";
                    break;
                case DumpTypes.Resources:
                    startInfo.Arguments = $"dump --values resources \"{path}\"";
                    break;
                case DumpTypes.XmlTree:
                    startInfo.Arguments = $"dump xmltree \"{path}\" {args}";
                    break;
                default:
                    return new DumpModel(path, false, output);
            }

            using Process aapt = Process.Start(startInfo);
            if (callback == null)
            {
                while (!aapt.StandardOutput.EndOfStream)
                {
                    await aapt.StandardOutput.ReadLineAsync()
                        .ContinueWith(x => output.Add(x.Result))
                        .ConfigureAwait(false);
                }
            }
            else
            {
                while (!aapt.StandardOutput.EndOfStream && !terminated)
                {
                    string msg = await aapt.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                    output.Add(msg);
                    if (terminated = callback?.Invoke(msg, index) == true)
                    {
                        try
                        {
                            aapt.Kill();
                        }
                        catch { }
                        goto end;
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            while (!aapt.StandardError.EndOfStream)
            {
                await aapt.StandardError.ReadLineAsync()
                    .ContinueWith(x => output.Add(x.Result))
                    .ConfigureAwait(false);
            }

            end:
            _ = Task.Run(() =>
            {
                try
                {
                    aapt.WaitForExit(5000);
                    aapt.Dispose();
                }
                catch { }
            });

            // Dump xml tree get only 1 message when failed, the others are 2.
            bool isSuccess =
                type != DumpTypes.XmlTree
                    ? output.Count > 2
                    : output.Count > 0;
            return new DumpModel(path, isSuccess, output);
        }

        protected virtual Task<DumpModel> DumpByOverrideAsync(
            string path,
            string args,
            DumpTypes type,
            Func<string, int, bool> callback) => null;

        internal Task<DumpModel> DumpManifestAsync(string path) =>
            DumpAsync(path, string.Empty, DumpTypes.Manifest, null);

        internal Task<DumpModel> DumpResourcesAsync(string path, Func<string, int, bool> callback = null) =>
            DumpAsync(path, string.Empty, DumpTypes.Resources, callback);

        internal Task<DumpModel> DumpXmlTreeAsync(string path, string asset, Func<string, int, bool> callback = null) =>
            DumpAsync(path, asset, DumpTypes.XmlTree, callback);

        internal Task<DumpModel> DumpManifestTreeAsync(string path, Func<string, int, bool> callback = null) =>
            DumpXmlTreeAsync(path, "AndroidManifest.xml", callback);

        /// <summary>
        /// Start point. Begin decompile apk to extract resources
        /// </summary>
        /// <param name="path">Absolute path to .apk file</param>
        /// <returns>Filled apk if dump process is not failed</returns>
        public async Task<ApkInfo> DecompileAsync(StorageFile file)
        {
            List<string> apkList = [];

            if (file.FileType.Equals(".apk", StringComparison.OrdinalIgnoreCase))
            {
                file = await PrefixStorageFile(file).ConfigureAwait(false);
                apkList.Add(file.Path);
            }
            else
            {
                using (Stream stream = await file.OpenStreamForReadAsync().ConfigureAwait(false))
                using (ZipArchive archive = new(stream, ZipArchiveMode.Read))
                {
                    string path = Path.Combine(TempPath, file.Name);

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    foreach (ZipArchiveEntry entry in archive.Entries.Where(x => !x.FullName.Contains("/")))
                    {
                        if (entry.Name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                        {
                            string APKTemp = Path.Combine(path, entry.FullName);
                            entry.ExtractToFile(APKTemp, true);
                            apkList.Add(APKTemp);
                        }
                    }
                }

                if (apkList.Count <= 0)
                {
                    apkList.Add(file.Path);
                }
            }

            ApkExtractor apkExtractor = new(this);
            ApkInfo[] apkInfo = await Task.WhenAll(apkList.Select(async apkPath =>
            {
                DumpModel manifest = await apkExtractor.ExtractManifestAsync(apkPath).ConfigureAwait(false);
                if (!manifest.IsSuccess)
                {
                    return null;
                }

                ApkInfo apk = ApkParser.Parse(manifest);
                apk.FullPath = apkPath;
                apk.PackagePath = file.Path;

                Task<ulong> task = StorageFile
                    .GetFileFromPathAsync(apkPath).AsTask()
                    .ContinueWith(x =>
                        x.Result.GetBasicPropertiesAsync().AsTask()
                        .ContinueWith(x => apk.PackageSize = x.Result.Size)).Unwrap();

                if (apk.Icon.IsImage)
                {
                    // Included icon in manifest, extract it from apk
                    apk.Icon.RealPath = await apkExtractor.ExtractIconImageAsync(apkPath, apk.Icon).ConfigureAwait(false);
                    if (await apk.Icon.IsHighDensityAsync().ConfigureAwait(false))
                    {
                        return apk;
                    }
                }

                Icon icon = await apkExtractor.ExtractLargestIconAsync(apkPath).ConfigureAwait(false);
                if (icon != Icon.Default)
                {
                    apk.Icon = icon;
                }

                await task.ConfigureAwait(false);
                return apk;
            })).ContinueWith(x => x.Result.OfType<ApkInfo>().ToArray()).ConfigureAwait(false);

            if (apkInfo.Length <= 1) { return apkInfo.FirstOrDefault(); }

            IGrouping<string, ApkInfo> package = apkInfo.GroupBy(x => x.PackageName).FirstOrDefault();
            ApkInfo baseApk = package.Where(x => !x.IsSplit).FirstOrDefault();
            baseApk.SplitApks = package.Where(x => x.IsSplit).Where(x => x.VersionCode == baseApk.VersionCode).ToList();

            return baseApk;
        }

        protected virtual async Task<StorageFile> PrefixStorageFile(StorageFile sourceFile)
        {
            if (HasDumpOverride ? sourceFile.Path.StartsWith(LocalPath, StringComparison.OrdinalIgnoreCase)
                || await sourceFile.GetBasicPropertiesAsync().AsTask().ContinueWith(x => x.Result.Size) > 500 * 1024 * 1024
                : sourceFile.Path.StartsWith(LocalPath, StringComparison.OrdinalIgnoreCase))
            {
                return sourceFile;
            }

            if (!Directory.Exists(TempPath))
            {
                _ = Directory.CreateDirectory(TempPath);
            }

            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(TempPath);
                StorageFile temp = await sourceFile.CopyAsync(folder, sourceFile.Name, NameCollisionOption.GenerateUniqueName);
                return temp;
            }
            catch
            {
                return sourceFile;
            }
        }
    }
}
