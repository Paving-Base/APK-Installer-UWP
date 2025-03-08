using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf.Interfaces;
using Zeroconf.Models;

namespace Zeroconf.Common
{
    internal class NetworkInterface : INetworkInterface
    {
        public async Task NetworkRequestAsync(
            byte[] requestBytes,
            TimeSpan scanTime,
            int retries,
            int retryDelayMilliseconds,
            Action<IPAddress, byte[]> onResponse,
            CancellationToken cancellationToken,
            IEnumerable<System.Net.NetworkInformation.NetworkInterface> netInterfacesToSendRequestOn = null)
        {
            // populate list with all adapters if none specified
            if (netInterfacesToSendRequestOn?.Any() != true)
            {
                netInterfacesToSendRequestOn = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            }

            List<Task> tasks = [.. netInterfacesToSendRequestOn
                .Select(inter =>
                    NetworkRequestAsync(requestBytes, scanTime, retries, retryDelayMilliseconds, onResponse, inter, cancellationToken))];

            await Task.WhenAll(tasks)
                      .ConfigureAwait(false);
        }

        private async Task NetworkRequestAsync(
            byte[] requestBytes,
            TimeSpan scanTime,
            int retries,
            int retryDelayMilliseconds,
            Action<IPAddress, byte[]> onResponse,
            System.Net.NetworkInformation.NetworkInterface adapter,
            CancellationToken cancellationToken)
        {
            // http://stackoverflow.com/questions/2192548/specifying-what-network-interface-an-udp-multicast-should-go-to-in-net

            // Xamarin doesn't support this
            if (adapter.GetIPProperties().MulticastAddresses.Count is not > 0)
            {
                return; // most of VPN adapters will be skipped
            }

            if (!adapter.SupportsMulticast)
            {
                return; // multicast is meaningless for this type of connection
            }

            if (OperationalStatus.Up != adapter.OperationalStatus)
            {
                return; // this adapter is off or not connected
            }

            if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                return; // strip out loopback addresses
            }

            IPv4InterfaceProperties p = adapter.GetIPProperties().GetIPv4Properties();
            if (null == p)
            {
                return; // IPv4 is not configured on this adapter
            }

            IPAddress ipv4Address = adapter.GetIPProperties().UnicastAddresses
                .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;

            if (ipv4Address == null)
            {
                return; // could not find an IPv4 address for this adapter
            }

            int ifaceIndex = p.Index;

            Debug.WriteLine("Scanning on iface {0}, idx {1}, IP: {2}", adapter.Name, ifaceIndex, ipv4Address);

            using UdpClient client = new();
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    Socket socket = client.Client;

                    if (socket.IsBound)
                    {
                        continue;
                    }

                    socket.SetSocketOption(
                        SocketOptionLevel.IP,
                        SocketOptionName.MulticastInterface,
                        IPAddress.HostToNetworkOrder(ifaceIndex));

                    client.ExclusiveAddressUse = false;
                    socket.SetSocketOption(
                        SocketOptionLevel.Socket,
                        SocketOptionName.ReuseAddress,
                        true);
                    socket.SetSocketOption(
                        SocketOptionLevel.Socket,
                        SocketOptionName.ReceiveTimeout,
                        (int)scanTime.TotalMilliseconds);
                    client.ExclusiveAddressUse = false;

                    IPEndPoint localEp = new(IPAddress.Any, 5353);
                    Debug.WriteLine("Attempting to bind to {0} on adapter {1}", localEp, adapter.Name);

                    socket.Bind(localEp);
                    Debug.WriteLine("Bound to {0}", localEp);

                    IPAddress multicastAddress = IPAddress.Parse("224.0.0.251");
                    MulticastOption multOpt = new(multicastAddress, ifaceIndex);
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multOpt);
                    Debug.WriteLine("Bound to multicast address");

                    // Start a receive loop
                    bool shouldCancel = false;
                    Task recTask = Task.Run(async () =>
                    {
                        try
                        {
                            while (!Volatile.Read(ref shouldCancel))
                            {
                                UdpReceiveResult res = await client.ReceiveAsync()
                                                      .ConfigureAwait(false);

                                onResponse(res.RemoteEndPoint.Address, res.Buffer);
                            }
                        }
                        catch when (Volatile.Read(ref shouldCancel))
                        {
                            // If we're canceling, eat any exceptions that come from here   
                        }
                    }, cancellationToken);

                    IPEndPoint broadcastEp = new(IPAddress.Parse("224.0.0.251"), 5353);
                    Debug.WriteLine("About to send on iface {0}", [adapter.Name]);

                    await client.SendAsync(requestBytes, requestBytes.Length, broadcastEp)
                                .ConfigureAwait(false);
                    Debug.WriteLine("Sent mDNS query on iface {0}", [adapter.Name]);

                    // wait for responses
                    await Task.Delay(scanTime, cancellationToken)
                              .ConfigureAwait(false);

                    Volatile.Write(ref shouldCancel, true);

                    ((IDisposable)client).Dispose();

                    Debug.WriteLine("Done Scanning");

                    await recTask.ConfigureAwait(false);

                    return;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Execption with network request, IP {0}\n: {1}", ipv4Address, e);
                    if (i + 1 >= retries) // last one, pass underlying out
                    {
                        // Ensure all inner info is captured                            
                        ExceptionDispatchInfo.Capture(e).Throw();
                        throw;
                    }
                }

                await Task.Delay(retryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task ListenForAnnouncementsAsync(Action<AdapterInformation, string, byte[]> callback, CancellationToken cancellationToken)
        {
            return Task.WhenAll(
                System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .Where(a => a.GetIPProperties().MulticastAddresses.Count > 0) // Xamarin doesn't support this
                    .Where(a => a.SupportsMulticast)
                    .Where(a => a.OperationalStatus == OperationalStatus.Up)
                    .Where(a => a.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Where(a => a.GetIPProperties().GetIPv4Properties() != null)
                    .Where(a => a.GetIPProperties().UnicastAddresses.Any(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork))
                    .Select(inter => ListenForAnnouncementsAsync(inter, callback, cancellationToken)));
        }

        private Task ListenForAnnouncementsAsync(System.Net.NetworkInformation.NetworkInterface adapter, Action<AdapterInformation, string, byte[]> callback, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(async () =>
            {
                IPAddress ipv4Address = adapter.GetIPProperties().UnicastAddresses
                .First(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;

                if (ipv4Address == null)
                {
                    return;
                }

                int? ifaceIndex = adapter.GetIPProperties().GetIPv4Properties()?.Index;
                if (ifaceIndex == null)
                {
                    return;
                }

                Debug.WriteLine("Scanning on iface {0}, idx {1}, IP: {2}", adapter.Name, ifaceIndex, ipv4Address);

                using UdpClient client = new();
                Socket socket = client.Client;
                socket.SetSocketOption(
                    SocketOptionLevel.IP,
                    SocketOptionName.MulticastInterface,
                    IPAddress.HostToNetworkOrder(ifaceIndex.Value));

                socket.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress,
                    true);
                client.ExclusiveAddressUse = false;


                IPEndPoint localEp = new(IPAddress.Any, 5353);
                socket.Bind(localEp);

                IPAddress multicastAddress = IPAddress.Parse("224.0.0.251");
                MulticastOption multOpt = new(multicastAddress, ifaceIndex.Value);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multOpt);

                _ = cancellationToken.Register(() => ((IDisposable)client).Dispose());

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        UdpReceiveResult packet = await client.ReceiveAsync()
                                             .ConfigureAwait(false);
                        try
                        {
                            callback(new AdapterInformation(ipv4Address.ToString(), adapter.Name), packet.RemoteEndPoint.Address.ToString(), packet.Buffer);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Callback threw an exception: {0}", ex);
                        }
                    }
                    catch when (cancellationToken.IsCancellationRequested)
                    {
                        // eat any exceptions if we've been cancelled
                    }
                }

                Debug.WriteLine("Done listening for mDNS packets on {0}, idx {1}, IP: {2}.", adapter.Name, ifaceIndex, ipv4Address);

                cancellationToken.ThrowIfCancellationRequested();
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }
    }
}
