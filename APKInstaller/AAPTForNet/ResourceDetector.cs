using System;
using System.Text.RegularExpressions;

namespace AAPTForNet
{
    internal static partial class ResourceDetector
    {
        private static readonly string config =
            string.Join("|", Enum.GetNames<Models.Configs>());

        /// <summary>
        /// </summary>
        /// <param name="id">resource id</param>
        public static bool IsResource(string input, string id = "")
        {
            // id is a word (\w)
            id = string.IsNullOrEmpty(id) ? @"\w*" : id;
            return Regex.IsMatch(input, $"^\\s*resource\\s{id}");
        }
        /// <summary>
        /// Is resource value
        /// </summary>
        public static bool IsResourceValue(string input)
        {
            // Start with space, then (string?)
            return ResourceValueRegex.IsMatch(input);
        }
        /// <summary>
        /// Determines resource is reference (to another)
        /// </summary>
        public static bool IsReference(string input)
        {
            // Start with space, then (reference)
            return ReferenceRegex.IsMatch(input);
        }
        /// <summary>
        /// Determines resource is a bitmap resource
        /// </summary>
        public static bool IsBitmapElement(string input)
        {
            return BitmapRegex.IsMatch(input);
        }

        public static bool IsConfig(string input)
        {
            // config (default) | (hdpi|mdpi|...)[-vxx]
            return Regex.IsMatch(input, $"^\\s*config\\s\\(?({config})(-v\\d*)?\\)?:");
        }

        public static bool IsEntryType(string input)
        {
            // type x configCount=xx entryCount=xxx
            return EntryTypeRegex.IsMatch(input);
        }

        [GeneratedRegex(@"^\s*\((string\d*)\)*")]
        private static partial Regex ResourceValueRegex { get; }

        [GeneratedRegex(@"^\s*\((reference)\)*")]
        private static partial Regex ReferenceRegex { get; }

        [GeneratedRegex(@"^\s*E:\sbitmap")]
        private static partial Regex BitmapRegex { get; }

        [GeneratedRegex(@"^\s*type\s\d*\sconfigCount=\d*\sentryCount=\d*$")]
        private static partial Regex EntryTypeRegex { get; }
    }
}
