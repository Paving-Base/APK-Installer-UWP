﻿using AAPTForNet.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Windows.Storage;
using Detector = AAPTForNet.ResourceDetector;

namespace AAPTForNet
{
    internal class ApkExtractor
    {
        private static int id = 0;

#if NET
        private static readonly string TempPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Caches", $"{Environment.ProcessId}");
#else
        private static readonly string TempPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Caches", $"{Process.GetCurrentProcess().Id}");
#endif

        public static DumpModel ExtractManifest(string path)
        {
            return AAPTool.DumpManifest(path);
        }

        /// <summary>
        /// Find the icon with maximum config (largest), then extract to file
        /// </summary>
        /// <param name="path"></param>
        public static Icon ExtractLargestIcon(string path)
        {
            Dictionary<string, Icon> iconTable = ExtractIconTable(path);

            if (iconTable.Count == 0)
            {
                return Icon.Default;
            }

            if (iconTable.Values.All(i => i.IsReference))
            {
                string refID = iconTable.Values.FirstOrDefault().IconName;
                iconTable = ExtractIconTable(path, refID);
            }

            if (iconTable.Values.All(i => i.IsMarkup))
            {
                // Try dumping markup asset and get icon
                string asset = iconTable.Values.FirstOrDefault().IconName;
                iconTable = DumpMarkupIcon(path, asset);
            }

            Icon largestIcon = ExtractLargestIcon(iconTable);
            largestIcon.RealPath = ExtractIconImage(path, largestIcon);

            return largestIcon;
        }

        private static Dictionary<string, Icon> DumpMarkupIcon(string path, string asset, int startIndex = -1)
        {
            Dictionary<string, Icon> output = DumpMarkupIcon(path, asset, out startIndex);

            return output.Count == 0 && startIndex < 5
                ? DumpMarkupIcon(path, asset, startIndex + 1)
                : output;
        }

        private static Dictionary<string, Icon> DumpMarkupIcon(
            string path, string asset, out int lastTryIndex, int start = -1)
        {
            // Not found any icon image in package?,
            // it maybe a markup file
            // try getting some images from markup.
            lastTryIndex = -1;

            DumpModel tree = AAPTool.DumpXmlTree(path, asset);
            if (!tree.IsSuccess)
            {
                return [];
            }

            string msg = string.Empty;
            start = start >= 0 && start < tree.Messages.Count ? start : 0;
            for (int i = start; i < tree.Messages.Count; i++)
            {
                lastTryIndex = i;
                msg = tree.Messages[i];

                if (Detector.IsBitmapElement(msg))
                {
                    string iconID = tree.Messages[i + 1].Split('@')[1];
                    return ExtractIconTable(path, iconID);
                }
            }

            return [];
        }

        private static Dictionary<string, Icon> ExtractIconTable(string path)
        {
            string iconID = ExtractIconID(path);
            return ExtractIconTable(path, iconID);
        }

        /// <summary>
        /// Extract resource id of launch icon from manifest tree
        /// </summary>
        /// <param name="path"></param>
        /// <returns>icon id</returns>
        private static string ExtractIconID(string path)
        {
            int iconIndex = 0;
            DumpModel manifestTree = AAPTool.DumpManifestTree(
                path,
                (m, i) =>
                {
                    if (m.Contains("android:icon"))
                    {
                        iconIndex = i;
                        return true;
                    }
                    return false;
                }
            );

            if (iconIndex == 0) // Package without launcher icon
            {
                return string.Empty;
            }

            if (manifestTree.IsSuccess)
            {
                string msg = manifestTree.Messages[iconIndex];
                return msg.Split('@')[1];
            }

            return string.Empty;
        }

        private static Dictionary<string, Icon> ExtractIconTable(string path, string iconID)
        {
            if (string.IsNullOrEmpty(iconID))
            {
                return [];
            }

            bool matchedEntry = false;
            List<int> indexes = [];  // Get position of icon in resource list
            DumpModel resTable = AAPTool.DumpResources(path, (m, i) =>
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
            });

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

            const char seperator = '\"';
            // Prevent duplicate key when add to Dictionary,
            // because comparison statement with 'hdpi' in config's values,
            // reverse list and get first elem with LINQ
            IEnumerable<string> configNames = Enum.GetNames(typeof(Configs)).Reverse();
            Dictionary<string, Icon> iconTable = [];
            void AddIcon2Table(string cfg, string iconName)
            {
                if (!iconTable.ContainsKey(cfg))
                {
                    iconTable.Add(cfg, new Icon(iconName));
                }
            }
            string msg, resValue, config;

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

                        if (Detector.IsResourceValue(resValue))
                        {
                            // Resource value is icon url
                            string iconName = resValue.Split(seperator)
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
        public static string ExtractIconImage(string path, Icon icon)
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

            TryExtractIconImage(path, icon.IconName, IconPath);
            return IconPath;
        }

        private static void TryExtractIconImage(string path, string iconName, string desFile)
        {
            try
            {
                ExtractIconImage(path, iconName, desFile);
            }
            catch (ArgumentException) { }
        }

        /// <summary>
        /// Extract icon with name @iconName from @path to @desFile
        /// </summary>
        /// <param name="path"></param>
        /// <param name="iconName"></param>
        /// <param name="desFile"></param>
        private static void ExtractIconImage(string path, string iconName, string desFile)
        {
            if (iconName.EndsWith(".xml") || !File.Exists(path))
            {
                throw new ArgumentException("Invalid params");
            }

            using ZipArchive archive = ZipFile.OpenRead(path);
            ZipArchiveEntry entry;

            for (int i = archive.Entries.Count - 1; i > 0; i--)
            {
                entry = archive.Entries[i];

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

            Icon icon = Icon.Default;
            List<string> configNames = [.. Enum.GetNames(typeof(Configs))];
            configNames.Sort(new ConfigComparer());

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
            public int Compare(string x, string y)
            {
                _ = Enum.TryParse(x, out Configs ex);
                _ = Enum.TryParse(y, out Configs ey);
                return ex > ey ? -1 : 1;
            }
        }
    }
}
