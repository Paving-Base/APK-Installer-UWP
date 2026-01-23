using AAPTForNet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Detector = AAPTForNet.ResourceDetector;

namespace AAPTForNet
{
    internal sealed class ApkExtractor(AAPTool AAPTool)
    {
        private static int id = 0;
        private static readonly string TempPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Caches", $"{Environment.ProcessId}");

        public Task<DumpModel> ExtractManifestAsync(string path) => AAPTool.DumpManifestAsync(path);

        /// <summary>
        /// Find the icon with maximum config (largest), then extract to file
        /// </summary>
        /// <param name="path"></param>
        public async ValueTask<Icon> ExtractLargestIconAsync(string path)
        {
            Dictionary<string, Icon> iconTable = await ExtractIconTableAsync(path).ConfigureAwait(false);

            if (iconTable.Count == 0)
            {
                return Icon.Default;
            }

            if (iconTable.Values.All(i => i.IsReference))
            {
                string refID = iconTable.Values.First().IconName;
                iconTable = await ExtractIconTableAsync(path, refID).ConfigureAwait(false);
            }

            if (iconTable.Values.All(i => i.IsMarkup))
            {
                // Try dumping markup asset and get icon
                string asset = iconTable.Values.First().IconName;
                iconTable = await DumpMarkupIconAsync(path, asset).ConfigureAwait(false);
            }

            Icon largestIcon = ExtractLargestIcon(iconTable);
            largestIcon.RealPath = await ExtractIconImageAsync(path, largestIcon).ConfigureAwait(false);

            return largestIcon;
        }

        private async ValueTask<Dictionary<string, Icon>> DumpMarkupIconAsync(string path, string asset)
        {
            StrongBox<int> startIndex = new(-1);
            Dictionary<string, Icon> output = await DumpMarkupIconAsync(path, asset, startIndex).ConfigureAwait(false);
            return output.Count == 0 && startIndex.Value < 5
                ? await DumpMarkupIconAsync(path, asset).ConfigureAwait(false)
                : output;
        }

        private async ValueTask<Dictionary<string, Icon>> DumpMarkupIconAsync(
            string path, string asset, StrongBox<int> lastTryIndex, int start = -1)
        {
            // Not found any icon image in package?,
            // it maybe a markup file
            // try getting some images from markup.
            lastTryIndex.Value = -1;

            DumpModel tree = await AAPTool.DumpXmlTreeAsync(path, asset).ConfigureAwait(false);
            if (!tree.IsSuccess)
            {
                return [];
            }

            start = start >= 0 && start < tree.Messages.Count ? start : 0;
            for (int i = start; i < tree.Messages.Count; i++)
            {
                lastTryIndex.Value = i;
                string msg = tree.Messages[i];

                if (Detector.IsBitmapElement(msg))
                {
                    string iconID = tree.Messages[i + 1].Split('@')[1];
                    return await ExtractIconTableAsync(path, iconID).ConfigureAwait(false);
                }
            }

            return [];
        }

        private async ValueTask<Dictionary<string, Icon>> ExtractIconTableAsync(string path)
        {
            string iconID = await ExtractIconIDAsync(path).ConfigureAwait(false);
            return await ExtractIconTableAsync(path, iconID).ConfigureAwait(false);
        }

        /// <summary>
        /// Extract resource id of launch icon from manifest tree
        /// </summary>
        /// <param name="path"></param>
        /// <returns>icon id</returns>
        private async ValueTask<string> ExtractIconIDAsync(string path)
        {
            int iconIndex = -1;
            TaskCompletionSource<string> source = new();

            _ = AAPTool.DumpManifestTreeAsync(
                path,
                (m, i) =>
                {
                    if (m.Contains("android:icon"))
                    {
                        iconIndex = i;
                        _ = source.TrySetResult(m);
                        return true;
                    }
                    return false;
                }).ContinueWith(x =>
                {
                    if (iconIndex == -1)
                    {
                        _ = source.TrySetResult(null!);
                    }
                });

            string msg = await source.Task.ConfigureAwait(false);

            return iconIndex == -1 ? string.Empty : msg.Split('@')[1];
        }

        private async ValueTask<Dictionary<string, Icon>> ExtractIconTableAsync(string path, string iconID)
        {
            if (string.IsNullOrEmpty(iconID))
            {
                return [];
            }

            bool matchedEntry = false;
            List<int> indexes = [];  // Get position of icon in resource list
            DumpModel resTable = await AAPTool.DumpResourcesAsync(path, (m, i) =>
            {
                // Dump resources and get icons,
                // terminate when meet the end of mipmap entry,
                // icons are in 'drawable' or 'mipmap' resource
                if (Detector.IsResource(m, iconID))
                {
                    indexes.Add(i);
                }

                if (!matchedEntry)
                {
                    if (m.Contains("mipmap/"))
                    {
                        matchedEntry = true;    // Begin mipmap entry
                    }
                }
                else
                {
                    if (Detector.IsEntryType(m))
                    {  // Next entry, terminate
                        matchedEntry = false;
                        return true;
                    }
                }
                return false;
            }).ConfigureAwait(false);

            return CreateIconTable(indexes, resTable.Messages);
        }

        // Create table like below
        //  configs  |    mdpi           hdpi    ...    anydpi
        //  icon     |    icon1          icon2   ...    icon4
        private static Dictionary<string, Icon> CreateIconTable(List<int> positions, List<string> messages)
        {
            if (positions.Count == 0 || messages.Count <= 2)    // If dump failed
            {
                return [];
            }

            const char separator = '\"';
            // Prevent duplicate key when add to Dictionary,
            // because comparison statement with 'hdpi' in config's values,
            // reverse list and get first elem with LINQ
            IEnumerable<string> configNames = Enum.GetNames<Configs>().Reverse();
            Dictionary<string, Icon> iconTable = [];
            void AddIcon2Table(string cfg, string? iconName)
            {
                if (!iconTable.ContainsKey(cfg))
                {
                    iconTable[cfg] = new Icon(iconName);
                }
            }
            string msg, resValue;
            string? config;

            foreach (int index in positions)
            {
                for (int i = index; ; i--)
                {
                    // Go prev to find config
                    msg = messages[i];

                    if (Detector.IsEntryType(msg))  // Out of entry and not found
                    {
                        break;
                    }

                    if (Detector.IsConfig(msg))
                    {
                        // Match with predefined configs,
                        // go next to get icon name
                        resValue = messages[index + 1];

                        config = configNames.FirstOrDefault(msg.Contains);

                        if (config != null)
                        {
                            if (Detector.IsResourceValue(resValue))
                            {
                                // Resource value is icon url
                                string? iconName = resValue.Split(separator)
                                    .FirstOrDefault(n => n.Contains('/'));
                                AddIcon2Table(config, iconName);
                                break;
                            }
                            if (Detector.IsReference(resValue))
                            {
                                string iconID = resValue.Trim().Split(' ')[1];
                                AddIcon2Table(config, iconID);
                                break;
                            }
                        }

                        break;
                    }
                }
            }
            return iconTable;
        }

        /// <summary>
        /// Extract icon image from apk file
        /// </summary>
        /// <param name="path">path to apk file</param>
        /// <param name="icon"></param>
        /// <returns>Absolute path to extracted image</returns>
        public static async ValueTask<string> ExtractIconImageAsync(string path, Icon icon)
        {
            if (Icon.Default.Equals(icon))
            {
                return Icon.DefaultName;
            }

            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }

            string IconPath = Path.Combine(TempPath, $@"AAPToolTempImage-{id++}.png");

            await TryExtractIconImageAsync(path, icon.IconName, IconPath).ConfigureAwait(false);
            return IconPath;
        }

        private static async ValueTask TryExtractIconImageAsync(string path, string iconName, string desFile)
        {
            try
            {
                await ExtractIconImageAsync(path, iconName, desFile).ConfigureAwait(false);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Extract icon with name @iconName from @path to @desFile
        /// </summary>
        /// <param name="path"></param>
        /// <param name="iconName"></param>
        /// <param name="desFile"></param>
        private static async ValueTask ExtractIconImageAsync(string path, string iconName, string desFile)
        {
            if (iconName.EndsWith(".xml"))
            {
                throw new ArgumentException("Invalid params");
            }

            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            using Stream stream = await file.OpenStreamForReadAsync().ConfigureAwait(false);
            using ZipArchive archive = new(stream, ZipArchiveMode.Read);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.Name.Equals(iconName) ||
                    entry.FullName.Equals(iconName))
                {
                    entry.ExtractToFile(desFile, true);
                    break;
                }
            }
        }

        private static Icon ExtractLargestIcon(Dictionary<string, Icon> iconTable)
        {
            if (iconTable.Count == 0)
            {
                return Icon.Default;
            }

            Icon? icon = Icon.Default;
            string[] configNames = Enum.GetNames<Configs>();
            Array.Sort(configNames, new ConfigComparer());

            foreach (string cfg in configNames)
            {
                // Get the largest icon image, skip markup file (xml)
                if (iconTable.TryGetValue(cfg, out icon))
                {
                    if (icon.IconName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    break;  // Largest icon here :)
                }
            }

            return icon ?? Icon.Default;
        }

        /// <summary>
        /// DPI config comparer, ordered by desc (largest first)
        /// </summary>
        private class ConfigComparer : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                _ = Enum.TryParse(x, out Configs ex);
                _ = Enum.TryParse(y, out Configs ey);
                return ex > ey ? -1 : 1;
            }
        }
    }
}
