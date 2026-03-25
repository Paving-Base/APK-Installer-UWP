using System;
using System.Collections.Generic;

namespace Zeroconf.Models
{
    public enum ScanQueryType
    {
        Ptr,
        Any
    }

    public abstract class ZeroconfOptions
    {
        public int Retries
        {
            get;
            set => field = value < 0 ? throw new ArgumentOutOfRangeException(nameof(value)) : value;
        }

        public TimeSpan ScanTime { get; set; }
        public TimeSpan RetryDelay { get; set; }

        public IEnumerable<string> Protocols { get; }

        public bool AllowOverlappedQueries { get; set; }

        public ScanQueryType ScanQueryType { get; set; }

        protected ZeroconfOptions(string protocol) : this([protocol])
        {
        }

        protected ZeroconfOptions(IEnumerable<string> protocols)
        {
            Protocols = new HashSet<string>(protocols
                ?? throw new ArgumentNullException(nameof(protocols)), StringComparer.OrdinalIgnoreCase);
            Retries = 2;
            RetryDelay = TimeSpan.FromSeconds(2);
            ScanTime = TimeSpan.FromSeconds(2);
            ScanQueryType = ScanQueryType.Ptr;
        }
    }

    public sealed class BrowseDomainsOptions() : ZeroconfOptions("_services._dns-sd._udp.local.");

    public sealed class ResolveOptions : ZeroconfOptions
    {
        public ResolveOptions(string protocol) : base(protocol)
        {
        }

        public ResolveOptions(IEnumerable<string> protocols) : base(protocols)
        {
        }
    }
}