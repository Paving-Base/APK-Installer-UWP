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
    public static class AAPTool
    {
        private enum DumpTypes
        {
            Manifest = 0,
            Resources = 1,
            XmlTree = 2,
        }

        private static readonly string AppPath = Package.Current.InstalledLocation.Path;
        private static readonly string TempPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Caches", $"{Process.GetCurrentProcess().Id}", "AppPackages");
        private static readonly string LocalPath = ApplicationData.Current.LocalFolder.Path is string path ? path.Substring(0, path.LastIndexOf('\\')) : string.Empty;

        public static Func<string, string, Func<string, int, bool>, IList<string>, int, Task> DumpOverrideAsync;

        private static Task<DumpModel> DumpAsync(
            string path,
            string args,
            DumpTypes type,
            Func<string, int, bool> callback)
        {
            return DumpOverrideAsync == null || path.StartsWith(LocalPath, StringComparison.OrdinalIgnoreCase)
                ? DumpByProcessAsync(path, args, type, callback)
                : DumpByOverrideAsync(path, args, type, callback);
        }

        private static async Task<DumpModel> DumpByProcessAsync(
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
            while (!aapt.StandardOutput.EndOfStream && !terminated)
            {
                string msg = await aapt.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                output.Add(msg);
                if (terminated = callback(msg, index))
                {
                    try
                    {
                        aapt.Kill();
                    }
                    catch { }
                }
                else
                {
                    index++;
                }
            }

            while (!aapt.StandardError.EndOfStream)
            {
                await aapt.StandardError.ReadLineAsync()
                    .ContinueWith(x => output.Add(x.Result))
                    .ConfigureAwait(false);
            }

            try
            {
                aapt.WaitForExit();
                aapt.Close();
            }
            catch { }

            // Dump xml tree get only 1 message when failed, the others are 2.
            bool isSuccess =
                type != DumpTypes.XmlTree
                    ? output.Count > 2
                    : output.Count > 0;
            return new DumpModel(path, isSuccess, output);
        }

        private static async Task<DumpModel> DumpByOverrideAsync(
            string path,
            string args,
            DumpTypes type,
            Func<string, int, bool> callback)
        {
            string fileName = Path.Combine(AppPath + @"\Tools\aapt.exe");
            List<string> output = [];    // Messages from output stream
            string arguments = string.Empty;

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

            await DumpOverrideAsync(fileName, arguments, callback, output, Encoding.UTF8.CodePage).ConfigureAwait(false);

            // Dump xml tree get only 1 message when failed, the others are 2.
            bool isSuccess =
                type != DumpTypes.XmlTree
                    ? output.Count > 2
                    : output.Count > 0;
            return new DumpModel(path, isSuccess, output);
        }

        internal static Task<DumpModel> DumpManifestAsync(string path)
        {
            return DumpAsync(path, string.Empty, DumpTypes.Manifest, (msg, i) => false);
        }

        internal static Task<DumpModel> DumpResourcesAsync(string path, Func<string, int, bool> callback)
        {
            return DumpAsync(path, string.Empty, DumpTypes.Resources, callback);
        }

        internal static Task<DumpModel> DumpXmlTreeAsync(string path, string asset, Func<string, int, bool> callback = null)
        {
            callback ??= ((_, __) => false);
            return DumpAsync(path, asset, DumpTypes.XmlTree, callback);
        }

        internal static Task<DumpModel> DumpManifestTreeAsync(string path, Func<string, int, bool> callback = null)
        {
            return DumpXmlTreeAsync(path, "AndroidManifest.xml", callback);
        }

        /// <summary>
        /// Start point. Begin decompile apk to extract resources
        /// </summary>
        /// <param name="path">Absolute path to .apk file</param>
        /// <returns>Filled apk if dump process is not failed</returns>
        public static Task<ApkInfo> DecompileAsync(string file) =>
            StorageFile.GetFileFromPathAsync(file).AsTask().ContinueWith(x => DecompileAsync(x.Result)).Unwrap();

        /// <summary>
        /// Start point. Begin decompile apk to extract resources
        /// </summary>
        /// <param name="path">Absolute path to .apk file</param>
        /// <returns>Filled apk if dump process is not failed</returns>
        public static async Task<ApkInfo> DecompileAsync(StorageFile file)
        {
            List<string> apkList = [];

            if (file.FileType.Equals(".apk", StringComparison.OrdinalIgnoreCase))
            {
                if (DumpOverrideAsync == null ? !file.Path.StartsWith(LocalPath, StringComparison.OrdinalIgnoreCase)
                    : !(file.Path.StartsWith(LocalPath, StringComparison.OrdinalIgnoreCase)
                    || await file.GetBasicPropertiesAsync().AsTask().ContinueWith(x => x.Result.Size) > 500 * 1024 * 1024))
                {
                    file = await CreateTempApk(file).ConfigureAwait(false);
                }
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

            List<ApkInfo> apkInfo = [];
            foreach (string apkPath in apkList)
            {
                DumpModel manifest = await ApkExtractor.ExtractManifestAsync(apkPath).ConfigureAwait(false);
                if (!manifest.IsSuccess)
                {
                    continue;
                }

                ApkInfo apk = ApkParser.Parse(manifest);
                apk.FullPath = apkPath;
                apk.PackagePath = file.Path;

                if (apk.Icon.IsImage)
                {
                    // Included icon in manifest, extract it from apk
                    apk.Icon.RealPath = await ApkExtractor.ExtractIconImageAsync(apkPath, apk.Icon).ConfigureAwait(false);
                    if (await apk.Icon.IsHighDensityAsync().ConfigureAwait(false))
                    {
                        apkInfo.Add(apk);
                        continue;
                    }
                }

                apk.Icon = await ApkExtractor.ExtractLargestIconAsync(apkPath).ConfigureAwait(false);
                apkInfo.Add(apk);
            }

            if (apkInfo.Count <= 1) { return apkInfo.FirstOrDefault(); }

            ApkInfos package = apkInfo.GroupBy(x => x.PackageName).Select(x => new ApkInfos { PackageName = x.Key, Apks = [.. x] }).FirstOrDefault();
            ApkInfo baseApk = package.Apks.Where(x => !x.IsSplit).FirstOrDefault();
            baseApk.SplitApks = package.Apks.Where(x => x.IsSplit).Where(x => x.VersionCode == baseApk.VersionCode).ToList();

            return baseApk;
        }

        private static async Task<StorageFile> CreateTempApk(StorageFile sourceFile)
        {
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
