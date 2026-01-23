using AAPTForNet.Models;

namespace AAPTForNet.Filters
{
    internal sealed class PackageFilter : BaseFilter
    {
        private string[] Segments = [];

        public override bool CanHandle(string msg) => msg.StartsWith("package:");

        public override void AddMessage(string msg) => Segments = msg.Split(Separator);

        public override ApkInfo GetAPK() => new()
        {
            SplitName = GetValueOrDefault("split"),
            PackageName = GetValueOrDefault("package"),
            VersionName = GetValueOrDefault("versionName"),
            VersionCode = GetValueOrDefault("versionCode"),
        };

        public override void Clear() => Segments = [];

        private string GetValueOrDefault(string key)
        {
            string output = string.Empty;
            for (int i = 0; i < Segments.Length; i++)
            {
                if (Segments[i].Contains(key))
                {
                    // Find key
                    output = Segments[++i]; // Get value
                    break;
                }
            }
            return string.IsNullOrEmpty(output) ? DefaultEmptyValue : output;
        }
    }
}
