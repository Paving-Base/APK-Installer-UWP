using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.DeviceCommands.Models;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using APKInstaller.Common;
using APKInstaller.Controls;
using APKInstaller.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace APKInstaller.ViewModels.ToolsPages
{
    public partial class ApplicationsViewModel(CoreDispatcher dispatcher) : INotifyPropertyChanged
    {
        public TitleBar TitleBar;
        public ComboBox DeviceComboBox;
        public List<DeviceData> devices;

        public CoreDispatcher Dispatcher { get; } = dispatcher;

        private ObservableCollection<string> deviceList = [];
        public ObservableCollection<string> DeviceList
        {
            get => deviceList;
            set => SetProperty(ref deviceList, value);
        }

        private ObservableCollection<APKInfo> applications = [];
        public ObservableCollection<APKInfo> Applications
        {
            get => applications;
            set => SetProperty(ref applications, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected async void RaisePropertyChangedEvent([CallerMemberName] string name = null)
        {
            if (name != null)
            {
                await Dispatcher.ResumeForegroundAsync();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        protected void SetProperty<TProperty>(ref TProperty property, TProperty value, [CallerMemberName] string name = null)
        {
            if (property == null ? value != null : !property.Equals(value))
            {
                property = value;
                RaisePropertyChangedEvent(name);
            }
        }

        public async Task GetDevicesAsync()
        {
            try
            {
                await ThreadSwitcher.ResumeBackgroundAsync();
                devices = await new AdbClient().GetDevicesAsync().ContinueWith(x => x.Result.Where(x => x.State == DeviceState.Online).ToList()).ConfigureAwait(false);
                await Dispatcher.AwaitableRunAsync(DeviceList.Clear);
                if (devices.Count > 0)
                {
                    foreach (DeviceData device in devices)
                    {
                        if (!string.IsNullOrEmpty(device.Name))
                        {
                            await Dispatcher.AwaitableRunAsync(() => DeviceList.Add(device.Name));
                        }
                        else if (!string.IsNullOrEmpty(device.Model))
                        {
                            await Dispatcher.AwaitableRunAsync(() => DeviceList.Add(device.Model));
                        }
                        else if (!string.IsNullOrEmpty(device.Product))
                        {
                            await Dispatcher.AwaitableRunAsync(() => DeviceList.Add(device.Product));
                        }
                        else if (!string.IsNullOrEmpty(device.Serial))
                        {
                            await Dispatcher.AwaitableRunAsync(() => DeviceList.Add(device.Serial));
                        }
                        else
                        {
                            await Dispatcher.AwaitableRunAsync(() => DeviceList.Add("Device"));
                        }
                    }
                    await Dispatcher.AwaitableRunAsync(() =>
                    {
                        if (DeviceComboBox.SelectedIndex == -1)
                        {
                            DeviceComboBox.SelectedIndex = 0;
                        }
                    });
                }
                else
                {
                    await Dispatcher.AwaitableRunAsync(() =>
                    {
                        DeviceComboBox.SelectedIndex = -1;
                        Applications.Clear();
                    });
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LoggerFactory.CreateLogger<ApplicationsViewModel>().LogError(ex, "Error getting devices. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
            }
        }

        public async Task CheckAPPAsync(Dictionary<string, string> apps, int index)
        {
            try
            {
                await ThreadSwitcher.ResumeBackgroundAsync();
                await Dispatcher.AwaitableRunAsync(Applications.Clear);
                AdbClient client = new();
                PackageManager manager = new(client, devices[index]);
                int count = 0;
                foreach (KeyValuePair<string, string> app in apps)
                {
                    _ = Dispatcher.AwaitableRunAsync(() => TitleBar.SetProgressValue(++count * 100 / apps.Count));
                    if (!string.IsNullOrEmpty(app.Key))
                    {
                        ConsoleOutputReceiver receiver = new();
                        await client.ExecuteRemoteCommandAsync($"pidof {app.Key}", devices[index], receiver).ConfigureAwait(false);
                        bool isActive = !string.IsNullOrEmpty(receiver.ToString());
                        VersionInfo version = await manager.GetVersionInfoAsync(app.Key).ConfigureAwait(false);
                        await Dispatcher.AwaitableRunAsync(() =>
                        {
                            FontIcon source = new() { Glyph = "\xECAA" };
                            Applications.Add(new APKInfo
                            {
                                Name = app.Key,
                                Icon = source,
                                IsActive = isActive,
                                VersionInfo = version,
                            });
                        });
                    }
                };
            }
            catch (Exception ex)
            {
                SettingsHelper.LoggerFactory.CreateLogger<ApplicationsViewModel>().LogError(ex, "Error checking applications. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
            }
        }

        public async Task GetAppsAsync()
        {
            try
            {
                await ThreadSwitcher.ResumeBackgroundAsync();
                if (devices?.Count > 0)
                {
                    _ = Dispatcher.AwaitableRunAsync(() =>
                    {
                        TitleBar.ShowProgressRing();
                        TitleBar.IsRefreshButtonVisible = false;
                    });
                    AdbClient client = new();
                    int index = await Dispatcher.AwaitableRunAsync(() => { return DeviceComboBox.SelectedIndex; });
                    PackageManager manager = new(client, devices[index]);
                    await CheckAPPAsync(manager.Packages, index).ConfigureAwait(false);
                    _ = Dispatcher.AwaitableRunAsync(() =>
                    {
                        TitleBar.IsRefreshButtonVisible = true;
                        TitleBar.HideProgressRing();
                    });
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LoggerFactory.CreateLogger<ApplicationsViewModel>().LogError(ex, "Error getting applications. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                _ = Dispatcher.AwaitableRunAsync(() =>
                {
                    TitleBar.IsRefreshButtonVisible = true;
                    TitleBar.HideProgressRing();
                });
            }
        }
    }

    public partial class ApplicationConverter : IValueConverter
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
