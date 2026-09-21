using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace AAPTForNet.Models
{
    public readonly struct SDKInfo(string level, string ver, string code) : IEquatable<SDKInfo>, IComparable<SDKInfo>, IComparisonOperators<SDKInfo, SDKInfo, bool>
    {
        private const string UnknownVersion = nameof(Unknown);
        internal static SDKInfo Unknown => new();

        // https://source.android.com/setup/start/build-numbers
        private static readonly (string Version, string CodeName)[] AndroidVersions = [
            ("Unknown", "Unknown"),
            ("1.0", "Unnamed"),         // API level 1
            ("1.1", "Petit Four"),
            ("1.5", "Cupcake"),
            ("1.6", "Donut"),
            ("2.0", "Éclair"),
            ("2.0.1", "Éclair"),
            ("2.1", "Éclair"),
            ("2.2", "Froyo"),
            ("2.3", "Gingerbread"),
            ("2.3.3", "Gingerbread"),   // API level 10
            ("3.0", "Honeycomb"),
            ("3.1", "Honeycomb"),
            ("3.2", "Honeycomb"),
            ("4.0", "Ice Cream Sandwich"),
            ("4.0.3", "Ice Cream Sandwich"),
            ("4.1", "Jelly Bean"),
            ("4.2", "Jelly Bean"),
            ("4.3", "Jelly Bean"),
            ("4.4", "KitKat"),
            ("4.4W", "KitKat"),         // API level 20
            ("5.0", "Lollipop"),
            ("5.1", "Lollipop"),
            ("6.0", "Marshmallow"),
            ("7.0", "Nougat"),
            ("7.1", "Nougat"),
            ("8.0", "Oreo"),
            ("8.1", "Oreo"),
            ("9.0", "Pie"),
            ("10", "Q"),
            ("11", "Red Velvet Cake"),  // API level 30
            ("12", "Snow Cone"),
            ("12.1", "Snow Cone"),
            ("13", "Tiramisu"),
            ("14", "Upside Down Cake"),
            ("15", "Vanilla Ice Cream"),
            ("16", "Baklava"),
            ("17", "Cinnamon Bun"),
            ("18", "Y"),
            ("19", "Z"),
            ("20", "Hello from 2022!")  // API level 40
        ];

        public string APILevel => level;
        public string Version => ver;
        public string CodeName => code;

        public SDKInfo() : this("0", "0", UnknownVersion) { }

        public static SDKInfo GetInfo(int sdkVer)
        {
            int index = Math.Min(Math.Max(sdkVer, 0), AndroidVersions.Length - 1);
            if (sdkVer <= AndroidVersions.Length - 1)
            {
                (string version, string codeName) = AndroidVersions[index];
                return new SDKInfo(sdkVer.ToString(), version, codeName);
            }
            else
            {
                return new SDKInfo(sdkVer.ToString(), (sdkVer - 20).ToString(), "Unknown");
            }
        }

        public static SDKInfo GetInfo(string sdkVer) => int.TryParse(sdkVer, out int ver) ? GetInfo(ver) : new SDKInfo(sdkVer, sdkVer, UnknownVersion);

        public override int GetHashCode() => HashCode.Combine(APILevel);

        public bool Equals(SDKInfo other) => APILevel == other.APILevel;

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is SDKInfo other && Equals(other);

        public int CompareTo(SDKInfo other) =>
            int.TryParse(APILevel, out int ver) && int.TryParse(other.APILevel, out int anotherver) ? ver.CompareTo(anotherver) : 0;

        public static bool operator ==(SDKInfo left, SDKInfo right) => left.Equals(right);

        public static bool operator !=(SDKInfo left, SDKInfo right) => !(left == right);

        public static bool operator <(SDKInfo left, SDKInfo right) => left.CompareTo(right) < 0;

        public static bool operator <=(SDKInfo left, SDKInfo right) => left.CompareTo(right) <= 0;

        public static bool operator >(SDKInfo left, SDKInfo right) => left.CompareTo(right) > 0;
        public static bool operator >=(SDKInfo left, SDKInfo right) => left.CompareTo(right) >= 0;

        public override string ToString()
        {
            if (this == Unknown)
            {
                return UnknownVersion;
            }
            else
            {
                DefaultInterpolatedStringHandler handler = new(24, 3);
                handler.AppendLiteral("API Level ");
                handler.AppendLiteral(APILevel);
                handler.AppendLiteral(" (");
                if (Version != UnknownVersion)
                {
                    handler.AppendLiteral("Android ");
                }
                handler.AppendLiteral(Version);
                handler.AppendLiteral(" - ");
                handler.AppendLiteral(CodeName);
                handler.AppendLiteral(")");
                return handler.ToStringAndClear();
            }
        }
    }
}
