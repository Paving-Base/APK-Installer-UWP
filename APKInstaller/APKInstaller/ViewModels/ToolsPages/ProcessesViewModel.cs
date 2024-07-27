using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.DeviceCommands.Models;
using AdvancedSharpAdbClient.Models;
using APKInstaller.Common;
using APKInstaller.Controls;
using APKInstaller.Helpers;
using Microsoft.Toolkit.Uwp.Helpers;
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
    public class ProcessesViewModel(CoreDispatcher dispatcher) : INotifyPropertyChanged
    {
        public TitleBar TitleBar;
        public ComboBox DeviceComboBox;
        public List<DeviceData> devices;

        public string CachedSortedColumn { get; set; }
        public CoreDispatcher Dispatcher { get; } = dispatcher;

        private ObservableCollection<string> deviceList = [];
        public ObservableCollection<string> DeviceList
        {
            get => deviceList;
            set => SetProperty(ref deviceList, value);
        }

        private ObservableCollection<AndroidProcess> processes = [];
        public ObservableCollection<AndroidProcess> Processes
        {
            get => processes;
            set => SetProperty(ref processes, value);
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
                    await Dispatcher.ResumeForegroundAsync();
                    if (DeviceComboBox.SelectedIndex == -1)
                    {
                        DeviceComboBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    await Dispatcher.ResumeForegroundAsync();
                    DeviceComboBox.SelectedIndex = -1;
                    Processes.Clear();
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(ProcessesViewModel)).Error(ex.ExceptionToMessage());
            }
        }

        public async Task SortDataAsync(string sortBy, bool ascending)
        {
            try
            {
                await ThreadSwitcher.ResumeBackgroundAsync();
                CachedSortedColumn = sortBy;
                switch (sortBy)
                {
                    case "Name":
                        static string GetName(AndroidProcess item) => item.Name.Split('/').LastOrDefault()?.Split(':').FirstOrDefault()?.Split('@').FirstOrDefault();
                        Processes = ascending
                            ? new(Processes.OrderBy(GetName))
                            : new(Processes.OrderByDescending(GetName));
                        break;
                    case "ProcessId":
                        Processes = ascending
                            ? new(Processes.OrderBy(item => item.ProcessId))
                            : new(Processes.OrderByDescending(item => item.ProcessId));
                        break;
                    case "State":
                        Processes = ascending
                            ? new(Processes.OrderBy(item => item.State))
                            : new(Processes.OrderByDescending(item => item.State));
                        break;
                    case "ResidentSetSize":
                        Processes = ascending
                            ? new(Processes.OrderBy(item => item.ResidentSetSize))
                            : new(Processes.OrderByDescending(item => item.ResidentSetSize));
                        break;
                    case "Detail":
                        Processes = ascending
                            ? new(Processes.OrderBy(item => item.Name))
                            : new(Processes.OrderByDescending(item => item.Name));
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(ProcessesViewModel)).Error(ex.ExceptionToMessage());
            }
        }

        public async Task GetProcessAsync()
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
                    IEnumerable<AndroidProcess> list = await client.ListProcessesAsync(devices[index]);
                    Processes = new(list);
                    _ = Dispatcher.AwaitableRunAsync(() =>
                    {
                        TitleBar.IsRefreshButtonVisible = true;
                        TitleBar.HideProgressRing();
                    });
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(ProcessesViewModel)).Error(ex.ExceptionToMessage());
                _ = Dispatcher.AwaitableRunAsync(() =>
                {
                    TitleBar.IsRefreshButtonVisible = true;
                    TitleBar.HideProgressRing();
                });
            }
        }
    }

    public class ProcessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (string)parameter switch
            {
                "Size" => ((double)(int)value).GetSizeString(),
                "Name" => ((string)value).Split('/').Last().Split(':').First().Split('@').First(),
                "State" => (AndroidProcessState)value switch
                {
                    AndroidProcessState.Unknown => "Unknown",
                    AndroidProcessState.D => "Sleep(D)",
                    AndroidProcessState.R => "Running",
                    AndroidProcessState.S => "Sleep(S)",
                    AndroidProcessState.T => "Stopped",
                    AndroidProcessState.W => "Paging",
                    AndroidProcessState.X => "Dead",
                    AndroidProcessState.Z => "Defunct",
                    AndroidProcessState.K => "Wakekill",
                    AndroidProcessState.P => "Parked",
                    _ => value.ToString(),
                },
                _ => value.ToString(),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => (Visibility)value == Visibility.Visible;
    }
}
