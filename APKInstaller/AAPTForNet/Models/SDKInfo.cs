using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace AAPTForNet.Models
{
    public readonly struct SDKInfo(string level, string ver, string code) : IComparable<SDKInfo>, IComparisonOperators<SDKInfo, SDKInfo, bool>
    {
        internal static readonly SDKInfo Unknown = new();

        // https://source.android.com/setup/start/build-numbers
        private static readonly string[] AndroidCodeNames = [
            "Unknown",
            "Unnamed",          // API level 1
            "Petit Four",
            "Cupcake",
            "Donut",
            "Éclair",
            "Éclair",
            "Éclair",
            "Froyo",
            "Gingerbread",
            "Gingerbread",      // API level 10
            "Honeycomb",
            "Honeycomb",
            "Honeycomb",
            "Ice Cream Sandwich",
            "Ice Cream Sandwich",
            "Jelly Bean",
            "Jelly Bean",
            "Jelly Bean",
            "KitKat",
            "KitKat",           // API level 20
            "Lollipop",
            "Lollipop",
            "Marshmallow",
            "Nougat",
            "Nougat",
            "Oreo",
            "Oreo",
            "Pie",
            "Q",
            "Red Velvet Cake",  // API level 30
            "Snow Cone",
            "Snow Cone",
            "Tiramisu",
            "Upside Down Cake",
            "Vanilla Ice Cream",
            "Baklava",
            "Cinnamon Bun",
            "Y",
            "Z",
            "Hello from 2022!"  // API level 40
        ];

        private static readonly string[] AndroidVersionCodes = [
            "Unknown",
            "1.0",      // API level 1
            "1.1",
            "1.5",
            "1.6",
            "2.0",
            "2.0.1",
            "2.1",
            "2.2",
            "2.3",
            "2.3.3",    // API level 10
            "3.0",
            "3.1",
            "3.2",
            "4.0",
            "4.0.3",
            "4.1",
            "4.2",
            "4.3",
            "4.4",
            "4.4W",     // API level 20
            "5.0",
            "5.1",
            "6.0",
            "7.0",
            "7.1",
            "8.0",
            "8.1",
            "9.0",
            "10",
            "11",       // API level 30
            "12",
            "12.1",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20"        // API level 40
        ];

        public string APILevel { get; } = level;
        public string Version { get; } = ver;
        public string CodeName { get; } = code;

        public SDKInfo() : this("0", "0", "0") { }

        public static SDKInfo GetInfo(int sdkVer)
        {
            int index = Math.Min(Math.Max(sdkVer, 0), AndroidCodeNames.Length - 1);
            string version = sdkVer <= AndroidVersionCodes.Length - 1 ? AndroidVersionCodes[index] : (sdkVer - 20).ToString();
            return new SDKInfo(sdkVer.ToString(), version, AndroidCodeNames[index]);
        }

        public static SDKInfo GetInfo(string sdkVer) => int.TryParse(sdkVer, out int ver) ? GetInfo(ver) : new SDKInfo(sdkVer, sdkVer, AndroidCodeNames[0]);

        public override int GetHashCode() => 1008763889 + EqualityComparer<string>.Default.GetHashCode(APILevel);

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is SDKInfo another && APILevel == another.APILevel;

        public int CompareTo(SDKInfo other) =>
            int.TryParse(APILevel, out int ver) && int.TryParse(other.APILevel, out int anotherver) ? ver.CompareTo(anotherver) : 0;

        public static bool operator ==(SDKInfo left, SDKInfo right) => left.Equals(right);

        public static bool operator !=(SDKInfo left, SDKInfo right) => !(left == right);

        public static bool operator <(SDKInfo left, SDKInfo right) => left.CompareTo(right) < 0;

        public static bool operator <=(SDKInfo left, SDKInfo right) => left.CompareTo(right) <= 0;

        public static bool operator >(SDKInfo left, SDKInfo right) => left.CompareTo(right) > 0;
        public static bool operator >=(SDKInfo left, SDKInfo right) => left.CompareTo(right) >= 0;

        public override string ToString() => this == Unknown
            ? AndroidCodeNames[0]
            : $"API Level {APILevel} " +
                $"{(Version == AndroidVersionCodes[0]
                ? $"({Version} - " : $"(Android {Version} - ")}" +
                $"{CodeName})";
    }
}
