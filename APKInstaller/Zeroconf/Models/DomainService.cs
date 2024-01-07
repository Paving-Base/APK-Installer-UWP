using System;

namespace Zeroconf.Models
{
    public readonly struct DomainService(string domain, string service) : IEquatable<DomainService>
    {
        public string Domain { get; } = domain;
        public string Service { get; } = service;

        public void Deconstruct(out string domain, out string service)
        {
            domain = Domain;
            service = Service;
        }

        public bool Equals(DomainService other)
        {
            return string.Equals(Domain, other.Domain) && string.Equals(Service, other.Service);
        }

        public override bool Equals(object obj)
        {
            return obj is not null && obj is DomainService service && Equals(service);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Domain != null ? Domain.GetHashCode() : 0) * 397) ^ (Service != null ? Service.GetHashCode() : 0);
            }
        }

        public static bool operator ==(DomainService left, DomainService right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DomainService left, DomainService right)
        {
            return !left.Equals(right);
        }
    }
}
