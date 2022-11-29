﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AAPTForUWP.Models
{
    public class ApkInfo
    {
        public string AppName { get; set; }
        public string SplitName { get; set; }
        public string PackageName { get; set; }
        public string VersionName { get; set; }
        public string VersionCode { get; set; }
        /// <summary>
        /// Absolute path to apk file
        /// </summary>
        public string FullPath { get; set; }
        public Icon Icon { get; set; }
        public SDKInfo MinSDK { get; set; }
        public SDKInfo TargetSDK { get; set; }
        public List<ApkInfo> SplitApks { get; set; }
        public List<string> Permissions { get; set; }
        /// <summary>
        /// Supported application binary interfaces
        /// </summary>
        public List<string> SupportedABIs { get; set; }
        public List<string> SupportScreens { get; set; }
        /// <summary>
        /// Size of package, in bytes
        /// </summary>
        public long PackageSize
        {
            get
            {
                try
                {
                    return new FileInfo(FullPath).Length;
                }
                catch
                {
                    return 0;
                }
            }
        }
        /// <summary>
        /// Determines whether this package is filled or not
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return AppName == string.Empty && PackageName == string.Empty;
            }
        }

        public bool IsSplit
        {
            get
            {
                return SplitName != "Unknown";
            }
        }

        public bool IsBundle
        {
            get
            {
                return SplitApks != null && SplitApks.Any();
            }
        }

        public ApkInfo()
        {
            AppName = string.Empty;
            PackageName = string.Empty;
            VersionName = string.Empty;
            VersionCode = string.Empty;
            FullPath = string.Empty;
            Icon = Icon.Default;
            MinSDK = SDKInfo.Unknown;
            TargetSDK = SDKInfo.Unknown;
            Permissions = new List<string>();
            SupportedABIs = new List<string>();
            SupportScreens = new List<string>();
        }

        public void AddSplit(ApkInfo info) => SplitApks.Add(info);

        public void AddSplit(string path) => SplitApks.Add(AAPTool.Decompile(path));

        internal ApkInfo Megre(params ApkInfo[] apks)
        {
            return apks.Any(a => a == null) ? throw new ArgumentNullException() : ApkInfo.Merge(this, apks);
        }

        internal static ApkInfo Merge(IEnumerable<ApkInfo> apks)
        {
            return Merge(null, apks);
        }

        internal static ApkInfo Merge(ApkInfo init, IEnumerable<ApkInfo> apks)
        {
            init ??= new ApkInfo();

            ApkInfo appApk = apks.FirstOrDefault(a => a.AppName.Length > 0);
            if (appApk != null)
            {
                init.AppName = appApk.AppName;
            }

            ApkInfo pckApk = apks.FirstOrDefault(a => a.PackageName.Length > 0);
            if (pckApk != null)
            {
                init.SplitName = pckApk.SplitName;
                init.VersionName = pckApk.VersionName;
                init.VersionCode = pckApk.VersionCode;
                init.PackageName = pckApk.PackageName;
            }

            ApkInfo sdkApk = apks.FirstOrDefault(a => !SDKInfo.Unknown.Equals(a.MinSDK));
            if (sdkApk != null)
            {
                init.MinSDK = sdkApk.MinSDK;
                init.TargetSDK = sdkApk.TargetSDK;
            }

            ApkInfo perApk = apks.FirstOrDefault(a => a.Permissions.Count > 0);
            if (perApk != null)
            {
                init.Permissions = perApk.Permissions;
            }

            ApkInfo abiApk = apks.FirstOrDefault(a => a.SupportedABIs.Count > 0);
            if (abiApk != null)
            {
                init.SupportedABIs = abiApk.SupportedABIs;
            }

            ApkInfo scrApk = apks.FirstOrDefault(a => a.SupportScreens.Count > 0);
            if (scrApk != null)
            {
                init.SupportScreens = scrApk.SupportScreens;
            }

            ApkInfo iconApk = apks.FirstOrDefault(a => !Icon.Default.Equals(a.Icon));
            if (iconApk != null)
            {
                init.Icon = iconApk.Icon;
            }

            ApkInfo pathApk = apks.FirstOrDefault(a => a.FullPath.Length > 0);
            if (pathApk != null)
            {
                init.FullPath = pathApk.FullPath;
            }

            if (init.IsSplit)
            {
                if (init.AppName == "Unknown" && init.SplitName != "Unknown")
                {
                    init.AppName = init.SplitName;
                }
                if (init.VersionName == "Unknown" && init.VersionCode != "Unknown")
                {
                    init.VersionName = init.VersionCode;
                }
            }

            return init;
        }
    }

    internal class ApkInfos
    {
        public string PackageName { get; set; }
        public string VersionCode { get; set; }
        public List<ApkInfo> Apks { get; set; }
    }
}
