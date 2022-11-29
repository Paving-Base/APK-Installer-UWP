﻿using AAPTForUWP;
using AAPTForUWP.Models;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using APKInstaller.Controls.Dialogs;
using APKInstaller.Helpers;
using APKInstaller.Models;
using APKInstaller.Pages;
using APKInstaller.Pages.SettingsPages;
using Downloader;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Connectivity;
using ProcessForUWP.UWP;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;

namespace APKInstaller.ViewModels
{
    public class InstallViewModel : INotifyPropertyChanged, IDisposable
    {
        private InstallPage _page;
        private DeviceData _device;
        private readonly ProtocolForResultsOperation _operation;
        private static readonly string APKTemp = Path.Combine(CachesHelper.TempPath, "NetAPKTemp.apk");
        private static readonly string ADBTemp = Path.Combine(CachesHelper.TempPath, "platform-tools.zip");

#if !DEBUG
        private Uri _url;
        private string _path = string.Empty;
#else
        private Uri _url = new("apkinstaller:?source=https://dl.coolapk.com/down?pn=com.coolapk.market&id=NDU5OQ&h=46bb9d98&from=from-web");
        private string _path = @"C:\Users\qq251\Downloads\Programs\Minecraft_1.18.2.03_sign.apk";
#endif
        private bool NetAPKExist => _path != APKTemp || File.Exists(_path);

        private bool _disposedValue;
        private readonly ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse("InstallPage");

        public static InstallViewModel Caches;

        public string InstallFormat => _loader.GetString("InstallFormat");
        public string VersionFormat => _loader.GetString("VersionFormat");
        public string PackageNameFormat => _loader.GetString("PackageNameFormat");

        private static bool IsOnlyWSA => SettingsHelper.Get<bool>(SettingsHelper.IsOnlyWSA);
        private static bool IsCloseAPP => SettingsHelper.Get<bool>(SettingsHelper.IsCloseAPP);
        private static bool ShowDialogs => SettingsHelper.Get<bool>(SettingsHelper.ShowDialogs);
        private static bool AutoGetNetAPK => SettingsHelper.Get<bool>(SettingsHelper.AutoGetNetAPK);

        private ApkInfo _apkInfo = null;
        public ApkInfo ApkInfo
        {
            get => _apkInfo;
            set
            {
                if (_apkInfo != value)
                {
                    _apkInfo = value;
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

        public static bool IsOpenApp
        {
            get => SettingsHelper.Get<bool>(SettingsHelper.IsOpenApp);
            set => SettingsHelper.Set(SettingsHelper.IsOpenApp, value);
        }

        private bool _isInstalling;
        public bool IsInstalling
        {
            get => _isInstalling;
            set
            {
                if (_isInstalling != value)
                {
                    _isInstalling = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private bool _isInitialized;
        public bool IsInitialized
        {
            get => _isInitialized;
            set
            {
                if (_isInitialized != value)
                {
                    _isInitialized = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _appName;
        public string AppName
        {
            get => _appName;
            set
            {
                if (_appName != value)
                {
                    _appName = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _textOutput;
        public string TextOutput
        {
            get => _textOutput;
            set
            {
                if (_textOutput != value)
                {
                    _textOutput = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _infoMessage;
        public string InfoMessage
        {
            get => _infoMessage;
            set
            {
                if (_infoMessage != value)
                {
                    _infoMessage = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _progressText;
        public string ProgressText
        {
            get => _progressText;
            set
            {
                if (_progressText != value)
                {
                    _progressText = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private bool _actionButtonEnable;
        public bool ActionButtonEnable
        {
            get => _actionButtonEnable;
            set
            {
                if (_actionButtonEnable != value)
                {
                    _actionButtonEnable = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private bool _secondaryActionButtonEnable;
        public bool SecondaryActionButtonEnable
        {
            get => _secondaryActionButtonEnable;
            set
            {
                if (_secondaryActionButtonEnable != value)
                {
                    _secondaryActionButtonEnable = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private bool _fileSelectButtonEnable;
        public bool FileSelectButtonEnable
        {
            get => _fileSelectButtonEnable;
            set
            {
                if (_fileSelectButtonEnable != value)
                {
                    _fileSelectButtonEnable = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private bool _downloadButtonEnable;
        public bool DownloadButtonEnable
        {
            get => _downloadButtonEnable;
            set
            {
                if (_downloadButtonEnable != value)
                {
                    _downloadButtonEnable = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private bool _deviceSelectButtonEnable;
        public bool DeviceSelectButtonEnable
        {
            get => _deviceSelectButtonEnable;
            set
            {
                if (_deviceSelectButtonEnable != value)
                {
                    _deviceSelectButtonEnable = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private bool _cancelOperationButtonEnable;
        public bool CancelOperationButtonEnable
        {
            get => _cancelOperationButtonEnable;
            set
            {
                if (_cancelOperationButtonEnable != value)
                {
                    _cancelOperationButtonEnable = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _waitProgressText;
        public string WaitProgressText
        {
            get => _waitProgressText;
            set
            {
                if (_waitProgressText != value)
                {
                    _waitProgressText = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private double _waitProgressValue = 0;
        public double WaitProgressValue
        {
            get => _waitProgressValue;
            set
            {
                if (_waitProgressValue != value)
                {
                    _waitProgressValue = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private double _appxInstallBarValue = 0;
        public double AppxInstallBarValue
        {
            get => _appxInstallBarValue;
            set
            {
                if (_appxInstallBarValue != value)
                {
                    _appxInstallBarValue = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private bool _waitProgressIndeterminate = true;
        public bool WaitProgressIndeterminate
        {
            get => _waitProgressIndeterminate;
            set
            {
                if (_waitProgressIndeterminate != value)
                {
                    _waitProgressIndeterminate = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private bool _appxInstallBarIndeterminate = true;
        public bool AppxInstallBarIndeterminate
        {
            get => _appxInstallBarIndeterminate;
            set
            {
                if (_appxInstallBarIndeterminate != value)
                {
                    _appxInstallBarIndeterminate = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _actionButtonText;
        public string ActionButtonText
        {
            get => _actionButtonText;
            set
            {
                if (_actionButtonText != value)
                {
                    _actionButtonText = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _secondaryActionButtonText;
        public string SecondaryActionButtonText
        {
            get => _secondaryActionButtonText;
            set
            {
                if (_secondaryActionButtonText != value)
                {
                    _secondaryActionButtonText = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _fileSelectButtonText;
        public string FileSelectButtonText
        {
            get => _fileSelectButtonText;
            set
            {
                if (_fileSelectButtonText != value)
                {
                    _fileSelectButtonText = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _downloadButtonText;
        public string DownloadButtonText
        {
            get => _downloadButtonText;
            set
            {
                if (_downloadButtonText != value)
                {
                    _downloadButtonText = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _deviceSelectButtonText;
        public string DeviceSelectButtonText
        {
            get => _deviceSelectButtonText;
            set
            {
                if (_deviceSelectButtonText != value)
                {
                    _deviceSelectButtonText = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private string _cancelOperationButtonText;
        public string CancelOperationButtonText
        {
            get => _cancelOperationButtonText;
            set
            {
                if (_cancelOperationButtonText != value)
                {
                    _cancelOperationButtonText = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _textOutputVisibility = Visibility.Collapsed;
        public Visibility TextOutputVisibility
        {
            get => _textOutputVisibility;
            set
            {
                if (_textOutputVisibility != value)
                {
                    _textOutputVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _installOutputVisibility = Visibility.Collapsed;
        public Visibility InstallOutputVisibility
        {
            get => _installOutputVisibility;
            set
            {
                if (_installOutputVisibility != value)
                {
                    _installOutputVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _actionVisibility = Visibility.Collapsed;
        public Visibility ActionVisibility
        {
            get => _actionVisibility;
            set
            {
                if (_actionVisibility != value)
                {
                    _actionVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _secondaryActionVisibility = Visibility.Collapsed;
        public Visibility SecondaryActionVisibility
        {
            get => _secondaryActionVisibility;
            set
            {
                if (_secondaryActionVisibility != value)
                {
                    _secondaryActionVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _fileSelectVisibility = Visibility.Collapsed;
        public Visibility FileSelectVisibility
        {
            get => _fileSelectVisibility;
            set
            {
                if (_fileSelectVisibility != value)
                {
                    _fileSelectVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _downloadVisibility = Visibility.Collapsed;
        public Visibility DownloadVisibility
        {
            get => _downloadVisibility;
            set
            {
                if (_downloadVisibility != value)
                {
                    _downloadVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _deviceSelectVisibility = Visibility.Collapsed;
        public Visibility DeviceSelectVisibility
        {
            get => _deviceSelectVisibility;
            set
            {
                if (_deviceSelectVisibility != value)
                {
                    _deviceSelectVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _cancelOperationVisibility = Visibility.Collapsed;
        public Visibility CancelOperationVisibility
        {
            get => _cancelOperationVisibility;
            set
            {
                if (_cancelOperationVisibility != value)
                {
                    _cancelOperationVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _messagesToUserVisibility = Visibility.Collapsed;
        public Visibility MessagesToUserVisibility
        {
            get => _messagesToUserVisibility;
            set
            {
                if (_messagesToUserVisibility != value)
                {
                    _messagesToUserVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _launchWhenReadyVisibility = Visibility.Collapsed;
        public Visibility LaunchWhenReadyVisibility
        {
            get => _launchWhenReadyVisibility;
            set
            {
                if (_launchWhenReadyVisibility != value)
                {
                    _launchWhenReadyVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _appVersionVisibility;
        public Visibility AppVersionVisibility
        {
            get => _appVersionVisibility;
            set
            {
                if (_appVersionVisibility != value)
                {
                    _appVersionVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _appPublisherVisibility;
        public Visibility AppPublisherVisibility
        {
            get => _appPublisherVisibility;
            set
            {
                if (_appPublisherVisibility != value)
                {
                    _appPublisherVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private Visibility _appCapabilitiesVisibility;
        public Visibility AppCapabilitiesVisibility
        {
            get => _appCapabilitiesVisibility;
            set
            {
                if (_appCapabilitiesVisibility != value)
                {
                    _appCapabilitiesVisibility = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (name != null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        public InstallViewModel(Uri Url, InstallPage Page, ProtocolForResultsOperation Operation = null)
        {
            _url = Url;
            _page = Page;
            Caches = this;
            _path = APKTemp;
            _operation = Operation;
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        public InstallViewModel(string Path, InstallPage Page, ProtocolForResultsOperation Operation = null)
        {
            _page = Page;
            Caches = this;
            _operation = Operation;
            _path = string.IsNullOrEmpty(Path) ? _path : Path;
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public static void SetPage(InstallPage Page) => Caches._page = Page;

        public async Task Refresh(bool force = true)
        {
            IsInitialized = false;
            WaitProgressText = _loader.GetString("Loading");
            //await OnFirstRun();
            try
            {
                if (force)
                {
                    await InitilizeADB();
                    await InitilizeUI();
                }
                else
                {
                    await ReinitilizeUI();
                    IsInitialized = true;
                }
            }
            catch (Exception ex)
            {
                PackageError(ex.Message);
                IsInstalling = false;
            }
        }

        private async Task OnFirstRun()
        {
            if (SettingsHelper.Get<bool>(SettingsHelper.IsFirstRun))
            {
                ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse("InstallPage");
                MarkdownDialog dialog = new()
                {
                    Title = _loader.GetString("Welcome"),
                    DefaultButton = ContentDialogButton.Close,
                    CloseButtonText = _loader.GetString("IKnow"),
                    ContentTask = async () =>
                    {
                        string langcode = LanguageHelper.GetCurrentLanguage();
                        Uri dataUri = new($"ms-appx:///String/{langcode}/About.md");
                        StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
                        return await FileIO.ReadTextAsync(file);
                    }
                };
                _ = await UIHelper.DispatcherQueue?.EnqueueAsync(async () => await dialog.ShowAsync());
                SettingsHelper.Set(SettingsHelper.IsFirstRun, false);
            }
        }

        private async Task CheckADB(bool force = false)
        {
            if (!await Task.Run(() => FileEx.Exists(ADBPath)))
            {
                ADBPath = @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe";
            }

            checkadb:
            if (force || !await Task.Run(() => FileEx.Exists(ADBPath)))
            {
                StackPanel StackPanel = new();
                StackPanel.Children.Add(
                    new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap,
                        Text = _loader.GetString("AboutADB")
                    });
                StackPanel.Children.Add(
                    new HyperlinkButton
                    {
                        Content = _loader.GetString("ClickToRead"),
                        NavigateUri = new Uri("https://developer.android.google.cn/studio/releases/platform-tools")
                    });
                ContentDialog dialog = new()
                {
                    Title = _loader.GetString("ADBMissing"),
                    PrimaryButtonText = _loader.GetString("Download"),
                    SecondaryButtonText = _loader.GetString("Select"),
                    CloseButtonText = _loader.GetString("Cancel"),
                    Content = new ScrollViewer()
                    {
                        Content = StackPanel
                    },
                    DefaultButton = ContentDialogButton.Primary
                };
                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    downloadadb:
                    if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    {
                        try
                        {
                            await DownloadADB();
                        }
                        catch (Exception ex)
                        {
                            ContentDialog dialogs = new()
                            {
                                Title = _loader.GetString("DownloadFailed"),
                                PrimaryButtonText = _loader.GetString("Retry"),
                                CloseButtonText = _loader.GetString("Cancel"),
                                Content = new TextBlock { Text = ex.Message },
                                DefaultButton = ContentDialogButton.Primary
                            };
                            ContentDialogResult results = await dialogs.ShowAsync();
                            if (results == ContentDialogResult.Primary)
                            {
                                goto downloadadb;
                            }
                            else
                            {
                                SendResults(new Exception($"ADB {_loader.GetString("DownloadFailed")}"));
                                Application.Current.Exit();
                                return;
                            }
                        }
                    }
                    else
                    {
                        ContentDialog dialogs = new()
                        {
                            Title = _loader.GetString("NoInternet"),
                            PrimaryButtonText = _loader.GetString("Retry"),
                            CloseButtonText = _loader.GetString("Cancel"),
                            Content = new TextBlock { Text = _loader.GetString("NoInternetInfo") },
                            DefaultButton = ContentDialogButton.Primary
                        };
                        ContentDialogResult results = await dialogs.ShowAsync();
                        if (results == ContentDialogResult.Primary)
                        {
                            goto checkadb;
                        }
                        else
                        {
                            SendResults(new Exception($"{_loader.GetString("NoInternet")}, ADB {_loader.GetString("DownloadFailed")}"));
                            Application.Current.Exit();
                            return;
                        }
                    }
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    FileOpenPicker FileOpen = new();
                    FileOpen.FileTypeFilter.Add(".exe");
                    FileOpen.SuggestedStartLocation = PickerLocationId.ComputerFolder;

                    StorageFile file = await FileOpen.PickSingleFileAsync();
                    if (file != null)
                    {
                        ADBPath = file.Path;
                    }
                }
                else
                {
                    SendResults(new Exception(_loader.GetString("ADBMissing")));
                    Application.Current.Exit();
                    return;
                }
            }
        }

        private async Task DownloadADB()
        {
            if (!Directory.Exists(ADBTemp.Substring(0, ADBTemp.LastIndexOf(@"\"))))
            {
                _ = Directory.CreateDirectory(ADBTemp.Substring(0, ADBTemp.LastIndexOf(@"\")));
            }
            else if (Directory.Exists(ADBTemp))
            {
                Directory.Delete(ADBTemp, true);
            }
            using (DownloadService downloader = new(DownloadHelper.Configuration))
            {
                bool IsCompleted = false;
                Exception exception = null;
                long ReceivedBytesSize = 0;
                long TotalBytesToReceive = 0;
                double ProgressPercentage = 0;
                double BytesPerSecondSpeed = 0;
                void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
                {
                    exception = e.Error;
                    IsCompleted = true;
                }
                void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
                {
                    ReceivedBytesSize = e.ReceivedBytesSize;
                    ProgressPercentage = e.ProgressPercentage;
                    TotalBytesToReceive = e.TotalBytesToReceive;
                    BytesPerSecondSpeed = e.BytesPerSecondSpeed;
                }
                downloader.DownloadFileCompleted += OnDownloadFileCompleted;
                downloader.DownloadProgressChanged += OnDownloadProgressChanged;
                downloadadb:
                WaitProgressText = _loader.GetString("WaitDownload");
                _ = downloader.DownloadFileTaskAsync("https://dl.google.com/android/repository/platform-tools-latest-windows.zip", ADBTemp);
                while (TotalBytesToReceive <= 0)
                {
                    await Task.Delay(1);
                    if (IsCompleted)
                    {
                        goto downloadfinish;
                    }
                }
                WaitProgressIndeterminate = false;
                while (!IsCompleted)
                {
                    WaitProgressText = $"{((double)BytesPerSecondSpeed).GetSizeString()}/s";
                    WaitProgressValue = ProgressPercentage;
                    await Task.Delay(1);
                }
                WaitProgressIndeterminate = true;
                WaitProgressValue = 0;
                downloadfinish:
                if (exception != null)
                {
                    ContentDialog dialog = new()
                    {
                        Content = exception.Message,
                        Title = _loader.GetString("DownloadFailed"),
                        PrimaryButtonText = _loader.GetString("Retry"),
                        CloseButtonText = _loader.GetString("Cancel"),
                        DefaultButton = ContentDialogButton.Primary
                    };
                    ContentDialogResult result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        goto downloadadb;
                    }
                    else
                    {
                        SendResults(new Exception($"ADB {_loader.GetString("DownloadFailed")}"));
                        Application.Current.Exit();
                        return;
                    }
                }
                downloader.DownloadProgressChanged -= OnDownloadProgressChanged;
                downloader.DownloadFileCompleted -= OnDownloadFileCompleted;
            }
            WaitProgressText = _loader.GetString("UnzipADB");
            await Task.Delay(1);
            using (IArchive archive = ArchiveFactory.Open(ADBTemp))
            {
                WaitProgressIndeterminate = false;
                int Progressed = 0;
                bool IsCompleted = false;
                double ProgressPercentage = 0;
                int TotalCount = archive.Entries.Count();
                _ = Task.Run(() =>
                {
                    foreach (IArchiveEntry entry in archive.Entries)
                    {
                        Progressed = archive.Entries.ToList().IndexOf(entry) + 1;
                        ProgressPercentage = archive.Entries.GetProgressValue(entry);
                        if (!entry.IsDirectory)
                        {
                            entry.WriteToDirectory(ApplicationData.Current.LocalFolder.Path, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                        }
                    }
                    IsCompleted = true;
                });
                while (!IsCompleted)
                {
                    WaitProgressValue = ProgressPercentage;
                    WaitProgressText = string.Format(_loader.GetString("UnzippingFormat"), Progressed, TotalCount);
                    await Task.Delay(1);
                }
                WaitProgressValue = 0;
                WaitProgressIndeterminate = true;
                WaitProgressText = _loader.GetString("UnzipComplete");
            }
            ADBPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, @"platform-tools\adb.exe");
        }

        private async Task InitilizeADB()
        {
            WaitProgressText = _loader.GetString("Loading");
            if (!string.IsNullOrEmpty(_path) || _url != null)
            {
                IAdbServer ADBServer = AdbServer.Instance;
                if (!ADBServer.GetStatus().IsRunning)
                {
                    WaitProgressText = _loader.GetString("CheckingADB");
                    await CheckADB();
                    startadb:
                    WaitProgressText = _loader.GetString("StartingADB");
                    try
                    {
                        await ADBHelper.StartADB();
                    }
                    catch
                    {
                        await CheckADB(true);
                        goto startadb;
                    }
                }
                WaitProgressText = _loader.GetString("Loading");
                if (!CheckDevice())
                {
                    if (IsOnlyWSA)
                    {
                        if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                        {
                            await AddressHelper.ConnectHyperV();
                            if (!CheckDevice())
                            {
                                new AdvancedAdbClient().Connect(new DnsEndPoint("127.0.0.1", 58526));
                            }
                        }
                        else
                        {
                            new AdvancedAdbClient().Connect(new DnsEndPoint("127.0.0.1", 58526));
                        }
                    }
                }
                ADBHelper.Monitor.DeviceChanged += OnDeviceChanged;
            }
        }

        private async Task InitilizeUI()
        {
            if (!string.IsNullOrEmpty(_path) || _url != null)
            {
                WaitProgressText = _loader.GetString("Loading");
                if (NetAPKExist)
                {
                    try
                    {
                        ApkInfo = await Task.Run(() => AAPTool.Decompile(_path));
                    }
                    catch (Exception ex)
                    {
                        PackageError(ex.Message);
                        IsInitialized = true;
                        return;
                    }
                }
                else
                {
                    ApkInfo ??= new ApkInfo();
                }
                if (string.IsNullOrEmpty(ApkInfo?.PackageName) && NetAPKExist)
                {
                    PackageError(_loader.GetString("InvalidPackage"));
                }
                else
                {
                    checkdevice:
                    WaitProgressText = _loader.GetString("Checking");
                    if (CheckDevice() && _device != null)
                    {
                        if (NetAPKExist)
                        {
                            CheckAPK();
                        }
                        else
                        {
                            ResetUI();
                            CheckOnlinePackage();
                        }
                    }
                    else
                    {
                        ResetUI();
                        if (NetAPKExist)
                        {
                            ActionButtonEnable = false;
                            ActionButtonText = _loader.GetString("Install");
                            InfoMessage = _loader.GetString("WaitingDevice");
                            DeviceSelectButtonText = _loader.GetString("Devices");
                            AppName = string.Format(_loader.GetString("WaitingForInstallFormat"), ApkInfo?.AppName);
                            ActionVisibility = DeviceSelectVisibility = MessagesToUserVisibility = Visibility.Visible;
                        }
                        else
                        {
                            CheckOnlinePackage();
                        }
                        if (ShowDialogs && await ShowDeviceDialog())
                        {
                            goto checkdevice;
                        }
                    }
                }
                WaitProgressText = _loader.GetString("Finished");
            }
            else
            {
                ResetUI();
                ApkInfo ??= new ApkInfo();
                AppName = _loader.GetString("NoPackageWranning");
                FileSelectButtonText = _loader.GetString("Select");
                CancelOperationButtonText = _loader.GetString("Close");
                FileSelectVisibility = CancelOperationVisibility = Visibility.Visible;
                AppVersionVisibility = AppPublisherVisibility = AppCapabilitiesVisibility = Visibility.Collapsed;
            }
            IsInitialized = true;
        }

        private async Task<bool> ShowDeviceDialog()
        {
            if (IsOnlyWSA)
            {
                WaitProgressText = _loader.GetString("FindingWSA");
                if ((await PackageHelper.FindPackagesByName("MicrosoftCorporationII.WindowsSubsystemForAndroid_8wekyb3d8bbwe")).isfound)
                {
                    WaitProgressText = _loader.GetString("FoundWSA");
                    ContentDialog dialog = new MarkdownDialog
                    {
                        Title = _loader.GetString("HowToConnect"),
                        DefaultButton = ContentDialogButton.Close,
                        CloseButtonText = _loader.GetString("IKnow"),
                        PrimaryButtonText = _loader.GetString("StartWSA"),
                        FallbackContent = _loader.GetString("HowToConnectInfo"),
                        ContentInfo = new GitInfo("Paving-Base", "APK-Installer", "screenshots", "Documents/Tutorials/How%20To%20Connect%20WSA", "How%20To%20Connect%20WSA.md")
                    };
                    ContentDialogResult result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        startwsa:
                        CancellationTokenSource TokenSource = new(TimeSpan.FromMinutes(5));
                        try
                        {
                            WaitProgressText = _loader.GetString("LaunchingWSA");
                            _ = await Launcher.LaunchUriAsync(new Uri("wsa://"));
                            WaitProgressText = _loader.GetString("WaitingWSAStart");
                            while (!CheckDevice())
                            {
                                TokenSource.Token.ThrowIfCancellationRequested();
                                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                                {
                                    await AddressHelper.ConnectHyperV();
                                    if (!CheckDevice())
                                    {
                                        new AdvancedAdbClient().Connect(new DnsEndPoint("127.0.0.1", 58526));
                                    }
                                }
                                else
                                {
                                    new AdvancedAdbClient().Connect(new DnsEndPoint("127.0.0.1", 58526));
                                }
                                await Task.Delay(100);
                            }
                            WaitProgressText = _loader.GetString("WSARunning");
                            return true;
                        }
                        catch
                        {
                            ContentDialog dialogs = new()
                            {
                                Title = _loader.GetString("CannotConnectWSA"),
                                DefaultButton = ContentDialogButton.Close,
                                CloseButtonText = _loader.GetString("IKnow"),
                                PrimaryButtonText = _loader.GetString("Retry"),
                                Content = _loader.GetString("CannotConnectWSAInfo"),
                            };
                            ContentDialogResult results = await dialogs.ShowAsync();
                            if (results == ContentDialogResult.Primary)
                            {
                                goto startwsa;
                            }
                        }
                    }
                }
                else
                {
                    ContentDialog dialog = new()
                    {
                        Title = _loader.GetString("NoDevice10"),
                        DefaultButton = ContentDialogButton.Primary,
                        CloseButtonText = _loader.GetString("IKnow"),
                        PrimaryButtonText = _loader.GetString("InstallWSA"),
                        SecondaryButtonText = _loader.GetString("GoToSetting"),
                        Content = _loader.GetString("NoDeviceInfo"),
                    };
                    ContentDialogResult result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        _ = await Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?ProductId=9P3395VX91NR&mode=mini"));
                    }
                    else if (result == ContentDialogResult.Secondary)
                    {
                        UIHelper.Navigate(typeof(SettingsPage), null);
                    }
                }
            }
            else
            {
                ContentDialog dialog = new MarkdownDialog
                {
                    Title = _loader.GetString("NoDevice"),
                    DefaultButton = ContentDialogButton.Close,
                    CloseButtonText = _loader.GetString("IKnow"),
                    PrimaryButtonText = _loader.GetString("GoToSetting"),
                    FallbackContent = _loader.GetString("NoDeviceInfo10"),
                    ContentInfo = new GitInfo("Paving-Base", "APK-Installer", "screenshots", "Documents/Tutorials/How%20To%20Connect%20Device", "How%20To%20Connect%20Device.md")
                };
                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    UIHelper.Navigate(typeof(SettingsPage), null);
                }
            }
            return false;
        }

        private async Task ReinitilizeUI()
        {
            WaitProgressText = _loader.GetString("Loading");
            if ((!string.IsNullOrEmpty(_path) || _url != null) && NetAPKExist)
            {
                checkdevice:
                if (CheckDevice() && _device != null)
                {
                    CheckAPK();
                }
                else if (ShowDialogs)
                {
                    if (await ShowDeviceDialog())
                    {
                        goto checkdevice;
                    }
                }
            }
        }

        private void CheckAPK()
        {
            ResetUI();
            AdvancedAdbClient client = new();
            PackageManager manager = new(client, _device);
            VersionInfo info = null;
            if (ApkInfo != null && !string.IsNullOrEmpty(ApkInfo?.PackageName))
            {
                info = manager.GetVersionInfo(ApkInfo?.PackageName);
            }
            if (info == null)
            {
                ActionButtonText = _loader.GetString("Install");
                AppName = string.Format(_loader.GetString("InstallFormat"), ApkInfo?.AppName);
                ActionVisibility = LaunchWhenReadyVisibility = Visibility.Visible;
            }
            else if (info.VersionCode < int.Parse(ApkInfo?.VersionCode))
            {
                ActionButtonText = _loader.GetString("Update");
                AppName = string.Format(_loader.GetString("UpdateFormat"), ApkInfo?.AppName);
                ActionVisibility = LaunchWhenReadyVisibility = Visibility.Visible;
            }
            else
            {
                ActionButtonText = _loader.GetString("Reinstall");
                SecondaryActionButtonText = _loader.GetString("Launch");
                AppName = string.Format(_loader.GetString("ReinstallFormat"), ApkInfo?.AppName);
                TextOutput = string.Format(_loader.GetString("ReinstallOutput"), ApkInfo?.AppName);
                ActionVisibility = SecondaryActionVisibility = TextOutputVisibility = Visibility.Visible;
            }
        }

        private void CheckOnlinePackage()
        {
            Regex[] UriRegex = new Regex[] { new Regex(@":\?source=(.*)"), new Regex(@"://(.*)") };
            string Uri = UriRegex[0].IsMatch(_url.ToString()) ? UriRegex[0].Match(_url.ToString()).Groups[1].Value : UriRegex[1].Match(_url.ToString()).Groups[1].Value;
            Uri Url = Uri.ValidateAndGetUri();
            if (Url != null)
            {
                _url = Url;
                AppName = _loader.GetString("OnlinePackage");
                DownloadButtonText = _loader.GetString("Download");
                CancelOperationButtonText = _loader.GetString("Close");
                DownloadVisibility = CancelOperationVisibility = Visibility.Visible;
                AppVersionVisibility = AppPublisherVisibility = AppCapabilitiesVisibility = Visibility.Collapsed;
                if (AutoGetNetAPK)
                {
                    LoadNetAPK();
                }
            }
            else
            {
                PackageError(_loader.GetString("InvalidURL"));
            }
        }

        public async void LoadNetAPK()
        {
            IsInstalling = true;
            DownloadVisibility = Visibility.Collapsed;
            try
            {
                await DownloadAPK();
            }
            catch (Exception ex)
            {
                PackageError(ex.Message);
                IsInstalling = false;
                return;
            }

            try
            {
                ApkInfo = await Task.Run(() => { return AAPTool.Decompile(_path); });
            }
            catch (Exception ex)
            {
                PackageError(ex.Message);
                IsInstalling = false;
                return;
            }

            if (string.IsNullOrEmpty(ApkInfo?.PackageName))
            {
                PackageError(_loader.GetString("InvalidPackage"));
            }
            else
            {
                if (CheckDevice() && _device != null)
                {
                    CheckAPK();
                }
                else
                {
                    ResetUI();
                    ActionButtonEnable = false;
                    ActionButtonText = _loader.GetString("Install");
                    InfoMessage = _loader.GetString("WaitingDevice");
                    DeviceSelectButtonText = _loader.GetString("Devices");
                    AppName = string.Format(_loader.GetString("WaitingForInstallFormat"), ApkInfo?.AppName);
                    ActionVisibility = DeviceSelectVisibility = MessagesToUserVisibility = Visibility.Visible;
                }
            }
            IsInstalling = false;
        }

        private async Task DownloadAPK()
        {
            if (_url != null)
            {
                if (!Directory.Exists(APKTemp.Substring(0, APKTemp.LastIndexOf(@"\"))))
                {
                    _ = Directory.CreateDirectory(APKTemp.Substring(0, APKTemp.LastIndexOf(@"\")));
                }
                else if (Directory.Exists(APKTemp))
                {
                    Directory.Delete(APKTemp, true);
                }
                using (DownloadService downloader = new(DownloadHelper.Configuration))
                {
                    bool IsCompleted = false;
                    Exception exception = null;
                    long ReceivedBytesSize = 0;
                    long TotalBytesToReceive = 0;
                    double ProgressPercentage = 0;
                    double BytesPerSecondSpeed = 0;
                    void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
                    {
                        exception = e.Error;
                        IsCompleted = true;
                    }
                    void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
                    {
                        ReceivedBytesSize = e.ReceivedBytesSize;
                        ProgressPercentage = e.ProgressPercentage;
                        TotalBytesToReceive = e.TotalBytesToReceive;
                        BytesPerSecondSpeed = e.BytesPerSecondSpeed;
                    }
                    downloader.DownloadFileCompleted += OnDownloadFileCompleted;
                    downloader.DownloadProgressChanged += OnDownloadProgressChanged;
                    downloadapk:
                    ProgressText = _loader.GetString("WaitDownload");
                    _ = downloader.DownloadFileTaskAsync(_url.ToString(), APKTemp);
                    while (TotalBytesToReceive <= 0)
                    {
                        await Task.Delay(1);
                        if (IsCompleted)
                        {
                            goto downloadfinish;
                        }
                    }
                    AppxInstallBarIndeterminate = false;
                    while (!IsCompleted)
                    {
                        ProgressText = $"{ProgressPercentage:N2}% {((double)BytesPerSecondSpeed).GetSizeString()}/s";
                        AppxInstallBarValue = ProgressPercentage;
                        await Task.Delay(1);
                    }
                    ProgressText = _loader.GetString("Loading");
                    AppxInstallBarIndeterminate = true;
                    AppxInstallBarValue = 0;
                    downloadfinish:
                    if (exception != null)
                    {
                        ContentDialog dialog = new()
                        {
                            Content = exception.Message,
                            Title = _loader.GetString("DownloadFailed"),
                            PrimaryButtonText = _loader.GetString("Retry"),
                            CloseButtonText = _loader.GetString("Cancel"),
                            DefaultButton = ContentDialogButton.Primary
                        };
                        ContentDialogResult result = await dialog.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            goto downloadapk;
                        }
                        else
                        {
                            SendResults(new Exception($"APK {_loader.GetString("DownloadFailed")}"));
                            Application.Current.Exit();
                            return;
                        }
                    }
                    downloader.DownloadProgressChanged -= OnDownloadProgressChanged;
                    downloader.DownloadFileCompleted -= OnDownloadFileCompleted;
                }
            }
        }

        private void ResetUI()
        {
            ActionVisibility =
            SecondaryActionVisibility =
            FileSelectVisibility =
            DownloadVisibility =
            DeviceSelectVisibility =
            CancelOperationVisibility =
            TextOutputVisibility =
            InstallOutputVisibility =
            LaunchWhenReadyVisibility =
            MessagesToUserVisibility = Visibility.Collapsed;
            AppVersionVisibility =
            AppPublisherVisibility =
            AppCapabilitiesVisibility = Visibility.Visible;
            AppxInstallBarIndeterminate =
            ActionButtonEnable =
            SecondaryActionButtonEnable =
            FileSelectButtonEnable =
            DownloadButtonEnable =
            DeviceSelectButtonEnable =
            CancelOperationButtonEnable = true;
        }

        private void PackageError(string message)
        {
            ResetUI();
            TextOutput = message;
            ApkInfo ??= new ApkInfo();
            AppName = _loader.GetString("CannotOpenPackage");
            TextOutputVisibility = InstallOutputVisibility = Visibility.Visible;
            AppVersionVisibility = AppPublisherVisibility = AppCapabilitiesVisibility = Visibility.Collapsed;
        }

        private void OnDeviceChanged(object sender, DeviceDataEventArgs e)
        {
            if (IsInitialized && !IsInstalling)
            {
                UIHelper.DispatcherQueue?.EnqueueAsync(() =>
                {
                    if (CheckDevice() && _device != null)
                    {
                        CheckAPK();
                    }
                });
            }
        }

        private bool CheckDevice()
        {
            AdvancedAdbClient client = new();
            List<DeviceData> devices = client.GetDevices();
            ConsoleOutputReceiver receiver = new();
            if (devices.Count <= 0) { return false; }
            foreach (DeviceData device in devices)
            {
                if (device == null || device.State == DeviceState.Offline) { continue; }
                if (IsOnlyWSA)
                {
                    client.ExecuteRemoteCommand("getprop ro.boot.hardware", device, receiver);
                    if (receiver.ToString().Contains("windows"))
                    {
                        _device = device ?? _device;
                        return true;
                    }
                }
                else
                {
                    DeviceData data = SettingsHelper.Get<DeviceData>(SettingsHelper.DefaultDevice);
                    if (data != null && data.Name == device.Name && data.Model == device.Model && data.Product == device.Product)
                    {
                        _device = data;
                        return true;
                    }
                }
            }
            return false;
        }

        public void OpenAPP() => new AdvancedAdbClient().StartApp(_device, ApkInfo?.PackageName);

        public async void InstallAPP()
        {
            try
            {
                IsInstalling = true;
                AppxInstallBarIndeterminate = true;
                ProgressText = _loader.GetString("Installing");
                CancelOperationButtonText = _loader.GetString("Cancel");
                CancelOperationVisibility = LaunchWhenReadyVisibility = Visibility.Visible;
                ActionVisibility = SecondaryActionVisibility = TextOutputVisibility = InstallOutputVisibility = Visibility.Collapsed;
                await Task.Run(() =>
                {
                    new AdvancedAdbClient().Install(_device, File.Open(ApkInfo.FullPath, FileMode.Open, FileAccess.Read));
                });
                AppName = string.Format(_loader.GetString("InstalledFormat"), ApkInfo?.AppName);
                if (IsOpenApp)
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000);// 据说如果安装完直接启动会崩溃。。。
                        OpenAPP();
                        if (IsCloseAPP)
                        {
                            await Task.Delay(5000);
                            UIHelper.DispatcherQueue?.TryEnqueue(() => Application.Current.Exit());
                        }
                    });
                }
                SendResults();
                IsInstalling = false;
                AppxInstallBarValue = 0;
                AppxInstallBarIndeterminate = true;
                SecondaryActionVisibility = Visibility.Visible;
                SecondaryActionButtonText = _loader.GetString("Launch");
                CancelOperationVisibility = LaunchWhenReadyVisibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                SendResults(ex);
                IsInstalling = false;
                TextOutput = ex.Message;
                TextOutputVisibility = InstallOutputVisibility = Visibility.Visible;
                ActionVisibility = SecondaryActionVisibility = CancelOperationVisibility = LaunchWhenReadyVisibility = Visibility.Collapsed;
            }

            void OnInstallProgressChanged(object sender, double e)
            {
                UIHelper.DispatcherQueue?.TryEnqueue(() =>
                {
                    AppxInstallBarValue = e;
                    ProgressText = string.Format(_loader.GetString("InstallingPercent"), $"{e:N0}%");
                });
            }
        }

        public async void OpenAPK(string path)
        {
            if (path != null)
            {
                _path = path;
                await UIHelper.DispatcherQueue?.EnqueueAsync(async () => await Refresh());
            }
        }

        public async void OpenAPK()
        {
            FileOpenPicker FileOpen = new();
            FileOpen.FileTypeFilter.Add(".apk");
            FileOpen.FileTypeFilter.Add(".apks");
            FileOpen.FileTypeFilter.Add(".apkm");
            FileOpen.FileTypeFilter.Add(".xapk");
            FileOpen.SuggestedStartLocation = PickerLocationId.ComputerFolder;

            StorageFile file = await FileOpen.PickSingleFileAsync();
            if (file != null)
            {
                _path = file.Path;
                await Refresh();
            }
        }

        public async void OpenAPK(DataPackageView data)
        {
            WaitProgressText = _loader.GetString("CheckingPath");
            IsInitialized = false;

            await Task.Run(async () =>
            {
                if (data.Contains(StandardDataFormats.StorageItems))
                {
                    IReadOnlyList<IStorageItem> items = await data.GetStorageItemsAsync();
                    if (items.Count == 1)
                    {
                        IStorageItem storageItem = items.FirstOrDefault();
                        await OpenPath(storageItem);
                        return;
                    }
                    else if (items.Count >= 1)
                    {
                        await CreateAPKS(items);
                        return;
                    }
                }
                else if (data.Contains(StandardDataFormats.Text))
                {
                    string path = await data.GetTextAsync();
                    if (Directory.Exists(path))
                    {
                        StorageFolder storageItem = await StorageFolder.GetFolderFromPathAsync(path);
                        await OpenPath(storageItem);
                        return;
                    }
                    else if (File.Exists(path))
                    {
                        StorageFile storageItem = await StorageFile.GetFileFromPathAsync(path);
                        await OpenPath(storageItem);
                        return;
                    }
                }
                await (UIHelper.DispatcherQueue?.EnqueueAsync(() => { IsInitialized = true; }));
            });

            async Task OpenPath(IStorageItem storageItem)
            {
                if (storageItem != null)
                {
                    if (storageItem is StorageFolder folder)
                    {
                        IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
                        await CreateAPKS(files);
                        return;
                    }
                    else
                    {
                        if (storageItem.Name.ToLower().EndsWith(".apk"))
                        {
                            OpenAPK(storageItem.Path);
                            return;
                        }
                        try
                        {
                            using (IArchive archive = ArchiveFactory.Open(storageItem.Path))
                            {
                                foreach (IArchiveEntry entry in archive.Entries.Where(x => !x.Key.Contains('/')))
                                {
                                    if (entry.Key.ToLower().EndsWith(".apk"))
                                    {
                                        OpenAPK(storageItem.Path);
                                        return;
                                    }
                                }
                            }
                            await (UIHelper.DispatcherQueue?.EnqueueAsync(() => { IsInitialized = true; }));
                        }
                        catch { }
                    }
                }
                await (UIHelper.DispatcherQueue?.EnqueueAsync(() => { IsInitialized = true; }));
            }

            async Task CreateAPKS(IReadOnlyList<IStorageItem> items)
            {
                List<string> apks = new();
                foreach (IStorageItem item in items)
                {
                    if (item != null)
                    {
                        if (item.Name.ToLower().EndsWith(".apk"))
                        {
                            apks.Add(item.Path);
                            continue;
                        }
                        try
                        {
                            using (IArchive archive = ArchiveFactory.Open(item.Path))
                            {
                                foreach (IArchiveEntry entry in archive.Entries.Where(x => !x.Key.Contains('/')))
                                {
                                    if (entry.Key.ToLower().EndsWith(".apk"))
                                    {
                                        OpenAPK(item.Path);
                                        return;
                                    }
                                }
                            }
                            apks.Add(item.Path);
                            continue;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                if (apks.Count == 1)
                {
                    OpenAPK(apks.First());
                    return;
                }
                else if (apks.Count >= 1)
                {
                    IEnumerable<string> apklist = apks.Where(x => x.EndsWith(".apk"));
                    if (apklist.Any())
                    {
                        string temp = Path.Combine(CachesHelper.TempPath, $"TempSplitAPK.apks");

                        if (!Directory.Exists(temp.Substring(0, temp.LastIndexOf(@"\"))))
                        {
                            _ = Directory.CreateDirectory(temp.Substring(0, temp.LastIndexOf(@"\")));
                        }
                        else if (Directory.Exists(temp))
                        {
                            Directory.Delete(temp, true);
                        }

                        if (File.Exists(temp))
                        {
                            File.Delete(temp);
                        }

                        using (FileStream zip = File.OpenWrite(temp))
                        {
                            using IWriter zipWriter = WriterFactory.Open(zip, ArchiveType.Zip, CompressionType.Deflate);
                            foreach (string apk in apks.Where(x => x.EndsWith(".apk")))
                            {
                                zipWriter.Write(Path.GetFileName(apk), apk);
                            }
                        }

                        OpenAPK(temp);
                        return;
                    }
                    else
                    {
                        IEnumerable<string> apkslist = apks.Where(x => !x.EndsWith(".apk"));
                        if (apkslist.Count() == 1)
                        {
                            OpenAPK(apkslist.First());
                            return;
                        }
                    }
                }

                await (UIHelper.DispatcherQueue?.EnqueueAsync(() => { IsInitialized = true; }));
            }
        }

        private void SendResults(Exception exception = null)
        {
            if (_operation == null) { return; }
            ValueSet results = new()
            {
                ["Result"] = exception != null,
                ["Exception"] = exception
            };
            _operation.ReportCompleted(results);
        }

        public void CloseAPP()
        {
            SendResults(new Exception($"{_loader.GetString("Install")} {_loader.GetString("Cancel")}"));
            Application.Current.Exit();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    ADBHelper.Monitor.DeviceChanged -= OnDeviceChanged;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
