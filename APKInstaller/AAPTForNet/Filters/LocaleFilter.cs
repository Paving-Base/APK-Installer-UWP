using AAPTForNet.Models;
using System;
using System.Linq;

namespace AAPTForNet.Filters
{
    internal class LocaleFilter : BaseFilter
    {
        private string[] Segments = [];

        public override bool CanHandle(string msg) => msg.StartsWith("locales:");

        public override void AddMessage(string msg) => Segments = msg.Split(new char[2] { ' ', '\'' }, StringSplitOptions.RemoveEmptyEntries);

        public override ApkInfo GetAPK() => new() { SupportLocales = Segments.Skip(1).Where(x => !x.Equals("--_--", StringComparison.OrdinalIgnoreCase)).ToList() }; // Skip "locales:"     

        public override void Clear() => throw new NotImplementedException();
    }
}
