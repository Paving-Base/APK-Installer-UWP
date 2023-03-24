using AAPTForUWP.Models;
using ProcessForUWP.UWP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using Windows.ApplicationModel;
using Windows.Storage;

namespace AAPTForUWP
{
    /// <summary>
    /// Android Assert Packing Tool for NET
    /// </summary>
    public class AAPTool : ProcessEx
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
            StartInfo.StandardOutputEncoding = Encoding.GetEncoding("utf-8");
        }

        protected new void Start(string args)
        {
            StartInfo.Arguments = args;
            Start();
            BeginOutputReadLine();
        }

        private static DumpModel Dump(string path, string args, DumpTypes type, Func<string, int, bool> callback)
        {
            int index = 0;
            bool terminated = false;
            string msg = string.Empty;
            AAPTool aapt = new AAPTool();
            List<string> output = new List<string>();    // Messages from output stream
            CancellationTokenSource cancellationToken = new CancellationTokenSource();

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

            void OnOutputDataReceived(object sender, DataReceivedEventArgsEx e)
            {
                if (e.Data == null)
                {
                    terminated = true;
                    cancellationToken.Cancel();
                    return;
                }
                msg = e.Data ?? string.Empty;

                if (callback(msg, index))
                {
                    terminated = true;
                    try
                    {
                        aapt.Kill();
                        cancellationToken.Cancel();
                    }
                    catch { }
                }
                if (!terminated)
                {
                    index++;
                }

                output?.Add(msg);
            }

            try
            {
                aapt.OutputDataReceived += OnOutputDataReceived;
                while (!aapt.IsExited)
                {
                    if (cancellationToken.Token.IsCancellationRequested) { break; }
                }
            }
            catch (Exception)
            {
                aapt.Kill();
            }
            finally
            {
                aapt.Close();
                aapt.OutputDataReceived -= OnOutputDataReceived;
            }

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
        public static ApkInfo Decompile(string path)
        {
            string temppath = CreateTempApk(path);
            List<string> apks = new();

            if (temppath.EndsWith(".apk"))
            {
                apks.Add(temppath);
            }
            else
            {
                using ZipArchive archive = ZipFile.OpenRead(temppath);
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

                if (!apks.Any())
                {
                    apks.Add(temppath);
                }
            }

            List<ApkInfo> apkInfos = new();
            foreach (string apkpath in apks)
            {
                DumpModel manifest = null;
                try
                {
                    manifest = ApkExtractor.ExtractManifest(apkpath);
                    if (!manifest.IsSuccess)
                    {
                        continue;
                    }
                }
                catch
                {
                    if (manifest == null)
                    {
                        if (apks.Count() <= 1)
                        {
                            apkInfos.Add(new ApkInfo { AppName = Path.GetFileName(apkpath), FullPath = apkpath });
                        }
                        continue;
                    }
                }

                ApkInfo apk = ApkParser.Parse(manifest);
                apk.FullPath = apkpath;

                if (apk.Icon.IsImage)
                {
                    // Included icon in manifest, extract it from apk
                    apk.Icon.RealPath = ApkExtractor.ExtractIconImage(apkpath, apk.Icon);
                    if (apk.Icon.IsHighDensity)
                    {
                        apkInfos.Add(apk);
                        continue;
                    }
                }

                apk.Icon = ApkExtractor.ExtractLargestIcon(apkpath);
                apkInfos.Add(apk);
            }

            if (!apkInfos.Any()) { return new ApkInfo(); }

            if (apkInfos.Count <= 1) { return apkInfos.First(); }

            List<ApkInfos> packages = apkInfos.GroupBy(x => x.PackageName).Select(x => new ApkInfos { PackageName = x.Key, Apks = x.ToList() }).ToList();

            if (packages.Count > 1) { throw new Exception("This is a Multiple Package."); }

            List<ApkInfo> infos = new();
            foreach (ApkInfos package in packages)
            {
                foreach (ApkInfo baseapk in package.Apks.Where(x => !x.IsSplit))
                {
                    baseapk.SplitApks = package.Apks.Where(x => x.IsSplit).Where(x => x.VersionCode == baseapk.VersionCode).ToList();
                    infos.Add(baseapk);
                }
            }

            if (infos.Count > 1) { throw new Exception("There are more than one base APK in this Package."); }

            if (!infos.Any()) { throw new Exception("There are all dependents in this Package."); }

            return infos.First();
        }

        private static string CreateTempApk(string sourceFile)
        {
            string tempFile = Path.Combine(TempPath, Path.GetFileName(sourceFile));

            if (!Directory.Exists(TempPath))
            {
                _ = Directory.CreateDirectory(TempPath);
            }

            try
            {
                FileEx.CopyFile(sourceFile, tempFile, true);
                return tempFile;
            }
            catch
            {
                return sourceFile;
            }
        }
    }
}
