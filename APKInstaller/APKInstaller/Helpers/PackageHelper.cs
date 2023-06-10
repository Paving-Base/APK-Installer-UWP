using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.System;

namespace APKInstaller.Helpers
{
    internal class PackageHelper
    {
        public static async Task<(bool isfound, IEnumerable<Package> info)> FindPackagesByName(string PackageFamilyName)
        {
            PackageManager manager = new();
            IEnumerable<Package> WSAList = await Task.Run(() =>
            {
                try
                {
                    return manager.FindPackagesForUser("", PackageFamilyName);
                }
                catch
                {
                    return null;
                }
            });
            return (WSAList != null && WSAList.Any(), WSAList);
        }

        public static async void LaunchPackage(string packagefamilyname, string appname = "App") => await CommandHelper.ExecuteShellCommandAsync($@"explorer.exe shell:appsFolder\{packagefamilyname}!{appname}");

        public static async void LaunchWSAPackage(string packagename = "")
        {
            (bool isfound, _) = await FindPackagesByName("MicrosoftCorporationII.WindowsSubsystemForAndroid_8wekyb3d8bbwe");
            if (isfound)
            {
                _ = await Launcher.LaunchUriAsync(new Uri($"wsa://{packagename}"));
            }
        }
    }
}
