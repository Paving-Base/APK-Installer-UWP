using AAPTForNet.Models;

namespace AAPTForNet.Filters
{
    internal sealed class LaunchableFilter : BaseFilter
    {
        private string LaunchableActivity = string.Empty;

        public override bool CanHandle(string msg) => msg.StartsWith("launchable-activity:");

        public override void AddMessage(string msg) => LaunchableActivity = msg.Split(Separator)[1];

        public override ApkInfo GetAPK() => new() { LaunchableActivity = LaunchableActivity };

        public override void Clear() => LaunchableActivity = string.Empty;
    }
}
