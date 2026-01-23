using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Zeroconf.Interfaces;
using Zeroconf.Models;

namespace Zeroconf
{
    public sealed class ResolverListener : IDisposable
    {
        private readonly IEnumerable<string> protocols;
        private readonly TimeSpan scanTime;
        private readonly int retries;
        private readonly int retryDelayMilliseconds;
        private readonly Timer timer;
        private readonly int pingsUntilRemove;
        private HashSet<(string, string)> discoveredHosts = [];
        private readonly Dictionary<(string, string), int> toRemove = [];

        public IReadOnlyList<IZeroconfHost>? Hosts { get; private set; }

        public event EventHandler<IZeroconfHost>? ServiceFound;
        public event EventHandler<IZeroconfHost>? ServiceLost;
        public event EventHandler<Exception>? Error;

        internal ResolverListener(IEnumerable<string> protocols, int queryInterval, int pingsUntilRemove, TimeSpan scanTime, int retries, int retryDelayMilliseconds)
        {
            this.protocols = protocols;
            this.scanTime = scanTime;
            this.retries = retries;
            this.retryDelayMilliseconds = retryDelayMilliseconds;
            this.pingsUntilRemove = pingsUntilRemove;
            timer = new Timer(DiscoverHosts, this, 0, queryInterval);
        }

        private async void DiscoverHosts(object? state)
        {
            try
            {
                ResolverListener? instance = state as ResolverListener;
                IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(protocols, scanTime, retries, retryDelayMilliseconds).ConfigureAwait(false);
                instance?.OnResolved(hosts);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        [MemberNotNull(nameof(Hosts))]
        private void OnResolved(IReadOnlyList<IZeroconfHost> hosts)
        {
            Hosts = hosts;
            lock (discoveredHosts)
            {
                HashSet<(string, string)> newHosts = [.. discoveredHosts];
                HashSet<(string, string)> remainingHosts = [.. discoveredHosts];

                foreach (IZeroconfHost host in hosts)
                {
                    foreach (KeyValuePair<string, IService> service in host.Services)
                    {
                        (string, string) keyValue = (host.DisplayName, service.Value.Name);
                        if (discoveredHosts.Contains(keyValue))
                        {
                            remainingHosts.Remove(keyValue);
                        }
                        else
                        {
                            ServiceFound?.Invoke(this, host);
                            newHosts.Add(keyValue);
                        }
                        toRemove.Remove(keyValue);
                    }
                }

                foreach ((string, string) service in remainingHosts)
                {
                    if (!toRemove.TryAdd(service, 0))
                    {
                        //zeroconf sometimes reports missing hosts incorrectly. 
                        //after pingsUntilRemove missing hosts reports, we'll remove the service from the list.
                        if (++toRemove[service] > pingsUntilRemove)
                        {
                            toRemove.Remove(service);
                            newHosts.Remove(service);
                            ServiceLost?.Invoke(this, new ZeroconfHost { DisplayName = service.Item1, Id = service.Item2 });
                        }
                    }
                }

                discoveredHosts = newHosts;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer?.Dispose();
            }
        }
    }
}
