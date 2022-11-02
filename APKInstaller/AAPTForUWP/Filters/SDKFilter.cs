﻿using AAPTForUWP.Models;
using System.Collections.Generic;

namespace AAPTForUWP.Filters
{
    internal class SDKFilter : BaseFilter
    {
        private readonly List<string> Msessges = new();
        private string[] Segments => string.Join(" ", Msessges).Split(Seperator);

        public override bool CanHandle(string msg)
        {
            return msg.StartsWith("sdkVersion:") || msg.StartsWith("targetSdkVersion:");
        }

        public override void AddMessage(string msg)
        {
            if (!Msessges.Contains(msg))
            {
                Msessges.Add(msg);
            }
        }

        public override ApkInfo GetAPK()
        {
            return new ApkInfo()
            {
                MinSDK = SDKInfo.GetInfo(GetMinSDKVersion()),
                TargetSDK = SDKInfo.GetInfo(GetTargetSDKVersion())
            };
        }

        public override void Clear() => Msessges.Clear();

        private string GetMinSDKVersion()
        {
            for (int i = 0; i < Segments.Length; i++)
            {
                if (Segments[i].Contains("sdkVersion"))
                {
                    return Segments[++i];
                }
            }
            return string.Empty;
        }

        private string GetTargetSDKVersion()
        {
            for (int i = 0; i < Segments.Length; i++)
            {
                if (Segments[i].Contains("targetSdkVersion"))
                {
                    return Segments[++i];
                }
            }
            return string.Empty;
        }
    }
}
