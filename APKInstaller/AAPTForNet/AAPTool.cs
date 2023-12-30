using AAPTForNet.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;

namespace AAPTForNet
{
    /// <summary>
    /// Android Assert Packing Tool for NET
    /// </summary>
    public class AAPTool : Process
    {
        private enum DumpTypes
        {
            Manifest = 0,
            Resources = 1,
            XmlTree = 2,
        }

        private static readonly string AppPath = Package.Current.InstalledLocation.Path;
#if NET5_0_OR_GREATER
        private static readonly string TempPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Caches", $"{Environment.ProcessId}", "AppPackages");
#else
        private static readonly string TempPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Caches", $"{GetCurrentProcess().Id}", "AppPackages");
#endif

        protected AAPTool()
        {
            StartInfo.FileName = AppPath + @"\Tools\aapt.exe";
            StartInfo.CreateNoWindow = true;
            StartInfo.UseShellExecute = false; // For read output data
            StartInfo.RedirectStandardError = true;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.StandardOutputEncoding = Encoding.UTF8;
        }

        protected new bool Start(string args)
        {
            StartInfo.Arguments = args;
            return Start();
        }

        private static DumpModel Dump(
            string path,
            string args,
            DumpTypes type,
            Func<string, int, bool> callback)
        {

            int index = 0;
            bool terminated = false;
            AAPTool aapt = new();
            List<string> output = [];    // Messages from output stream

            switch (type)
            {
                case DumpTypes.Manifest:
                    aapt.Start($"dump badging \"{path}\"");
                    break;
                case DumpTypes.Resources:
                    aapt.Start($"dump --values resources \"{path}\"");
                    break;
                case DumpTypes.XmlTree:
                    aapt.Start($"dump xmltree \"{path}\" {args}");
                    break;
                default:
                    return new DumpModel(path, false, output);
            }

            while (!aapt.StandardOutput.EndOfStream && !terminated)
            {
                string msg = aapt.StandardOutput.ReadLine();

                if (callback(msg, index))
                {
                    terminated = true;
                    try
                    {
                        aapt.Kill();
                    }
                    catch { }
                }
                if (!terminated)
                {
                    index++;
                }

                output.Add(msg);
            }

            while (!aapt.StandardError.EndOfStream)
            {
                output.Add(aapt.StandardError.ReadLine());
            }

            try
            {
                aapt.WaitForExit();
                aapt.Close();
            }
            catch { }

            // Dump xml tree get only 1 message when failed, the others are 2.
            bool isSuccess = type != DumpTypes.XmlTree ?
                output.Count > 2 : output.Count > 0;
            return new DumpModel(path, isSuccess, output);
        }

        internal static DumpModel DumpManifest(string path)
        {
            return Dump(path, string.Empty, DumpTypes.Manifest, (msg, i) => false);
        }

        internal static DumpModel DumpResources(string path, Func<string, int, bool> callback)
        {
            return Dump(path, string.Empty, DumpTypes.Resources, callback);
        }

        internal static DumpModel DumpXmlTree(string path, string asset, Func<string, int, bool> callback = null)
        {
            callback ??= ((_, __) => false);
            return Dump(path, asset, DumpTypes.XmlTree, callback);
        }

        internal static DumpModel DumpManifestTree(string path, Func<string, int, bool> callback = null)
        {
            return DumpXmlTree(path, "AndroidManifest.xml", callback);
        }

        /// <summary>
        /// Start point. Begin decompile apk to extract resources
        /// </summary>
        /// <param name="path">Absolute path to .apk file</param>
        /// <returns>Filled apk if dump process is not failed</returns>
        public static Task<ApkInfo> Decompile(string file) =>
            StorageFile.GetFileFromPathAsync(file).AsTask().ContinueWith(x => Decompile(x.Result)).Unwrap();

        /// <summary>
        /// Start point. Begin decompile apk to extract resources
        /// </summary>
        /// <param name="path">Absolute path to .apk file</param>
        /// <returns>Filled apk if dump process is not failed</returns>
        public static async Task<ApkInfo> Decompile(StorageFile file)
        {
            List<string> apks = [];

            if (file.FileType == ".apk")
            {
                file = await CreateTempApk(file);
                apks.Add(file.Path);
            }
            else
            {
                using (IRandomAccessStreamWithContentType random = await file.OpenReadAsync())
                using (Stream stream = random.AsStream())
                using (ZipArchive archive = new(stream, ZipArchiveMode.Read))
                {
                    if (!Directory.Exists(TempPath))
                    {
                        Directory.CreateDirectory(TempPath);
                    }

                    foreach (ZipArchiveEntry entry in archive.Entries.Where(x => !x.FullName.Contains("/")))
                    {
                        if (entry.Name.ToLower().EndsWith(".apk"))
                        {
                            string APKTemp = Path.Combine(TempPath, entry.FullName);
                            entry.ExtractToFile(APKTemp, true);
                            apks.Add(APKTemp);
                        }
                    }
                }

                if (!apks.Any())
                {
                    apks.Add(file.Path);
                }
            }

            List<ApkInfo> apkInfos = [];
            foreach (string apkPath in apks)
            {
                DumpModel manifest = ApkExtractor.ExtractManifest(apkPath);
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
                    apk.Icon.RealPath = ApkExtractor.ExtractIconImage(apkPath, apk.Icon);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && apk.Icon.IsHighDensity)
                    {
                        apkInfos.Add(apk);
                        continue;
                    }
                }

                apk.Icon = ApkExtractor.ExtractLargestIcon(apkPath);
                apkInfos.Add(apk);
            }

            if (!apkInfos.Any()) { return new ApkInfo(); }

            if (apkInfos.Count <= 1) { return apkInfos.FirstOrDefault(); }

            List<ApkInfos> packages = apkInfos.GroupBy(x => x.PackageName).Select(x => new ApkInfos { PackageName = x.Key, Apks = x.ToList() }).ToList();

            if (packages.Count > 1) { throw new Exception("This is a Multiple Package."); }

            List<ApkInfo> infos = [];
            foreach (ApkInfos package in packages)
            {
                foreach (ApkInfo baseApk in package.Apks.Where(x => !x.IsSplit))
                {
                    baseApk.SplitApks = package.Apks.Where(x => x.IsSplit).Where(x => x.VersionCode == baseApk.VersionCode).ToList();
                    infos.Add(baseApk);
                }
            }

            if (infos.Count > 1) { throw new Exception("There are more than one base APK in this Package."); }

            if (!infos.Any()) { throw new Exception("There are all dependents in this Package."); }

            ApkInfo info = infos.FirstOrDefault();

            return info;
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
                StorageFile temp = await sourceFile.CopyAsync(folder);
                return temp;
            }
            catch
            {
                return sourceFile;
            }
        }
    }
}
