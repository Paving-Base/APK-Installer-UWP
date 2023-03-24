﻿using AAPTForNet.Models;
using System.Collections.Generic;

namespace AAPTForNet.Filters
{
    internal class LabelFilter : BaseFilter
    {
        private readonly List<string> Msessges = new();
        private string[] Segments => string.Join(string.Empty, Msessges).Split(Seperator);

        public override bool CanHandle(string msg) => msg.StartsWith("application-label-");

        public override void AddMessage(string msg)
        {
            if (!Msessges.Contains(msg))
            {
                Msessges.Add(msg);
            }
        }

        public override ApkInfo GetAPK() => new() { LocaleLabels = GetApplicationLabels() };

        public override void Clear() => Msessges.Clear();

        private Dictionary<string, string> GetApplicationLabels()
        {
            Dictionary<string, string> labels = new();
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
