using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using APKInstaller.Controls;
using APKInstaller.Helpers;
using APKInstaller.Pages.ToolsPages;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using File = System.IO.File;

namespace APKInstaller.ViewModels.ToolsPages
{
    public class ApplicationsViewModel : INotifyPropertyChanged
    {
        public TitleBar TitleBar;
        public ComboBox DeviceComboBox;
        public List<DeviceData> devices;
        private readonly ApplicationsPage _page;

        private List<string> deviceList = new();
        public List<string> DeviceList
        {
            get => deviceList;
            set
            {
                if (deviceList != value)
                {
                    deviceList = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private List<APKInfo> applications;
        public List<APKInfo> Applications
        {
            get => applications;
            set
            {
                if (applications != value)
                {
                    applications = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (name != null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }

        public ApplicationsViewModel(ApplicationsPage page)
        {
            _page = page;
        }

        public async Task GetDevices()
        {
            await Task.Run(async () =>
            {
                _ = (UIHelper.DispatcherQueue?.EnqueueAsync(TitleBar.ShowProgressRing));
                devices = new AdvancedAdbClient().GetDevices();
                await UIHelper.DispatcherQueue?.EnqueueAsync(DeviceList.Clear);
                if (devices.Count > 0)
                {
                    foreach (DeviceData device in devices)
                    {
                        if (!string.IsNullOrEmpty(device.Name))
                        {
                            await UIHelper.DispatcherQueue?.EnqueueAsync(() => DeviceList.Add(device.Name));
                        }
                        else if (!string.IsNullOrEmpty(device.Model))
                        {
                            await UIHelper.DispatcherQueue?.EnqueueAsync(() => DeviceList.Add(device.Model));
                        }
                        else if (!string.IsNullOrEmpty(device.Product))
                        {
                            await UIHelper.DispatcherQueue?.EnqueueAsync(() => DeviceList.Add(device.Product));
                        }
                        else if (!string.IsNullOrEmpty(device.Serial))
                        {
                            await UIHelper.DispatcherQueue?.EnqueueAsync(() => DeviceList.Add(device.Serial));
                        }
                        else
                        {
                            await UIHelper.DispatcherQueue?.EnqueueAsync(() => DeviceList.Add("Device"));
                        }
                    }
                    await UIHelper.DispatcherQueue?.EnqueueAsync(() => { DeviceComboBox.ItemsSource = DeviceList; if (DeviceComboBox.SelectedIndex == -1) { DeviceComboBox.SelectedIndex = 0; } });
                }
                else if (Applications != null)
                {
                    await UIHelper.DispatcherQueue?.EnqueueAsync(() => Applications = null);
                }
                _ = (UIHelper.DispatcherQueue?.EnqueueAsync(TitleBar.HideProgressRing));
            });
        }

        public async Task<List<APKInfo>> CheckAPP(Dictionary<string, string> apps, int index)
        {
            List<APKInfo> Applications = new();
            await Task.Run(async () =>
            {
                AdvancedAdbClient client = new();
                PackageManager manager = new(client, devices[index]);
                foreach (KeyValuePair<string, string> app in apps)
                {
                    _ = UIHelper.DispatcherQueue?.EnqueueAsync(() => TitleBar.SetProgressValue((double)apps.ToList().IndexOf(app) * 100 / apps.Count));
                    if (!string.IsNullOrEmpty(app.Key))
                    {
                        ConsoleOutputReceiver receiver = new();
                        client.ExecuteRemoteCommand($"pidof {app.Key}", devices[index], receiver);
                        bool isactive = !string.IsNullOrEmpty(receiver.ToString());
                        FontIcon source = await UIHelper.DispatcherQueue.EnqueueAsync(() => { return new FontIcon { Glyph = "\xECAA" }; });
                        Applications.Add(new APKInfo
                        {
                            Name = app.Key,
                            Icon = source,
                            IsActive = isactive,
                            VersionInfo = manager.GetVersionInfo(app.Key),
                        });
                    }
                }
            });
            return Applications;
        }

        public async Task GetApps()
        {
            await Task.Run(async () =>
            {
                _ = (UIHelper.DispatcherQueue?.EnqueueAsync(TitleBar.ShowProgressRing));
                AdvancedAdbClient client = new();
                int index = await UIHelper.DispatcherQueue?.EnqueueAsync(() => { return DeviceComboBox.SelectedIndex; });
                PackageManager manager = new(new AdvancedAdbClient(), devices[index]);
                List<APKInfo> list = await CheckAPP(manager.Packages, index);
                await UIHelper.DispatcherQueue?.EnqueueAsync(() => Applications = list);
                _ = (UIHelper.DispatcherQueue?.EnqueueAsync(TitleBar.HideProgressRing));
            });
        }
    }

    internal class ApplicationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (string)parameter switch
            {
                "State" => (bool)value ? "Running" : "Stop",
                _ => value.ToString(),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => (Visibility)value == Visibility.Visible;
    }

    public class APKInfo
    {
        public string Name { get; set; }
        public IconElement Icon { get; set; }
        public string PackageName { get; set; }
        public bool IsActive { get; set; }
        public VersionInfo VersionInfo { get; set; }
    }
}
