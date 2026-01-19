using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using APKInstaller.Common;
using APKInstaller.Helpers;
using APKInstaller.Models;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRT;

namespace APKInstaller.ViewModels.SettingsPages
{
    public partial class SettingsViewModel : INotifyPropertyChanged
    {
        private static readonly ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse("SettingsPage");

        public static ConditionalWeakTable<CoreDispatcher, SettingsViewModel> Caches { get; } = [];

        public CoreDispatcher Dispatcher { get; }

        public static GitInfo GitInfo { get; } = new GitInfo("Paving-Base", "APK-Installer", "screenshots", "Documents/Announcements", "Announcements.xaml");

        public static string DeviceFamily { get; } = AnalyticsInfo.VersionInfo.DeviceFamily.Replace('.', ' ');

        public static string WinRTVersion { get; } = Assembly.GetAssembly(typeof(TrustLevel)).GetName().Version.ToString(3);

        public static string ToolkitVersion { get; } = Assembly.GetAssembly(typeof(HsvColor)).GetName().Version.ToString(3);

        public static string SharpAdbClientVersion { get; } = Assembly.GetAssembly(typeof(IAdbClient)).GetName().Version.ToString(3);

        public static string VersionTextBlockText { get; } = $"{ResourceLoader.GetForViewIndependentUse().GetString("AppName") ?? Package.Current.DisplayName} v{Package.Current.Id.Version.ToFormattedString(3)}";

        public static bool IsModified { get; } = Package.Current.PublisherDisplayName != "wherewhere"
            || Package.Current.Id.Name != "18184wherewhere.AndroidAppInstaller.UWP"
            || (Package.Current.Id.PublisherId != "4v4sx105x6y4r" && Package.Current.Id.PublisherId != "d0s2e6z6qkbn0")
            || (Package.Current.Id.Publisher != "CN=2C3A37C0-35FC-4839-B08C-751C1C1AFBF5" && Package.Current.Id.Publisher != "CN=where");

        private DeviceData[] _deviceList;
        public DeviceData[] DeviceList
        {
            get => _deviceList;
            set
            {
                if (_deviceList != value)
                {
                    _deviceList = value;
                    RaisePropertyChangedEvent();
                    if (!IsOnlyWSA) { ChooseDevice(); }
                }
            }
        }

        public bool IsOnlyWSA
        {
            get
            {
                bool value = SettingsHelper.Get<bool>(SettingsHelper.IsOnlyWSA);
                DeviceSelectionMode = value ? ListViewSelectionMode.None : ListViewSelectionMode.Single;
                return value;
            }
            set
            {
                if (IsOnlyWSA != value)
                {
                    SettingsHelper.Set(SettingsHelper.IsOnlyWSA, value);
                    DeviceSelectionMode = value ? ListViewSelectionMode.None : ListViewSelectionMode.Single;
                    RaisePropertyChangedEvent();
                    if (!value) { ChooseDevice(); }
                }
            }
        }

        public bool ScanPairedDevice
        {
            get => SettingsHelper.Get<bool>(SettingsHelper.ScanPairedDevice);
            set
            {
                if (ScanPairedDevice != value)
                {
                    if (value)
                    {
                        ZeroconfHelper.InitializeConnectListener();
                        _ = ZeroconfHelper.ConnectPairedDevice();
                    }
                    else
                    {
                        ZeroconfHelper.DisposeConnectListener();
                    }
                    SettingsHelper.Set(SettingsHelper.ScanPairedDevice, value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        public bool IsCloseADB
        {
            get => SettingsHelper.Get<bool>(SettingsHelper.IsCloseADB);
            set
            {
                if (IsCloseADB != value)
                {
                    SettingsHelper.Set(SettingsHelper.IsCloseADB, value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        public bool IsCloseAPP
        {
            get => SettingsHelper.Get<bool>(SettingsHelper.IsCloseAPP);
            set
            {
                if (IsCloseAPP != value)
                {
                    SettingsHelper.Set(SettingsHelper.IsCloseAPP, value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        public bool ShowDialogs
        {
            get => SettingsHelper.Get<bool>(SettingsHelper.ShowDialogs);
            set
            {
                if (ShowDialogs != value)
                {
                    SettingsHelper.Set(SettingsHelper.ShowDialogs, value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        public bool ShowProgress
        {
            get => SettingsHelper.Get<bool>(SettingsHelper.IsUploadAPK);
            set
            {
                if (ShowProgress != value)
                {
                    SettingsHelper.Set(SettingsHelper.IsUploadAPK, value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        public bool AutoGetNetAPK
        {
            get => SettingsHelper.Get<bool>(SettingsHelper.AutoGetNetAPK);
            set
            {
                if (AutoGetNetAPK != value)
                {
                    SettingsHelper.Set(SettingsHelper.AutoGetNetAPK, value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        public string ADBPath
        {
            get => SettingsHelper.Get<string>(SettingsHelper.ADBPath);
            set
            {
                if (ADBPath != value)
                {
                    SettingsHelper.Set(SettingsHelper.ADBPath, value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        public bool ShowMessages
        {
            get => SettingsHelper.Get<bool>(SettingsHelper.ShowMessages);
            set
            {
                if (ShowMessages != value)
                {
                    SettingsHelper.Set(SettingsHelper.ShowMessages, value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        public DateTimeOffset UpdateDate
        {
            get => SettingsHelper.Get<DateTimeOffset>(SettingsHelper.UpdateDate);
            set
            {
                if (UpdateDate != value)
                {
                    SettingsHelper.Set(SettingsHelper.UpdateDate, value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        public int SelectedTheme
        {
            get => 2 - (int)ThemeHelper.RootTheme;
            set
            {
                if (SelectedTheme != value)
                {
                    ThemeHelper.RootTheme = (ElementTheme)(2 - value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        private static bool _checkingUpdate;
        public bool CheckingUpdate
        {
            get => _checkingUpdate;
            set => SetProperty(ref _checkingUpdate, value);
        }

        private static string _gotoUpdateTag;
        public string GotoUpdateTag
        {
            get => _gotoUpdateTag;
            set => SetProperty(ref _gotoUpdateTag, value);
        }

        private static Visibility _gotoUpdateVisibility = Visibility.Collapsed;
        public Visibility GotoUpdateVisibility
        {
            get => _gotoUpdateVisibility;
            set => SetProperty(ref _gotoUpdateVisibility, value);
        }

        private static bool _updateStateIsOpen;
        public bool UpdateStateIsOpen
        {
            get => _updateStateIsOpen;
            set => SetProperty(ref _updateStateIsOpen, value);
        }

        private static string _updateStateMessage;
        public string UpdateStateMessage
        {
            get => _updateStateMessage;
            set => SetProperty(ref _updateStateMessage, value);
        }

        private static InfoBarSeverity _updateStateSeverity = InfoBarSeverity.Success;
        public InfoBarSeverity UpdateStateSeverity
        {
            get => _updateStateSeverity;
            set => SetProperty(ref _updateStateSeverity, value);
        }

        private static string _updateStateTitle;
        public string UpdateStateTitle
        {
            get => _updateStateTitle;
            set => SetProperty(ref _updateStateTitle, value);
        }

        private static bool _pairingDevice;
        public bool PairingDevice
        {
            get => _pairingDevice;
            set => SetProperty(ref _pairingDevice, value);
        }

        private static bool _connectingDevice;
        public bool ConnectingDevice
        {
            get => _connectingDevice;
            set => SetProperty(ref _connectingDevice, value);
        }

        private static bool _connectInfoIsOpen;
        public bool ConnectInfoIsOpen
        {
            get => _connectInfoIsOpen;
            set => SetProperty(ref _connectInfoIsOpen, value);
        }

        private static InfoBarSeverity _connectInfoSeverity = InfoBarSeverity.Success;
        public InfoBarSeverity ConnectInfoSeverity
        {
            get => _connectInfoSeverity;
            set => SetProperty(ref _connectInfoSeverity, value);
        }

        private static string _connectInfoTitle;
        public string ConnectInfoTitle
        {
            get => _connectInfoTitle;
            set => SetProperty(ref _connectInfoTitle, value);
        }

        private static string _ADBVersion;
        public string ADBVersion
        {
            get => _ADBVersion;
            set => SetProperty(ref _ADBVersion, value);
        }

        private static string _aboutTextBlockText;
        public string AboutTextBlockText
        {
            get => _aboutTextBlockText;
            set => SetProperty(ref _aboutTextBlockText, value);
        }

        private static ListViewSelectionMode _deviceSelectionMode = ListViewSelectionMode.None;
        public ListViewSelectionMode DeviceSelectionMode
        {
            get => _deviceSelectionMode;
            set => SetProperty(ref _deviceSelectionMode, value);
        }

        private static DeviceData _selectedDevice;
        public DeviceData SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected static async void RaisePropertyChangedEvent([CallerMemberName] string name = null)
        {
            if (name != null)
            {
                foreach (KeyValuePair<CoreDispatcher, SettingsViewModel> cache in Caches)
                {
                    await cache.Key.ResumeForegroundAsync();
                    cache.Value.PropertyChanged?.Invoke(cache.Value, new PropertyChangedEventArgs(name));
                }
            }
        }

        protected static async void RaisePropertyChangedEvent(params string[] names)
        {
            if (names?.Length > 0)
            {
                foreach (KeyValuePair<CoreDispatcher, SettingsViewModel> cache in Caches)
                {
                    await cache.Key.ResumeForegroundAsync();
                    names.ForEach(name => cache.Value.PropertyChanged?.Invoke(cache.Value, new PropertyChangedEventArgs(name)));
                }
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

        public static List<HyperlinkContent> ConnectHelpers
        {
            get
            {
                string langCode = LanguageHelper.GetPrimaryLanguage();
                ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse("InstallPage");
                List<HyperlinkContent> values =
                [
                    new(_loader.GetString("NoDevice10"), new Uri($"https://github.com/Paving-Base/APK-Installer/blob/screenshots/Documents/Tutorials/How%20To%20Connect%20Device/How%20To%20Connect%20Device.{langCode}.md")),
                    new(_loader.GetString("HowToConnect"), new Uri($"https://github.com/Paving-Base/APK-Installer/blob/screenshots/Documents/Tutorials/How%20To%20Connect%20WSA/How%20To%20Connect%20WSA.{langCode}.md"))
                ];
                return values;
            }
        }

        public async Task GetADBVersionAsync()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            string version = "Not Running";
            if (await ADBHelper.CheckFileExistsAsync(ADBPath).ConfigureAwait(false))
            {
                AdbServerStatus info = await AdbServer.Instance.GetStatusAsync(default).ConfigureAwait(false);
                if (info.IsRunning)
                {
                    version = info.Version.ToString(3);
                }
            }
            ADBVersion = version;
        }

        private async Task GetAboutTextBlockTextAsync(bool reset)
        {
            if (reset || string.IsNullOrWhiteSpace(_aboutTextBlockText))
            {
                await ThreadSwitcher.ResumeBackgroundAsync();
                string langCode = LanguageHelper.GetPrimaryLanguage();
                Uri dataUri = new($"ms-appx:///Assets/About/About.{langCode}.md");
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
                if (file != null)
                {
                    string markdown = await FileIO.ReadTextAsync(file);
                    AboutTextBlockText = markdown;
                }
            }
        }

        public SettingsViewModel(CoreDispatcher dispatcher)
        {
            Dispatcher = dispatcher;
            Caches.AddOrUpdate(dispatcher, this);
        }

        public async Task RegisterDeviceMonitor()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (await ADBHelper.CheckIsRunningAsync().ConfigureAwait(false))
            {
                ADBHelper.Monitor.DeviceListChanged -= OnDeviceListChanged;
                ADBHelper.Monitor.DeviceListChanged += OnDeviceListChanged;
            }
        }

        public async Task UnregisterDeviceMonitor()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (await ADBHelper.CheckIsRunningAsync().ConfigureAwait(false))
            { ADBHelper.Monitor.DeviceListChanged -= OnDeviceListChanged; }
        }

        private async void OnDeviceListChanged(object sender, DeviceDataNotifyEventArgs e) => DeviceList = await new AdbClient().GetDevicesAsync().ContinueWith(x => x.Result.Where(x => x.State != DeviceState.Offline).ToArray()).ConfigureAwait(false);

        public async Task CheckUpdateAsync()
        {
            try
            {
                CheckingUpdate = true;
                await ThreadSwitcher.ResumeBackgroundAsync();
                UpdateInfo info = null;
                try
                {
                    info = await UpdateHelper.CheckUpdateAsync("Paving-Base", "APK-Installer-UWP").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    UpdateStateIsOpen = true;
                    UpdateStateMessage = ex.Message;
                    UpdateStateSeverity = InfoBarSeverity.Error;
                    GotoUpdateVisibility = Visibility.Collapsed;
                    UpdateStateTitle = _loader.GetString("CheckFailed");
                }
                if (info != null)
                {
                    if (info.IsExistNewVersion)
                    {
                        UpdateStateIsOpen = true;
                        GotoUpdateTag = info.ReleaseUrl;
                        GotoUpdateVisibility = Visibility.Visible;
                        UpdateStateSeverity = InfoBarSeverity.Warning;
                        UpdateStateTitle = _loader.GetString("FindUpdate");
                        UpdateStateMessage = $"{VersionTextBlockText} -> {info.TagName}";
                    }
                    else
                    {
                        UpdateStateIsOpen = true;
                        GotoUpdateVisibility = Visibility.Collapsed;
                        UpdateStateSeverity = InfoBarSeverity.Success;
                        UpdateStateTitle = _loader.GetString("UpToDate");
                    }
                }
                UpdateDate = DateTimeOffset.Now;
            }
            finally
            {
                CheckingUpdate = false;
            }
        }

        public void ChooseDevice()
        {
            DeviceData device = SettingsHelper.Get<DeviceData>(SettingsHelper.DefaultDevice);
            if (device is null) { return; }
            foreach (DeviceData data in DeviceList)
            {
                if (data.Name == device.Name && data.Model == device.Model && data.Product == device.Product)
                {
                    SelectedDevice = data;
                    break;
                }
            }
        }

        public async Task ConnectDeviceAsync(string ip)
        {
            try
            {
                ConnectingDevice = true;
                await ThreadSwitcher.ResumeBackgroundAsync();
                IAdbServer ADBServer = AdbServer.Instance;
                if (!await ADBServer.GetStatusAsync(default).ContinueWith(x => x.Result.IsRunning).ConfigureAwait(false))
                {
                    try
                    {
                        await ADBHelper.StartADBAsync(ADBPath).ConfigureAwait(false);
                        ADBHelper.Monitor.DeviceListChanged -= OnDeviceListChanged;
                        ADBHelper.Monitor.DeviceListChanged += OnDeviceListChanged;
                    }
                    catch (Exception ex)
                    {
                        SettingsHelper.LoggerFactory.CreateLogger<SettingsViewModel>().LogWarning(ex, "Error starting ADB server. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                        ConnectInfoSeverity = InfoBarSeverity.Warning;
                        ConnectInfoTitle = ResourceLoader.GetForViewIndependentUse("InstallPage").GetString("ADBMissing");
                        ConnectInfoIsOpen = true;
                        ConnectingDevice = false;
                        return;
                    }
                }
                try
                {
                    string results = await new AdbClient().ConnectAsync(ip).ContinueWith(x => x.Result.TrimStart()).ConfigureAwait(false);
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
                    SettingsHelper.LoggerFactory.CreateLogger<SettingsViewModel>().LogWarning(ex, "Error connecting device. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
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

        public async Task PairDeviceAsync(string ip, string code)
        {
            try
            {
                PairingDevice = true;
                await ThreadSwitcher.ResumeBackgroundAsync();
                IAdbServer ADBServer = AdbServer.Instance;
                if (!await ADBServer.GetStatusAsync(default).ContinueWith(x => x.Result.IsRunning).ConfigureAwait(false))
                {
                    try
                    {
                        await ADBHelper.StartADBAsync(ADBPath).ConfigureAwait(false);
                        ADBHelper.Monitor.DeviceListChanged -= OnDeviceListChanged;
                        ADBHelper.Monitor.DeviceListChanged += OnDeviceListChanged;
                    }
                    catch (Exception ex)
                    {
                        SettingsHelper.LoggerFactory.CreateLogger<SettingsViewModel>().LogWarning(ex, "Error starting ADB server. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                        ConnectInfoSeverity = InfoBarSeverity.Warning;
                        ConnectInfoTitle = ResourceLoader.GetForViewIndependentUse("InstallPage").GetString("ADBMissing");
                        ConnectInfoIsOpen = true;
                        PairingDevice = false;
                        return;
                    }
                }
                try
                {
                    string results = await new AdbClient().PairAsync(ip, code).ContinueWith(x => x.Result.TrimStart()).ConfigureAwait(false);
                    if (results.StartsWith("successfully", StringComparison.OrdinalIgnoreCase))
                    {
                        ConnectInfoSeverity = InfoBarSeverity.Success;
                        ConnectInfoTitle = results;
                        ConnectInfoIsOpen = true;
                    }
                    else if (results.StartsWith("failed:", StringComparison.OrdinalIgnoreCase))
                    {
                        ConnectInfoSeverity = InfoBarSeverity.Error;
                        ConnectInfoTitle = results[8..];
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
                    SettingsHelper.LoggerFactory.CreateLogger<SettingsViewModel>().LogWarning(ex, "Error pairing device. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                    ConnectInfoSeverity = InfoBarSeverity.Error;
                    ConnectInfoTitle = ex.Message;
                    ConnectInfoIsOpen = true;
                }
            }
            finally
            {
                PairingDevice = false;
            }
        }

        public async Task ChangeADBPathAsync()
        {
            await Dispatcher.ResumeForegroundAsync();

            FileOpenPicker FileOpen = new()
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            FileOpen.FileTypeFilter.Add(".exe");

            StorageFile file = await FileOpen.PickSingleFileAsync();
            if (file != null)
            {
                ADBPath = file.Path;
            }
        }

        public async Task Refresh(bool reset)
        {
            if (reset)
            {
                RaisePropertyChangedEvent(
                    nameof(IsOnlyWSA),
                    nameof(ScanPairedDevice),
                    nameof(IsCloseADB),
                    nameof(IsCloseAPP),
                    nameof(ShowDialogs),
                    nameof(ShowProgress),
                    nameof(AutoGetNetAPK),
                    nameof(ADBPath),
                    nameof(ShowMessages),
                    nameof(UpdateDate),
                    nameof(SelectedTheme));
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            if (await ADBHelper.CheckIsRunningAsync().ConfigureAwait(false))
            {
                DeviceList = await new AdbClient().GetDevicesAsync().ContinueWith(x => x.Result.Where(x => x.State != DeviceState.Offline).ToArray()).ConfigureAwait(false);
            }

            await GetAboutTextBlockTextAsync(reset).ConfigureAwait(false);
            await GetADBVersionAsync().ConfigureAwait(false);

            if (UpdateDate == default)
            {
                await CheckUpdateAsync().ConfigureAwait(false);
            }
        }
    }

    public record struct HyperlinkContent(string Content, Uri NavigateUri);
}
