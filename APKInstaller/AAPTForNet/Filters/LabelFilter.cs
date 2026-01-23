using AAPTForNet.Models;
using System.Collections.Generic;

namespace AAPTForNet.Filters
{
    internal sealed class LabelFilter : BaseFilter
    {
        private readonly List<string> Messages = [];
        private string[] Segments => string.Join(string.Empty, Messages).Split(Separator);

        public override bool CanHandle(string msg) => msg.StartsWith("application-label-");

        public override void AddMessage(string msg)
        {
            if (!Messages.Contains(msg))
            {
                Messages.Add(msg);
            }
        }

        public override ApkInfo GetAPK() => new() { LocaleLabels = GetApplicationLabels() };

        public override void Clear() => Messages.Clear();

        private Dictionary<string, string> GetApplicationLabels()
        {
            Dictionary<string, string> labels = [];
            for (int i = 0; i < Segments.Length; i++)
            {
                string Segment = Segments[i];
                if (Segment.StartsWith("application-label-"))
                {
                    string locale = Segment.Substring(18, Segments[i].Length - 19);
                    labels.Add(locale, Segments[++i]);
                }
            }
            return labels;
        }
    }
}
