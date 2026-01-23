using AAPTForNet.Models;
using System.Collections.Generic;

namespace AAPTForNet.Filters
{
    internal sealed class SDKFilter : BaseFilter
    {
        private readonly List<string> Messages = [];
        private string[] Segments => string.Join(string.Empty, Messages).Split(Separator);

        public override bool CanHandle(string msg) => msg.StartsWith("sdkVersion:") || msg.StartsWith("targetSdkVersion:");

        public override void AddMessage(string msg)
        {
            if (!Messages.Contains(msg))
            {
                Messages.Add(msg);
            }
        }

        public override ApkInfo GetAPK()
        {
            return new ApkInfo
            {
                MinSDK = SDKInfo.GetInfo(GetMinSDKVersion()),
                TargetSDK = SDKInfo.GetInfo(GetTargetSDKVersion())
            };
        }

        public override void Clear() => Messages.Clear();

        private string GetMinSDKVersion()
        {
            for (int i = 0; i < Segments.Length; i++)
            {
                if (Segments[i].StartsWith("sdkVersion:"))
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
                if (Segments[i].StartsWith("targetSdkVersion:"))
                {
                    return Segments[++i];
                }
            }
            return string.Empty;
        }
    }
}
