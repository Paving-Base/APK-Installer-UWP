using APKInstaller.Helpers;
using System.Text.RegularExpressions;

namespace APKInstaller.Models
{
    public readonly partial record struct GitInfo(string UserName, string Repository, string Branch, string Path, string FileName)
    {
        public const string NUAA_API = "https://raw.nuaa.cf/{0}/{1}/{2}/{3}/{4}";
        public const string YZUU_API = "https://raw.yzuu.cf/{0}/{1}/{2}/{3}/{4}";
        public const string FASTGIT_API = "https://raw.fastgit.org/{0}/{1}/{2}/{3}/{4}";
        public const string JSDELIVR_API = "https://cdn.jsdelivr.net/gh/{0}/{1}@{2}/{3}/{4}";
        public const string GITHUB_API = "https://raw.githubusercontent.com/{0}/{1}/{2}/{3}/{4}";

        public string FormatURL(string api, bool local = true)
        {
            if (local)
            {
                string culture = LanguageHelper.GetCurrentLanguage();
                return string.Format(api, UserName, Repository, Branch, Path, AddLanguage(FileName, culture));
            }
            return string.Format(api, UserName, Repository, Branch, Path, FileName);
        }

        private static string AddLanguage(string filename, string langCode) =>
            FileRegex.IsMatch(filename) && !LangRegex.IsMatch(filename)
                ? FileNameRegex.Replace(filename, $"${{name}}.{langCode}${{extension}}")
                : filename;

        [GeneratedRegex(@"^.*(\.\w+)$")]
        private static partial Regex FileRegex { get; }
        [GeneratedRegex(@"^.*\.[a-z]{2}(-[A-Z]{2})?\.\w+$")]
        private static partial Regex LangRegex { get; }
        [GeneratedRegex(@"(?<name>.*)(?<extension>\.\w+$)")]
        private static partial Regex FileNameRegex { get; }
    }
}
