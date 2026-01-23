using System;
using System.Diagnostics.CodeAnalysis;

namespace Zeroconf.Models
{
    public readonly struct AdapterInformation(string address, string name) : IEquatable<AdapterInformation>
    {
        public string Address { get; } = address;
        public string Name { get; } = name;

        public void Deconstruct(out string address, out string name)
        {
            address = Address;
            name = Name;
        }

        public bool Equals(AdapterInformation other) =>
            string.Equals(Address, other.Address) && string.Equals(Name, other.Name);

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is AdapterInformation information && Equals(information);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Address != null ? Address.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }

        public static bool operator ==(AdapterInformation left, AdapterInformation right) => left.Equals(right);

        public static bool operator !=(AdapterInformation left, AdapterInformation right) => !left.Equals(right);

        public override string ToString() => $"{Name}: {Address}";
    }
}