using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using APKInstaller.Common;
using APKInstaller.Helpers;
using APKInstaller.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Zeroconf;
using Zeroconf.Interfaces;

namespace APKInstaller.ViewModels.SettingsPages
{
    public partial class PairDeviceViewModel(CoreDispatcher dispatcher) : INotifyPropertyChanged, IDisposable, IAsyncDisposable
    {
        private string ssid;
        private string password;
        private readonly ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse("PairDevicePage");

        private static string ADBPath => SettingsHelper.Get<string>(SettingsHelper.ADBPath);

        public CoreDispatcher Dispatcher { get; } = dispatcher;
        public ResolverListener ConnectListener { get; private set; }
        public string DeviceListFormat => _loader.GetString("DeviceListFormat");
        public string ConnectedListFormat => _loader.GetString("ConnectedListFormat");
        public Action HideQRScanFlyout { get; set; }

        public ObservableCollection<MDNSDeviceData> DeviceList { get; } = [];

        private string _code = string.Empty;
        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        private string _IPAddress = string.Empty;
        public string IPAddress
        {
            get => _IPAddress;
            set => SetProperty(ref _IPAddress, value);
        }

        private List<DeviceData> _connectedList;
        public List<DeviceData> ConnectedList
        {
            get => _connectedList;
            set => SetProperty(ref _connectedList, value);
        }

        private bool _connectInfoIsOpen;
        public bool ConnectInfoIsOpen
        {
            get => _connectInfoIsOpen;
            set => SetProperty(ref _connectInfoIsOpen, value);
        }

        private InfoBarSeverity _connectInfoSeverity;
        public InfoBarSeverity ConnectInfoSeverity
        {
            get => _connectInfoSeverity;
            set => SetProperty(ref _connectInfoSeverity, value);
        }

        private string _connectInfoTitle;
        public string ConnectInfoTitle
        {
            get => _connectInfoTitle;
            set => SetProperty(ref _connectInfoTitle, value);
        }

        private bool _connectingDevice;
        public bool ConnectingDevice
        {
            get => _connectingDevice;
            set => SetProperty(ref _connectingDevice, value);
        }

        private string _connectLogText;
        public string ConnectLogText
        {
            get => _connectLogText;
            set => SetProperty(ref _connectLogText, value);
        }

        private string _QRCodeText;
        public string QRCodeText
        {
            get => _QRCodeText;
            set => SetProperty(ref _QRCodeText, value);
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

        public async void InitializeConnectListener()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (!SettingsHelper.Get<bool>(SettingsHelper.ScanPairedDevice))
            {
                ZeroconfHelper.InitializeConnectListener();
                _ = ZeroconfHelper.ConnectPairedDevice();
            }
            if (ConnectListener == null)
            {
                ConnectListener = ZeroconfResolver.CreateListener("_adb-tls-pairing._tcp.local.");
                ConnectListener.ServiceFound += ConnectListener_ServiceFound;
                ConnectListener.ServiceLost += ConnectListener_ServiceLost;
            }
            if (await ADBHelper.CheckIsRunningAsync().ConfigureAwait(false))
            {
                ADBHelper.Monitor.DeviceListChanged += OnDeviceListChanged;
                ConnectedList = await new AdbClient().GetDevicesAsync().ContinueWith(x => x.Result.Where(x => x.State != DeviceState.Offline).ToList()).ConfigureAwait(false);
            }
        }

        public Task ConnectWithPairingCodeAsync(MDNSDeviceData deviceData) => ConnectWithPairingCodeAsync(deviceData, Code);

        public async Task ConnectWithPairingCodeAsync(MDNSDeviceData deviceData, string code)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (!string.IsNullOrWhiteSpace(code) && deviceData != null)
            {
                try
                {
                    _ = deviceData.SetConnectingDevice(true, Dispatcher);
                    IAdbServer ADBServer = AdbServer.Instance;
                    if (!await ADBHelper.CheckIsRunningAsync().ConfigureAwait(false))
                    {
                        try
                        {
                            await ADBHelper.StartADBAsync(ADBPath).ConfigureAwait(false);
                            ADBHelper.Monitor.DeviceListChanged += OnDeviceListChanged;
                        }
                        catch (Exception ex)
                        {
                            SettingsHelper.LoggerFactory.CreateLogger<PairDeviceViewModel>().LogWarning(ex, "Error starting ADB server. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                            ConnectInfoSeverity = InfoBarSeverity.Warning;
                            ConnectInfoTitle = ResourceLoader.GetForViewIndependentUse("InstallPage").GetString("ADBMissing");
                            ConnectInfoIsOpen = true;
                            return;
                        }
                    }
                    try
                    {
                        ConnectLogText = _loader.GetString("PairingLogText");
                        AdbClient client = new();
                        string pair = await client.PairAsync(deviceData.Address, deviceData.Port, code).ConfigureAwait(false);
                        if (pair.StartsWith("successfully", StringComparison.OrdinalIgnoreCase))
                        {
                            ConnectInfoSeverity = InfoBarSeverity.Success;
                            ConnectInfoTitle = pair;
                            ConnectInfoIsOpen = true;
                            ConnectLogText = _loader.GetString("ConnectingLogText");
                            string connect = await Task.Run(async () =>
                            {
                                using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(10));
                                while (!tokenSource.Token.IsCancellationRequested)
                                {
                                    IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync("_adb-tls-connect._tcp.local.").ConfigureAwait(false);
                                    IZeroconfHost host = hosts.FirstOrDefault((x) => x.IPAddress == deviceData.Address);
                                    if (host != null)
                                    {
                                        string connect = await client.ConnectAsync(host.IPAddress, host.Services.FirstOrDefault().Value.Port).ConfigureAwait(false);
                                        return connect;
                                    }
                                }
                                return string.Empty;
                            }).ConfigureAwait(false);
                            if (connect.StartsWith("connected to", StringComparison.OrdinalIgnoreCase))
                            {
                                ConnectInfoSeverity = InfoBarSeverity.Success;
                                ConnectInfoTitle = pair;
                                ConnectInfoIsOpen = true;
                                ConnectLogText = _loader.GetString("ConnectedLogText");
                            }
                        }
                        else if (pair.StartsWith("failed:", StringComparison.OrdinalIgnoreCase))
                        {
                            ConnectInfoSeverity = InfoBarSeverity.Error;
                            ConnectInfoTitle = pair[8..];
                            ConnectInfoIsOpen = true;
                        }
                        else if (!string.IsNullOrWhiteSpace(pair))
                        {
                            ConnectInfoSeverity = InfoBarSeverity.Warning;
                            ConnectInfoTitle = pair;
                            ConnectInfoIsOpen = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        SettingsHelper.LoggerFactory.CreateLogger<PairDeviceViewModel>().LogWarning(ex, "Error pairing and connecting device. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                        ConnectInfoSeverity = InfoBarSeverity.Error;
                        ConnectInfoTitle = ex.Message;
                        ConnectInfoIsOpen = true;
                    }
                }
                finally
                {
                    _ = deviceData.SetConnectingDevice(false, Dispatcher);
                }
            }
        }

        public async Task ConnectWithPairingCodeAsync(string host, string code)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(code))
            {
                try
                {
                    ConnectingDevice = true;
                    IAdbServer ADBServer = AdbServer.Instance;
                    if (!await ADBHelper.CheckIsRunningAsync().ConfigureAwait(false))
                    {
                        try
                        {
                            await ADBHelper.StartADBAsync(ADBPath).ConfigureAwait(false);
                            ADBHelper.Monitor.DeviceListChanged += OnDeviceListChanged;
                        }
                        catch (Exception ex)
                        {
                            SettingsHelper.LoggerFactory.CreateLogger<PairDeviceViewModel>().LogWarning(ex, "Error starting ADB server. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                            ConnectInfoSeverity = InfoBarSeverity.Warning;
                            ConnectInfoTitle = ResourceLoader.GetForViewIndependentUse("InstallPage").GetString("ADBMissing");
                            ConnectInfoIsOpen = true;
                            return;
                        }
                    }
                    try
                    {
                        ConnectLogText = _loader.GetString("PairingLogText");
                        AdbClient client = new();
                        string pair = await client.PairAsync(host, code).ContinueWith(x => x.Result.TrimStart()).ConfigureAwait(false);
                        if (pair.StartsWith("successfully", StringComparison.OrdinalIgnoreCase))
                        {
                            ConnectInfoSeverity = InfoBarSeverity.Success;
                            ConnectInfoTitle = pair;
                            ConnectInfoIsOpen = true;
                            ConnectLogText = _loader.GetString("ConnectingLogText");
                            string connect = await Task.Run(async () =>
                            {
                                using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(10));
                                while (!tokenSource.Token.IsCancellationRequested)
                                {
                                    IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync("_adb-tls-connect._tcp.local.").ConfigureAwait(false);
                                    IZeroconfHost _host = hosts.FirstOrDefault((x) => host.StartsWith(x.IPAddress));
                                    if (_host != null)
                                    {
                                        string connect = await client.ConnectAsync(_host.IPAddress, _host.Services.FirstOrDefault().Value.Port).ConfigureAwait(false);
                                        return connect;
                                    }
                                }
                                return string.Empty;
                            }).ConfigureAwait(false);
                            if (connect.StartsWith("connected to", StringComparison.OrdinalIgnoreCase))
                            {
                                ConnectInfoSeverity = InfoBarSeverity.Success;
                                ConnectInfoTitle = pair;
                                ConnectInfoIsOpen = true;
                                ConnectLogText = _loader.GetString("ConnectedLogText");
                            }
                        }
                        else if (pair.StartsWith("failed:", StringComparison.OrdinalIgnoreCase))
                        {
                            ConnectInfoSeverity = InfoBarSeverity.Error;
                            ConnectInfoTitle = pair[8..];
                            ConnectInfoIsOpen = true;
                        }
                        else if (!string.IsNullOrWhiteSpace(pair))
                        {
                            ConnectInfoSeverity = InfoBarSeverity.Warning;
                            ConnectInfoTitle = pair;
                            ConnectInfoIsOpen = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        SettingsHelper.LoggerFactory.CreateLogger<PairDeviceViewModel>().LogWarning(ex, "Error pairing and connecting device. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                        ConnectInfoSeverity = InfoBarSeverity.Error;
                        ConnectInfoTitle = ex.Message;
                        ConnectInfoIsOpen = true;
                    }
                }
                finally
                {
                    ConnectingDevice = false;
                }
            }
        }

        public async Task ConnectWithoutPairingCodeAsync(string host)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (!string.IsNullOrWhiteSpace(host))
            {
                try
                {
                    ConnectingDevice = true;
                    IAdbServer ADBServer = AdbServer.Instance;
                    if (!await ADBHelper.CheckIsRunningAsync().ConfigureAwait(false))
                    {
                        try
                        {
                            await ADBHelper.StartADBAsync(ADBPath).ConfigureAwait(false);
                            ADBHelper.Monitor.DeviceListChanged += OnDeviceListChanged;
                        }
                        catch (Exception ex)
                        {
                            SettingsHelper.LoggerFactory.CreateLogger<PairDeviceViewModel>().LogWarning(ex, "Error starting ADB server. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                            ConnectInfoSeverity = InfoBarSeverity.Warning;
                            ConnectInfoTitle = ResourceLoader.GetForViewIndependentUse("InstallPage").GetString("ADBMissing");
                            ConnectInfoIsOpen = true;
                            return;
                        }
                    }
                    try
                    {
                        string results = await new AdbClient().ConnectAsync(host).ContinueWith(x => x.Result.TrimStart()).ConfigureAwait(false);
                        if (results.StartsWith("connected to", StringComparison.OrdinalIgnoreCase))
                        {
                            ConnectInfoSeverity = InfoBarSeverity.Success;
                            ConnectInfoTitle = results;
                            ConnectInfoIsOpen = true;
                        }
                        else if (results.StartsWith("cannot connect to", StringComparison.OrdinalIgnoreCase))
                        {
                            ConnectInfoSeverity = InfoBarSeverity.Error;
                            ConnectInfoTitle = results;
                            ConnectInfoIsOpen = true;
                        }
                        else if (!string.IsNullOrWhiteSpace(results))
                        {
                            ConnectInfoSeverity = InfoBarSeverity.Warning;
                            ConnectInfoTitle = results;
                            ConnectInfoIsOpen = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        SettingsHelper.LoggerFactory.CreateLogger<PairDeviceViewModel>().LogWarning(ex, "Error connecting device. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                        ConnectInfoSeverity = InfoBarSeverity.Error;
                        ConnectInfoTitle = ex.Message;
                        ConnectInfoIsOpen = true;
                    }
                }
                finally
                {
                    ConnectingDevice = false;
                }
            }
        }

        public void InitializeQRScan()
        {
            Random random = new((int)DateTime.Now.Ticks);
            ssid = $"APKInstaller-{random.Next(99999)}{random.Next(99999)}-4v4sx1";
            password = random.Next(999999).ToString();
            QRCodeText = $"WIFI:T:ADB;S:{ssid};P:{password};;";
            ConnectListener.ServiceFound += OnServiceFound;
        }

        public void DisposeQRScan()
        {
            QRCodeText = null;
            ConnectListener.ServiceFound -= OnServiceFound;
        }

        private async void OnServiceFound(object sender, IZeroconfHost e)
        {
            MDNSDeviceData deviceData = new(e);
            if (e.DisplayName == ssid)
            {
                ConnectListener.ServiceFound -= OnServiceFound;
                ConnectingDevice = true;
                await ConnectWithPairingCodeAsync(deviceData, password).ConfigureAwait(false);
                ConnectingDevice = false;
                await Dispatcher.ResumeForegroundAsync();
                HideQRScanFlyout?.Invoke();
            }
        }

        private void ConnectListener_ServiceFound(object sender, IZeroconfHost e)
        {
            bool add = true;
            MDNSDeviceData deviceData = new(e);
            foreach (MDNSDeviceData data in DeviceList)
            {
                if (data.Address == deviceData.Address)
                {
                    _ = Dispatcher.AwaitableRunAsync(() => DeviceList.Remove(data));
                }
            }
            if (add) { _ = Dispatcher.AwaitableRunAsync(() => DeviceList.Add(deviceData)); }
        }

        private void ConnectListener_ServiceLost(object sender, IZeroconfHost e)
        {
            foreach (MDNSDeviceData data in DeviceList)
            {
                if (data.Name == e.DisplayName)
                {
                    _ = Dispatcher.AwaitableRunAsync(() => DeviceList.Remove(data));
                }
            }
        }

        public async void OnDeviceListChanged(object sender, DeviceDataNotifyEventArgs e) =>
            ConnectedList = await new AdbClient().GetDevicesAsync().ContinueWith(x => x.Result.Where(x => x.State != DeviceState.Offline).ToList()).ConfigureAwait(false);

        public void Dispose()
        {
            _ = DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                DeviceList.Clear();
                await ThreadSwitcher.ResumeBackgroundAsync();
                if (ConnectListener != null)
                {
                    ConnectListener.ServiceLost -= ConnectListener_ServiceLost;
                    ConnectListener.ServiceFound -= ConnectListener_ServiceFound;
                    ConnectListener.Dispose();
                }
                if (ADBHelper.IsRunning)
                {
                    ADBHelper.Monitor.DeviceListChanged -= OnDeviceListChanged;
                }
                if (!SettingsHelper.Get<bool>(SettingsHelper.ScanPairedDevice))
                {
                    ZeroconfHelper.DisposeConnectListener();
                }
            }
        }
    }
}
