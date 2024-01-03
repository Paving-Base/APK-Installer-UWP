﻿using AdvancedSharpAdbClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;
using Zeroconf.Interfaces;

namespace APKInstaller.Helpers
{
    public static class ZeroconfHelper
    {
        public static ResolverListener ConnectListener { get; private set; }

        public static void InitializeConnectListener()
        {
            if (ConnectListener == null)
            {
                ConnectListener = ZeroconfResolver.CreateListener("_adb-tls-connect._tcp.local.");
                ConnectListener.ServiceFound += ConnectListener_ServiceFound;
            }
        }

        public static void DisposeConnectListener()
        {
            if (ConnectListener != null)
            {
                ConnectListener.ServiceFound -= ConnectListener_ServiceFound;
                ConnectListener.Dispose();
            }
        }

        private static async void ConnectListener_ServiceFound(object sender, IZeroconfHost e)
        {
            if ((await AdbServer.Instance.GetStatusAsync(CancellationToken.None)).IsRunning)
            {
                await new AdbClient().ConnectAsync(e.IPAddress, e.Services.FirstOrDefault().Value.Port);
            }
        }

        public static async Task ConnectPairedDevice()
        {
            IReadOnlyList<IZeroconfHost> hosts = ConnectListener != null
                ? ConnectListener.Hosts
                : await ZeroconfResolver.ResolveAsync("_adb-tls-connect._tcp.local.");
            if (hosts?.Count is > 0)
            {
                AdbClient AdbClient = new();
                foreach (IZeroconfHost host in hosts)
                {
                    _ = AdbClient.ConnectAsync(host.IPAddress, host.Services.FirstOrDefault().Value.Port);
                }
            }
        }

        public static async Task<List<string>> ConnectPairedDeviceAsync()
        {
            List<string> results = [];
            IReadOnlyList<IZeroconfHost> hosts = ConnectListener != null
                ? ConnectListener.Hosts
                : await ZeroconfResolver.ResolveAsync("_adb-tls-connect._tcp.local.");
            if (hosts?.Count is > 0)
            {
                AdbClient AdbClient = new();
                foreach (IZeroconfHost host in hosts)
                {
                    results.Add(await AdbClient.ConnectAsync(host.IPAddress, host.Services.FirstOrDefault().Value.Port));
                }
            }
            return results;
        }
    }
}
