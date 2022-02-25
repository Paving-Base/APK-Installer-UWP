﻿using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using APKInstaller.Controls;
using APKInstaller.Helpers;
using APKInstaller.Pages.ToolsPages;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace APKInstaller.ViewModels.ToolsPages
{
    public class ApplicationsViewModel : INotifyPropertyChanged
    {
        public TitleBar TitleBar;
        public ComboBox DeviceComboBox;
        public List<DeviceData> devices;
        private readonly ApplicationsPage _page;

        private List<string> deviceList = new List<string>();
        public List<string> DeviceList
        {
            get => deviceList;
            set
            {
                deviceList = value;
                RaisePropertyChangedEvent();
            }
        }

        private List<APKInfo> applications;
        public List<APKInfo> Applications
        {
            get => applications;
            set
            {
                applications = value;
                RaisePropertyChangedEvent();
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

        public void GetDevices()
        {
            devices = new AdvancedAdbClient().GetDevices();
            DeviceList.Clear();
            if (devices.Count > 0)
            {
                foreach (DeviceData device in devices)
                {
                    if (!string.IsNullOrEmpty(device.Name))
                    {
                        DeviceList.Add(device.Name);
                    }
                    else if (!string.IsNullOrEmpty(device.Model))
                    {
                        DeviceList.Add(device.Model);
                    }
                    else if (!string.IsNullOrEmpty(device.Product))
                    {
                        DeviceList.Add(device.Product);
                    }
                    else if (!string.IsNullOrEmpty(device.Serial))
                    {
                        DeviceList.Add(device.Serial);
                    }
                    else
                    {
                        DeviceList.Add("Device");
                    }
                }
                DeviceComboBox.ItemsSource = DeviceList;
                if (DeviceComboBox.SelectedIndex == -1)
                {
                    DeviceComboBox.SelectedIndex = 0;
                }
            }
            else if (Applications != null)
            {
                Applications.Clear();
            }
        }

        public List<APKInfo> CheckAPP(Dictionary<string, string> apps, int index)
        {
            List<APKInfo> Applications = new List<APKInfo>();
            AdvancedAdbClient client = new AdvancedAdbClient();
            PackageManager manager = new PackageManager(client, devices[index]);
            foreach (KeyValuePair<string, string> app in apps)
            {
                _ = UIHelper.DispatcherQueue.EnqueueAsync(() => TitleBar.SetProgressValue((double)apps.ToList().IndexOf(app) * 100 / apps.Count));
                if (!string.IsNullOrEmpty(app.Key))
                {
                    ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
                    client.ExecuteRemoteCommand($"pidof {app.Key}", devices[index], receiver);
                    bool isactive = !string.IsNullOrEmpty(receiver.ToString());
                    Applications.Add(new APKInfo()
                    {
                        Name = app.Key,
                        IsActive = isactive,
                        VersionInfo = manager.GetVersionInfo(app.Key),
                    });
                }
            }
            return Applications;
        }

        public async Task Refresh()
        {
            TitleBar.ShowProgressRing();
            GetDevices();
            int index = DeviceComboBox.SelectedIndex;
            PackageManager manager = new PackageManager(new AdvancedAdbClient(), devices[DeviceComboBox.SelectedIndex]);
            Applications = await Task.Run(() => { return CheckAPP(manager.Packages, index); });
            TitleBar.HideProgressRing();
        }
    }

    internal class ApplicationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch ((string)parameter)
            {
                case "State": return (bool)value ? "Running" : "Stop";
                default: return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => (Visibility)value == Visibility.Visible;
    }

    public class APKInfo
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public VersionInfo VersionInfo { get; set; }
    }
}
