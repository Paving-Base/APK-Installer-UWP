using AdvancedSharpAdbClient;
using APKInstaller.Projection;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APKInstaller.Helpers
{
    public class AddressHelper
    {
        public static async Task<List<string>> GetAddressID(string mac)
        {
            List<string> addresses = [];
            Regex Regex = new($@"\s*(\d+.\d+.\d+.\d+)\s*{mac}\S*\s*\w+");
            List<string> lines = [];
            _ = await APKInstallerProjectionFactory.ServerManager.RunProcessAsync("powershell.exe", $"arp -a|findstr {mac}", null, lines);
            foreach (string line in lines)
            {
                if (Regex.IsMatch(line))
                {
                    addresses.Add(Regex.Match(line).Groups[1].Value);
                }
            }
            return addresses;
        }

        public static async Task ConnectHyperV()
        {
            AdbClient AdbClient = new();
            List<string> addresses = await GetAddressID("00-15-5d");
            foreach (string address in addresses)
            {
                _ = AdbClient.ConnectAsync(address);
            }
        }

        public static async Task<List<string>> ConnectHyperVAsync()
        {
            AdbClient AdbClient = new();
            List<string> addresses = await GetAddressID("00-15-5d");
            List<string> results = [];
            foreach (string address in addresses)
            {
                results.Add(await AdbClient.ConnectAsync(address));
            }
            return results;
        }
    }
}
