using System;
using System.Numerics;
using Windows.ApplicationModel;

namespace APKInstaller.Models
{
    public readonly record struct SystemVersionInfo(int Major, int Minor, int Build, int Revision = 0) : IComparable, IComparable<SystemVersionInfo>, IComparisonOperators<SystemVersionInfo, SystemVersionInfo, bool>
    {
        public override int GetHashCode() => HashCode.Combine(Major, Minor, Build, Revision);

        public int CompareTo(SystemVersionInfo other) =>
            Major != other.Major
                ? Major.CompareTo(other.Major)
                : Minor != other.Minor
                    ? Minor.CompareTo(other.Minor)
                    : Build != other.Build
                        ? Build.CompareTo(other.Build)
                        : Revision != other.Revision
                            ? Revision.CompareTo(other.Revision)
                            : 0;

        public int CompareTo(object obj) => obj is SystemVersionInfo other ? CompareTo(other) : throw new ArgumentException("obj is not SystemVersionInfo", nameof(obj));

        public static bool operator <(SystemVersionInfo left, SystemVersionInfo right) => left.CompareTo(right) < 0;

        public static bool operator <=(SystemVersionInfo left, SystemVersionInfo right) => left.CompareTo(right) <= 0;

        public static bool operator >(SystemVersionInfo left, SystemVersionInfo right) => left.CompareTo(right) > 0;

        public static bool operator >=(SystemVersionInfo left, SystemVersionInfo right) => left.CompareTo(right) >= 0;

        /// <summary>
        /// Returns a string representation of a version with the format 'Major.Minor.Build.Revision'.
        /// </summary>
        /// <param name="significance">The number of version numbers to return, default is 4 for the full version number.</param>
        /// <returns>Version string of the format 'Major.Minor.Build.Revision'</returns>
        public string ToString(int significance = 4) => significance switch
        {
            4 => $"{Major}.{Minor}.{Build}.{Revision}",
            3 => $"{Major}.{Minor}.{Build}",
            2 => $"{Major}.{Minor}",
            1 => $"{Major}",
            _ => throw new ArgumentOutOfRangeException(nameof(significance), "Value must be a value 1 through 4."),
        };

        public override string ToString() => $"{Major}.{Minor}.{Build}.{Revision}";

        public static implicit operator SystemVersionInfo(Version version) => new(version.Major, version.Minor, version.Build, version.Revision);

        public static implicit operator SystemVersionInfo(PackageVersion version) => new(version.Major, version.Minor, version.Build, version.Revision);

        public static implicit operator SystemVersionInfo((int Major, int Minor, int Build, int Revision) version) => new(version.Major, version.Minor, version.Build, version.Revision);

        public static implicit operator Version(SystemVersionInfo version) => new(version.Major, version.Minor, version.Build, version.Revision);

        public static explicit operator PackageVersion(SystemVersionInfo version) => new() { Major = (ushort)version.Major, Minor = (ushort)version.Minor, Build = (ushort)version.Build, Revision = (ushort)version.Revision };

        public static implicit operator (int Major, int Minor, int Build, int Revision)(SystemVersionInfo version) => (version.Major, version.Minor, version.Build, version.Revision);
    }
}
