﻿using AAPTForNet;
using AAPTForNet.Models;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.DeviceCommands.Models;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using APKInstaller.Common;
using APKInstaller.Controls.Dialogs;
using APKInstaller.Helpers;
using APKInstaller.Models;
using APKInstaller.Pages;
using APKInstaller.Pages.SettingsPages;
using Downloader;
using Microsoft.Toolkit.Uwp.Connectivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;

namespace APKInstaller.ViewModels
{
    public class InstallViewModel : INotifyPropertyChanged
    {
        private DeviceData? _device;
        private readonly InstallPage _page;
        private readonly ProtocolForResultsOperation _operation;
        private static readonly string APKTemp = Path.Combine(CachesHelper.TempPath, "NetAPKTemp.apk");
        private static readonly string ADBTemp = Path.Combine(CachesHelper.TempPath, "platform-tools.zip");
        private static readonly ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse("InstallPage");

        private Uri _url;
        private StorageFile _file;
        private string _appLocaleName = string.Empty;

        public CoreDispatcher Dispatcher => _page.Dispatcher;

        public string InstallFormat => _loader.GetString("InstallFormat");
        public string VersionFormat => _loader.GetString("VersionFormat");
        public string PackageNameFormat => _loader.GetString("PackageNameFormat");

        private static bool IsOnlyWSA => SettingsHelper.Get<bool>(SettingsHelper.IsOnlyWSA);
        private static bool IsCloseAPP => SettingsHelper.Get<bool>(SettingsHelper.IsCloseAPP);
        private static bool IsUploadAPK => SettingsHelper.Get<bool>(SettingsHelper.IsUploadAPK);
        private static bool ShowDialogs => SettingsHelper.Get<bool>(SettingsHelper.ShowDialogs);
        private static bool AutoGetNetAPK => SettingsHelper.Get<bool>(SettingsHelper.AutoGetNetAPK);
        private static bool ScanPairedDevice => SettingsHelper.Get<bool>(SettingsHelper.ScanPairedDevice);

        private ApkInfo _apkInfo = null;
        public ApkInfo ApkInfo
        {
            get => _apkInfo;
            set => SetProperty(ref _apkInfo, value);
        }

        public string ADBPath
        {
            get => SettingsHelper.Get<string>(SettingsHelper.ADBPath);
            set => SettingsHelper.Set(SettingsHelper.ADBPath, value);
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
            set => SetProperty(ref _isInstalling, value);
        }

        private bool _isInitialized;
        public bool IsInitialized
        {
            get => _isInitialized;
            set => SetProperty(ref _isInitialized, value);
        }

        private string _appName;
        public string AppName
        {
            get => _appName;
            set => SetProperty(ref _appName, value);
        }

        private string _textOutput;
        public string TextOutput
        {
            get => _textOutput;
            set => SetProperty(ref _textOutput, value);
        }

        private string _infoMessage;
        public string InfoMessage
        {
            get => _infoMessage;
            set => SetProperty(ref _infoMessage, value);
        }

        private string _progressText;
        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        private bool _actionButtonEnable;
        public bool ActionButtonEnable
        {
            get => _actionButtonEnable;
            set => SetProperty(ref _actionButtonEnable, value);
        }

        private bool _secondaryActionButtonEnable;
        public bool SecondaryActionButtonEnable
        {
            get => _secondaryActionButtonEnable;
            set => SetProperty(ref _secondaryActionButtonEnable, value);
        }

        private bool _fileSelectButtonEnable;
        public bool FileSelectButtonEnable
        {
            get => _fileSelectButtonEnable;
            set => SetProperty(ref _fileSelectButtonEnable, value);
        }

        private bool _downloadButtonEnable;
        public bool DownloadButtonEnable
        {
            get => _downloadButtonEnable;
            set => SetProperty(ref _downloadButtonEnable, value);
        }

        private bool _deviceSelectButtonEnable;
        public bool DeviceSelectButtonEnable
        {
            get => _deviceSelectButtonEnable;
            set => SetProperty(ref _deviceSelectButtonEnable, value);
        }

        private bool _cancelOperationButtonEnable;
        public bool CancelOperationButtonEnable
        {
            get => _cancelOperationButtonEnable;
            set => SetProperty(ref _cancelOperationButtonEnable, value);
        }

        private string _waitProgressText;
        public string WaitProgressText
        {
            get => _waitProgressText;
            set => SetProperty(ref _waitProgressText, value);
        }

        private double _waitProgressValue = 0;
        public double WaitProgressValue
        {
            get => _waitProgressValue;
            set => SetProperty(ref _waitProgressValue, value);
        }

        private double _appxInstallBarValue = 0;
        public double AppxInstallBarValue
        {
            get => _appxInstallBarValue;
            set => SetProperty(ref _appxInstallBarValue, value);
        }

        private bool _waitProgressIndeterminate = true;
        public bool WaitProgressIndeterminate
        {
            get => _waitProgressIndeterminate;
            set => SetProperty(ref _waitProgressIndeterminate, value);
        }

        private bool _appxInstallBarIndeterminate = true;
        public bool AppxInstallBarIndeterminate
        {
            get => _appxInstallBarIndeterminate;
            set => SetProperty(ref _appxInstallBarIndeterminate, value);
        }

        private string _actionButtonText;
        public string ActionButtonText
        {
            get => _actionButtonText;
            set => SetProperty(ref _actionButtonText, value);
        }

        private string _secondaryActionButtonText;
        public string SecondaryActionButtonText
        {
            get => _secondaryActionButtonText;
            set => SetProperty(ref _secondaryActionButtonText, value);
        }

        private string _fileSelectButtonText;
        public string FileSelectButtonText
        {
            get => _fileSelectButtonText;
            set => SetProperty(ref _fileSelectButtonText, value);
        }

        private string _downloadButtonText;
        public string DownloadButtonText
        {
            get => _downloadButtonText;
            set => SetProperty(ref _downloadButtonText, value);
        }

        private string _deviceSelectButtonText;
        public string DeviceSelectButtonText
        {
            get => _deviceSelectButtonText;
            set => SetProperty(ref _deviceSelectButtonText, value);
        }

        private string _cancelOperationButtonText;
        public string CancelOperationButtonText
        {
            get => _cancelOperationButtonText;
            set => SetProperty(ref _cancelOperationButtonText, value);
        }

        private bool _textOutputVisibility = false;
        public bool TextOutputVisibility
        {
            get => _textOutputVisibility;
            set => SetProperty(ref _textOutputVisibility, value);
        }

        private bool _installOutputVisibility = false;
        public bool InstallOutputVisibility
        {
            get => _installOutputVisibility;
            set => SetProperty(ref _installOutputVisibility, value);
        }

        private bool _actionVisibility = false;
        public bool ActionVisibility
        {
            get => _actionVisibility;
            set => SetProperty(ref _actionVisibility, value);
        }

        private bool _secondaryActionVisibility = false;
        public bool SecondaryActionVisibility
        {
            get => _secondaryActionVisibility;
            set => SetProperty(ref _secondaryActionVisibility, value);
        }

        private bool _fileSelectVisibility = false;
        public bool FileSelectVisibility
        {
            get => _fileSelectVisibility;
            set => SetProperty(ref _fileSelectVisibility, value);
        }

        private bool _downloadVisibility = false;
        public bool DownloadVisibility
        {
            get => _downloadVisibility;
            set => SetProperty(ref _downloadVisibility, value);
        }

        private bool _deviceSelectVisibility = false;
        public bool DeviceSelectVisibility
        {
            get => _deviceSelectVisibility;
            set => SetProperty(ref _deviceSelectVisibility, value);
        }

        private bool _cancelOperationVisibility = false;
        public bool CancelOperationVisibility
        {
            get => _cancelOperationVisibility;
            set => SetProperty(ref _cancelOperationVisibility, value);
        }

        private bool _messagesToUserVisibility = false;
        public bool MessagesToUserVisibility
        {
            get => _messagesToUserVisibility;
            set => SetProperty(ref _messagesToUserVisibility, value);
        }

        private bool _launchWhenReadyVisibility = false;
        public bool LaunchWhenReadyVisibility
        {
            get => _launchWhenReadyVisibility;
            set => SetProperty(ref _launchWhenReadyVisibility, value);
        }

        private bool _appVersionVisibility;
        public bool AppVersionVisibility
        {
            get => _appVersionVisibility;
            set => SetProperty(ref _appVersionVisibility, value);
        }

        private bool _appPublisherVisibility;
        public bool AppPublisherVisibility
        {
            get => _appPublisherVisibility;
            set => SetProperty(ref _appPublisherVisibility, value);
        }

        private bool _appCapabilitiesVisibility;
        public bool AppCapabilitiesVisibility
        {
            get => _appCapabilitiesVisibility;
            set => SetProperty(ref _appCapabilitiesVisibility, value);
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

        public InstallViewModel(Uri url, InstallPage page, ProtocolForResultsOperation operation = null)
        {
            _url = url;
            _page = page;
            _operation = operation;
        }

        public InstallViewModel(StorageFile file, InstallPage page, ProtocolForResultsOperation operation = null)
        {
            _file = file;
            _page = page;
            _operation = operation;
        }

        public async Task Refresh(bool force = true)
        {
            IsInitialized = false;
            WaitProgressText = _loader.GetString("Loading");
            //await OnFirstRun();
            try
            {
                if (force)
                {
                    await InitializeADBAsync();
                    await InitializeUIAsync();
                }
                else
                {
                    await ReinitializeUIAsync();
                    IsInitialized = true;
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Error(ex.ExceptionToMessage(), ex);
                PackageError(ex.Message);
                IsInstalling = false;
            }
        }

        private async Task OnFirstRunAsync()
        {
            if (SettingsHelper.Get<bool>(SettingsHelper.IsFirstRun))
            {
                ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse("InstallPage");
                await Dispatcher.ResumeForegroundAsync();
                _ = await new MarkdownDialog
                {
                    Title = _loader.GetString("Welcome"),
                    DefaultButton = ContentDialogButton.Close,
                    CloseButtonText = _loader.GetString("IKnow"),
                    ContentTask = async () =>
                    {
                        string langCode = LanguageHelper.GetCurrentLanguage();
                        Uri dataUri = new($"ms-appx:///String/{langCode}/About.md");
                        StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
                        return await FileIO.ReadTextAsync(file);
                    }
                }.SetXAMLRoot(_page).ShowAsync();
                SettingsHelper.Set(SettingsHelper.IsFirstRun, false);
            }
        }

        private async Task CheckADBAsync(bool force = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (!await ADBHelper.CheckFileExistsAsync(ADBPath).ConfigureAwait(false))
            {
                ADBPath = @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe";
            }

        checkAdb:
            if (force || !await ADBHelper.CheckFileExistsAsync(ADBPath).ConfigureAwait(false))
            {
                await Dispatcher.ResumeForegroundAsync();
                StackPanel stackPanel = new() {
                    Children =
                    {
                        new TextBlock
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Text = _loader.GetString("AboutADB")
                        },
                        new HyperlinkButton
                        {
                            Content = _loader.GetString("ClickToRead"),
                            NavigateUri = new Uri("https://developer.android.google.cn/studio/releases/platform-tools")
                        }
                    }
                };
                ContentDialogResult result = await new ContentDialog
                {
                    Title = _loader.GetString("ADBMissing"),
                    PrimaryButtonText = _loader.GetString("Download"),
                    SecondaryButtonText = _loader.GetString("Select"),
                    CloseButtonText = _loader.GetString("Cancel"),
                    Content = new ScrollViewer
                    {
                        Content = stackPanel
                    },
                    DefaultButton = ContentDialogButton.Primary
                }.SetXAMLRoot(_page).ShowAsync();
                await ThreadSwitcher.ResumeBackgroundAsync();
                if (result == ContentDialogResult.Primary)
                {
                downloadAdb:
                    if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    {
                        try
                        {
                            await DownloadADBAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Error(ex.ExceptionToMessage(), ex);
                            await Dispatcher.ResumeForegroundAsync();
                            ContentDialogResult results = await new ContentDialog
                            {
                                Title = _loader.GetString("DownloadFailed"),
                                PrimaryButtonText = _loader.GetString("Retry"),
                                CloseButtonText = _loader.GetString("Cancel"),
                                Content = new TextBlock { Text = ex.Message },
                                DefaultButton = ContentDialogButton.Primary
                            }.SetXAMLRoot(_page).ShowAsync();
                            await ThreadSwitcher.ResumeBackgroundAsync();
                            if (results == ContentDialogResult.Primary)
                            {
                                goto downloadAdb;
                            }
                            else
                            {
                                SendResults(new Exception($"ADB {_loader.GetString("DownloadFailed")}"));
                                await ExitApplicationAsync().ConfigureAwait(false);
                                return;
                            }
                        }
                    }
                    else
                    {
                        await Dispatcher.ResumeForegroundAsync();
                        ContentDialogResult results = await new ContentDialog
                        {
                            Title = _loader.GetString("NoInternet"),
                            PrimaryButtonText = _loader.GetString("Retry"),
                            CloseButtonText = _loader.GetString("Cancel"),
                            Content = new TextBlock { Text = _loader.GetString("NoInternetInfo") },
                            DefaultButton = ContentDialogButton.Primary
                        }.SetXAMLRoot(_page).ShowAsync();
                        await ThreadSwitcher.ResumeBackgroundAsync();
                        if (results == ContentDialogResult.Primary)
                        {
                            goto checkAdb;
                        }
                        else
                        {
                            SendResults(new Exception($"{_loader.GetString("NoInternet")}, ADB {_loader.GetString("DownloadFailed")}"));
                            await ExitApplicationAsync().ConfigureAwait(false);
                            return;
                        }
                    }
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    await Dispatcher.ResumeForegroundAsync();

                    FileOpenPicker fileOpen = new()
                    {
                        SuggestedStartLocation = PickerLocationId.ComputerFolder
                    };
                    fileOpen.FileTypeFilter.Add(".exe");

                    StorageFile file = await fileOpen.PickSingleFileAsync();
                    await ThreadSwitcher.ResumeBackgroundAsync();

                    if (file != null)
                    {
                        ADBPath = file.Path;
                    }
                }
                else
                {
                    SendResults(new Exception(_loader.GetString("ADBMissing")));
                    await ExitApplicationAsync().ConfigureAwait(false);
                    return;
                }
            }
        }

        private async Task DownloadADBAsync()
        {
            await Dispatcher.ResumeForegroundAsync();
            FolderPicker folderPicker = new()
            {
                SuggestedStartLocation = PickerLocationId.Downloads
            };
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            await ThreadSwitcher.ResumeBackgroundAsync();
            if (!Directory.Exists(CachesHelper.TempPath))
            {
                _ = Directory.CreateDirectory(CachesHelper.TempPath);
            }
            else if (Directory.Exists(ADBTemp))
            {
                Directory.Delete(ADBTemp, true);
            }
            using (DownloadService downloader = new(DownloadHelper.Configuration))
            {
                bool isCompleted = false;
                Exception exception = null;
                long receivedBytesSize = 0;
                long totalBytesToReceive = 0;
                double progressPercentage = 0;
                double bytesPerSecondSpeed = 0;
                void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
                {
                    exception = e.Error;
                    isCompleted = true;
                }
                void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
                {
                    receivedBytesSize = e.ReceivedBytesSize;
                    progressPercentage = e.ProgressPercentage;
                    totalBytesToReceive = e.TotalBytesToReceive;
                    bytesPerSecondSpeed = e.BytesPerSecondSpeed;
                }
                downloader.DownloadFileCompleted += OnDownloadFileCompleted;
                downloader.DownloadProgressChanged += OnDownloadProgressChanged;
            downloadAdb:
                WaitProgressText = _loader.GetString("WaitDownload");
                _ = downloader.DownloadFileTaskAsync("https://dl.google.com/android/repository/platform-tools-latest-windows.zip", ADBTemp).ConfigureAwait(false);
                while (totalBytesToReceive <= 0)
                {
                    await Task.Delay(1);
                    if (isCompleted)
                    {
                        goto downloadFinish;
                    }
                }
                WaitProgressIndeterminate = false;
                while (!isCompleted)
                {
                    WaitProgressText = $"{((double)bytesPerSecondSpeed).GetSizeString()}/s";
                    WaitProgressValue = progressPercentage;
                    await Task.Delay(1);
                }
                WaitProgressIndeterminate = true;
                WaitProgressValue = 0;
            downloadFinish:
                if (exception != null)
                {
                    await Dispatcher.ResumeForegroundAsync();
                    ContentDialogResult result = await new ContentDialog
                    {
                        Content = exception.Message,
                        Title = _loader.GetString("DownloadFailed"),
                        PrimaryButtonText = _loader.GetString("Retry"),
                        CloseButtonText = _loader.GetString("Cancel"),
                        DefaultButton = ContentDialogButton.Primary
                    }.SetXAMLRoot(_page).ShowAsync();
                    await ThreadSwitcher.ResumeBackgroundAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        goto downloadAdb;
                    }
                    else
                    {
                        SendResults(new Exception($"ADB {_loader.GetString("DownloadFailed")}"));
                        await ExitApplicationAsync().ConfigureAwait(false);
                        return;
                    }
                }
                downloader.DownloadProgressChanged -= OnDownloadProgressChanged;
                downloader.DownloadFileCompleted -= OnDownloadFileCompleted;
            }
            WaitProgressText = _loader.GetString("UnzipADB");
            await Task.Delay(1);
            using (ZipArchive archive = ZipFile.OpenRead(ADBTemp))
            {
                WaitProgressIndeterminate = false;
                ReadOnlyCollection<ZipArchiveEntry> entries = archive.Entries;
                int totalCount = entries.Count;
                int progressed = 0;

                foreach (ZipArchiveEntry entry in entries)
                {
                    WaitProgressValue = archive.Entries.GetProgressValue(entry);
                    WaitProgressText = string.Format(_loader.GetString("UnzippingFormat"), ++progressed, totalCount);
                    if (string.IsNullOrWhiteSpace(entry.Name)) { continue; }
                    StorageFolder tempFolder = folder;
                    string[] parts = entry.FullName.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < parts.Length;)
                    {
                        string part = parts[i];
                        if (++i == parts.Length)
                        {
                            StorageFile file = await tempFolder.CreateFileAsync(part, CreationCollisionOption.ReplaceExisting);
                            using Stream stream = await file.OpenStreamForWriteAsync().ConfigureAwait(false);
                            using Stream entryStream = entry.Open();
                            await entryStream.CopyToAsync(stream).ConfigureAwait(false);
                        }
                        else
                        {
                            tempFolder = await tempFolder.CreateFolderAsync(part, CreationCollisionOption.OpenIfExists);
                        }
                    }
                }

                WaitProgressValue = 0;
                WaitProgressIndeterminate = true;
                WaitProgressText = _loader.GetString("UnzipComplete");
            }
            ADBPath = Path.Combine(folder.Path, @"platform-tools\adb.exe");
        }

        private async Task InitializeADBAsync()
        {
            WaitProgressText = _loader.GetString("Loading");
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (_file != null || _url != null)
            {
                IAdbServer ADBServer = AdbServer.Instance;
                if (!await ADBServer.GetStatusAsync(default).ContinueWith(x => x.Result.IsRunning).ConfigureAwait(false))
                {
                    WaitProgressText = _loader.GetString("CheckingADB");
                    await CheckADBAsync().ConfigureAwait(false);
                startAdb:
                    WaitProgressText = _loader.GetString("StartingADB");
                    try
                    {
                        await ADBServer.StartServerAsync(ADBPath, restartServerIfNewer: false, default).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Warn(ex.ExceptionToMessage(), ex);
                        await CheckADBAsync(true).ConfigureAwait(false);
                        goto startAdb;
                    }
                }
                WaitProgressText = _loader.GetString("Loading");
                if (!await CheckDeviceAsync().ConfigureAwait(false))
                {
                    WaitProgressText = _loader.GetString("ConnectPairedDevices");
                    if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    {
                        if (ScanPairedDevice)
                        {
                            ZeroconfHelper.InitializeConnectListener();
                        }

                        if (!await CheckDeviceAsync().ConfigureAwait(false))
                        {
                            _ = new AdbClient().ConnectAsync(new DnsEndPoint("127.0.0.1", 58526)).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        _ = new AdbClient().ConnectAsync(new DnsEndPoint("127.0.0.1", 58526)).ConfigureAwait(false);
                    }
                    WaitProgressText = _loader.GetString("Loading");
                }
                ADBHelper.Monitor.DeviceListChanged -= OnDeviceListChanged;
                ADBHelper.Monitor.DeviceListChanged += OnDeviceListChanged;
            }
        }

        private async Task InitializeUIAsync()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (_file != null || _url != null)
            {
                WaitProgressText = _loader.GetString("Loading");
                if (_file != null)
                {
                    WaitProgressText = _loader.GetString("Analysis");
                    try
                    {
                        ApkInfo = await new OOPAAPTool().DecompileAsync(_file).ConfigureAwait(false);
                        _appLocaleName = ApkInfo.GetLocaleLabel();
                    }
                    catch (Exception ex)
                    {
                        SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Error(ex.ExceptionToMessage(), ex);
                        PackageError(ex.Message);
                        IsInitialized = true;
                        return;
                    }
                    WaitProgressText = _loader.GetString("Loading");
                }
                else
                {
                    ApkInfo ??= new ApkInfo();
                }
                if (ApkInfo?.IsEmpty == true && _file != null)
                {
                    PackageError(_loader.GetString("InvalidPackage"));
                }
                else
                {
                checkDevice:
                    WaitProgressText = _loader.GetString("Checking");
                    if (await CheckDeviceAsync().ConfigureAwait(false) && _device != null)
                    {
                        if (_file != null)
                        {
                            await CheckAPKAsync().ConfigureAwait(false);
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
                        if (_file != null)
                        {
                            ActionButtonEnable = false;
                            ActionButtonText = _loader.GetString("Install");
                            InfoMessage = _loader.GetString("WaitingDevice");
                            DeviceSelectButtonText = _loader.GetString("Devices");
                            AppName = string.Format(_loader.GetString("WaitingForInstallFormat"), _appLocaleName);
                            ActionVisibility = DeviceSelectVisibility = MessagesToUserVisibility = true;
                        }
                        else
                        {
                            CheckOnlinePackage();
                        }
                        if (ShowDialogs && await ShowDeviceDialogAsync())
                        {
                            goto checkDevice;
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
                FileSelectVisibility = CancelOperationVisibility = true;
                AppVersionVisibility = AppPublisherVisibility = AppCapabilitiesVisibility = false;
            }
            IsInitialized = true;
        }

        private async Task<bool> ShowDeviceDialogAsync()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (IsOnlyWSA)
            {
                WaitProgressText = _loader.GetString("FindingWSA");
                if (await Launcher.FindUriSchemeHandlersAsync("wsa").AsTask().ContinueWith(x => x.Result?.Count > 0).ConfigureAwait(false))
                {
                    await Dispatcher.ResumeForegroundAsync();
                    WaitProgressText = _loader.GetString("FoundWSA");
                    ContentDialogResult result = await new MarkdownDialog
                    {
                        Title = _loader.GetString("HowToConnect"),
                        DefaultButton = ContentDialogButton.Close,
                        CloseButtonText = _loader.GetString("IKnow"),
                        PrimaryButtonText = _loader.GetString("StartWSA"),
                        FallbackContent = _loader.GetString("HowToConnectInfo"),
                        ContentInfo = new GitInfo("Paving-Base", "APK-Installer", "screenshots", "Documents/Tutorials/How%20To%20Connect%20WSA", "How%20To%20Connect%20WSA.md")
                    }.SetXAMLRoot(_page).ShowAsync();
                    await ThreadSwitcher.ResumeBackgroundAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                    startWSA:
                        CancellationTokenSource tokenSource = new(TimeSpan.FromMinutes(5));
                        try
                        {
                            await Dispatcher.ResumeForegroundAsync();
                            WaitProgressText = _loader.GetString("LaunchingWSA");
                            _ = await Launcher.LaunchUriAsync(new Uri("wsa://"));
                            WaitProgressText = _loader.GetString("WaitingWSAStart");
                            await ThreadSwitcher.ResumeBackgroundAsync();
                            while (!await CheckDeviceAsync().ConfigureAwait(false))
                            {
                                tokenSource.Token.ThrowIfCancellationRequested();
                                _ = new AdbClient().ConnectAsync(new DnsEndPoint("127.0.0.1", 58526), tokenSource.Token).ConfigureAwait(false);
                                await Task.Delay(100).ConfigureAwait(false);
                            }
                            WaitProgressText = _loader.GetString("WSARunning");
                            return true;
                        }
                        catch (Exception ex) when (tokenSource.IsCancellationRequested)
                        {
                            SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Error(ex.ExceptionToMessage(), ex);
                            await Dispatcher.ResumeForegroundAsync();
                            ContentDialogResult results = await new ContentDialog
                            {
                                Title = _loader.GetString("CannotConnectWSA"),
                                DefaultButton = ContentDialogButton.Close,
                                CloseButtonText = _loader.GetString("IKnow"),
                                PrimaryButtonText = _loader.GetString("Retry"),
                                Content = _loader.GetString("CannotConnectWSAInfo"),
                            }.SetXAMLRoot(_page).ShowAsync();
                            await ThreadSwitcher.ResumeBackgroundAsync();
                            if (results == ContentDialogResult.Primary)
                            {
                                goto startWSA;
                            }
                        }
                        catch (Exception e)
                        {
                            SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Warn(e.ExceptionToMessage(), e);
                            await Dispatcher.ResumeForegroundAsync();
                            ContentDialog dialogs = new()
                            {
                                XamlRoot = _page?.XamlRoot,
                                Title = _loader.GetString("CannotConnectWSA"),
                                DefaultButton = ContentDialogButton.Close,
                                CloseButtonText = _loader.GetString("IKnow"),
                                PrimaryButtonText = _loader.GetString("Retry"),
                                Content = new TextBlock
                                {
                                    Text = e.Message,
                                    IsTextSelectionEnabled = true
                                },
                            };
                            ContentDialogResult results = await dialogs.ShowAsync();
                            await ThreadSwitcher.ResumeBackgroundAsync();
                            if (results == ContentDialogResult.Primary)
                            {
                                goto startWSA;
                            }
                        }
                    }
                }
                else
                {
                    await Dispatcher.ResumeForegroundAsync();
                    ContentDialogResult result = await new ContentDialog
                    {
                        Title = _loader.GetString("NoDevice10"),
                        DefaultButton = ContentDialogButton.Primary,
                        CloseButtonText = _loader.GetString("IKnow"),
                        PrimaryButtonText = _loader.GetString("InstallWSA"),
                        SecondaryButtonText = _loader.GetString("GoToSetting"),
                        Content = _loader.GetString("NoDeviceInfo"),
                    }.SetXAMLRoot(_page).ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        _ = await Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?ProductId=9P3395VX91NR&mode=mini"));
                    }
                    else if (result == ContentDialogResult.Secondary)
                    {
                        _page.Frame?.Navigate(typeof(SettingsPage), null);
                    }
                }
            }
            else
            {
                await Dispatcher.ResumeForegroundAsync();
                ContentDialogResult result = await new MarkdownDialog
                {
                    Title = _loader.GetString("NoDevice"),
                    DefaultButton = ContentDialogButton.Close,
                    CloseButtonText = _loader.GetString("IKnow"),
                    PrimaryButtonText = _loader.GetString("GoToSetting"),
                    FallbackContent = _loader.GetString("NoDeviceInfo10"),
                    ContentInfo = new GitInfo("Paving-Base", "APK-Installer", "screenshots", "Documents/Tutorials/How%20To%20Connect%20Device", "How%20To%20Connect%20Device.md")
                }.SetXAMLRoot(_page).ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    _page.Frame?.Navigate(typeof(SettingsPage), null);
                }
            }
            return false;
        }

        private async Task ReinitializeUIAsync()
        {
            WaitProgressText = _loader.GetString("Loading");
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (_file != null || _url != null)
            {
            checkDevice:
                if (await CheckDeviceAsync().ConfigureAwait(false) && _device != null)
                {
                    await CheckAPKAsync().ConfigureAwait(false);
                }
                else
                {
                    if (ApkInfo == null)
                    {
                        await InitializeUIAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        ResetUI();
                        ActionButtonEnable = false;
                        ActionButtonText = _loader.GetString("Install");
                        InfoMessage = _loader.GetString("WaitingDevice");
                        DeviceSelectButtonText = _loader.GetString("Devices");
                        AppName = string.Format(_loader.GetString("WaitingForInstallFormat"), _appLocaleName);
                        ActionVisibility = DeviceSelectVisibility = MessagesToUserVisibility = true;

                        if (ShowDialogs)
                        {
                            if (await ShowDeviceDialogAsync().ConfigureAwait(false))
                            {
                                goto checkDevice;
                            }
                        }
                    }
                }
            }
        }

        private async Task CheckAPKAsync()
        {
            ResetUI();
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (this._device is DeviceData _device)
            {
                try
                {
                    AdbClient client = new();
                    VersionInfo info = default;
                    if (ApkInfo?.IsEmpty == false)
                    {
                        info = await client.GetPackageVersionAsync(_device, ApkInfo?.PackageName);
                    }
                    if (info == default)
                    {
                        ActionButtonText = _loader.GetString("Install");
                        AppName = string.Format(_loader.GetString("InstallFormat"), _appLocaleName);
                        ActionVisibility = true;
                        LaunchWhenReadyVisibility = !string.IsNullOrWhiteSpace(ApkInfo?.LaunchableActivity);
                    }
                    else if (info.VersionCode < int.Parse(ApkInfo?.VersionCode))
                    {
                        ActionButtonText = _loader.GetString("Update");
                        AppName = string.Format(_loader.GetString("UpdateFormat"), _appLocaleName);
                        ActionVisibility = true;
                        LaunchWhenReadyVisibility = !string.IsNullOrWhiteSpace(ApkInfo?.LaunchableActivity);
                    }
                    else
                    {
                        ActionButtonText = _loader.GetString("Reinstall");
                        SecondaryActionButtonText = _loader.GetString("Launch");
                        AppName = string.Format(_loader.GetString("ReinstallFormat"), _appLocaleName);
                        TextOutput = string.Format(_loader.GetString("ReinstallOutput"), _appLocaleName);
                        ActionVisibility = TextOutputVisibility = true;
                        SecondaryActionVisibility = !string.IsNullOrWhiteSpace(ApkInfo?.LaunchableActivity);
                    }
                    SDKInfo sdk = SDKInfo.GetInfo(await client.GetPropertyAsync(_device, "ro.build.version.sdk"));
                    if (sdk < ApkInfo.MinSDK)
                    {
                        ActionButtonEnable = false;
                        await Dispatcher.ResumeForegroundAsync();
                        _ = new ContentDialog
                        {
                            Content = string.Format(_loader.GetString("IncompatibleAppInfo"), ApkInfo?.MinSDK.ToString(), sdk.ToString()),
                            Title = _loader.GetString("IncompatibleApp"),
                            CloseButtonText = _loader.GetString("IKnow"),
                            DefaultButton = ContentDialogButton.Close
                        }.SetXAMLRoot(_page).ShowAsync();
                    }
                    return;
                }
                catch (Exception ex)
                {
                    SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Error(ex.ExceptionToMessage(), ex);
                }
            }
            ActionButtonEnable = false;
            ActionButtonText = _loader.GetString("Install");
            InfoMessage = _loader.GetString("WaitingDevice");
            DeviceSelectButtonText = _loader.GetString("Devices");
            AppName = string.Format(_loader.GetString("WaitingForInstallFormat"), _appLocaleName);
            ActionVisibility = DeviceSelectVisibility = MessagesToUserVisibility = true;
        } 

        private void CheckOnlinePackage()
        {
            Regex[] uriRegex = [new(@":\?source=(.*)"), new(@"://(.*)")];
            string uri = uriRegex[0].IsMatch(_url.ToString()) ? uriRegex[0].Match(_url.ToString()).Groups[1].Value : uriRegex[1].Match(_url.ToString()).Groups[1].Value;
            if (uri.TryGetUri(out Uri url))
            {
                _url = url;
                AppName = _loader.GetString("OnlinePackage");
                DownloadButtonText = _loader.GetString("Download");
                CancelOperationButtonText = _loader.GetString("Close");
                DownloadVisibility = CancelOperationVisibility = true;
                AppVersionVisibility = AppPublisherVisibility = AppCapabilitiesVisibility = false;
                if (AutoGetNetAPK)
                {
                    _ = LoadNetAPKAsync();
                }
            }
            else
            {
                PackageError(_loader.GetString("InvalidURL"));
            }
        }

        public async Task LoadNetAPKAsync()
        {
            IsInstalling = true;
            DownloadVisibility = false;
            await ThreadSwitcher.ResumeBackgroundAsync();
            try
            {
                await DownloadAPKAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Error(ex.ExceptionToMessage(), ex);
                PackageError(ex.Message);
                IsInstalling = false;
                return;
            }

            try
            {
                ApkInfo = await new OOPAAPTool().DecompileAsync(_file).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Error(ex.ExceptionToMessage(), ex);
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
                if (await CheckDeviceAsync().ConfigureAwait(false) && _device != null)
                {
                    await CheckAPKAsync().ConfigureAwait(false);
                }
                else
                {
                    ResetUI();
                    ActionButtonEnable = false;
                    ActionButtonText = _loader.GetString("Install");
                    InfoMessage = _loader.GetString("WaitingDevice");
                    DeviceSelectButtonText = _loader.GetString("Devices");
                    AppName = string.Format(_loader.GetString("WaitingForInstallFormat"), _appLocaleName);
                    ActionVisibility = DeviceSelectVisibility = MessagesToUserVisibility = true;
                }
            }
            IsInstalling = false;
        }

        private async Task DownloadAPKAsync()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (_url != null)
            {
                if (!Directory.Exists(CachesHelper.TempPath))
                {
                    _ = Directory.CreateDirectory(CachesHelper.TempPath);
                }
                else if (Directory.Exists(APKTemp))
                {
                    Directory.Delete(APKTemp, true);
                }
                using (DownloadService downloader = new(DownloadHelper.Configuration))
                {
                    bool isCompleted = false;
                    Exception exception = null;
                    long receivedBytesSize = 0;
                    long totalBytesToReceive = 0;
                    double progressPercentage = 0;
                    double bytesPerSecondSpeed = 0;
                    void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
                    {
                        exception = e.Error;
                        isCompleted = true;
                    }
                    void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
                    {
                        receivedBytesSize = e.ReceivedBytesSize;
                        progressPercentage = e.ProgressPercentage;
                        totalBytesToReceive = e.TotalBytesToReceive;
                        bytesPerSecondSpeed = e.BytesPerSecondSpeed;
                    }
                    downloader.DownloadFileCompleted += OnDownloadFileCompleted;
                    downloader.DownloadProgressChanged += OnDownloadProgressChanged;
                downloadApk:
                    ProgressText = _loader.GetString("WaitDownload");
                    _ = downloader.DownloadFileTaskAsync(_url.ToString(), APKTemp);
                    while (totalBytesToReceive <= 0)
                    {
                        await Task.Delay(1);
                        if (isCompleted)
                        {
                            goto downloadFinish;
                        }
                    }
                    AppxInstallBarIndeterminate = false;
                    while (!isCompleted)
                    {
                        ProgressText = $"{progressPercentage:N2}% {((double)bytesPerSecondSpeed).GetSizeString()}/s";
                        AppxInstallBarValue = progressPercentage;
                        await Task.Delay(1);
                    }
                    ProgressText = _loader.GetString("Loading");
                    AppxInstallBarIndeterminate = true;
                    AppxInstallBarValue = 0;
                downloadFinish:
                    if (exception != null)
                    {
                        await Dispatcher.ResumeForegroundAsync();
                        ContentDialogResult result = await new ContentDialog
                        {
                            Content = exception.Message,
                            Title = _loader.GetString("DownloadFailed"),
                            PrimaryButtonText = _loader.GetString("Retry"),
                            CloseButtonText = _loader.GetString("Cancel"),
                            DefaultButton = ContentDialogButton.Primary
                        }.SetXAMLRoot(_page).ShowAsync();
                        await ThreadSwitcher.ResumeBackgroundAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            goto downloadApk;
                        }
                        else
                        {
                            SendResults(new Exception($"APK {_loader.GetString("DownloadFailed")}"));
                            await Dispatcher.ResumeForegroundAsync();
                            await ExitApplicationAsync().ConfigureAwait(false);
                            return;
                        }
                    }
                    _file = await StorageFile.GetFileFromPathAsync(APKTemp);
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
            MessagesToUserVisibility = false;
            AppVersionVisibility =
            AppPublisherVisibility =
            AppCapabilitiesVisibility = true;
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
            TextOutputVisibility = InstallOutputVisibility = true;
            AppVersionVisibility = AppPublisherVisibility = AppCapabilitiesVisibility = false;
        }

        public async Task RegisterDeviceMonitor()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (await AdbServer.Instance.GetStatusAsync(default).ContinueWith(x => x.Result.IsRunning).ConfigureAwait(false))
            {
                ADBHelper.Monitor.DeviceListChanged -= OnDeviceListChanged;
                ADBHelper.Monitor.DeviceListChanged += OnDeviceListChanged;
            }
        }

        public async Task UnregisterDeviceMonitor()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (await AdbServer.Instance.GetStatusAsync(default).ContinueWith(x => x.Result.IsRunning).ConfigureAwait(false))
            { ADBHelper.Monitor.DeviceListChanged -= OnDeviceListChanged; }
        }

        private async void OnDeviceListChanged(object sender, DeviceDataNotifyEventArgs e)
        {
            if (IsInitialized && !IsInstalling)
            {
                await CheckDeviceAsync().ConfigureAwait(false);
                await CheckAPKAsync().ConfigureAwait(false);
            }
        }

        private async Task<bool> CheckDeviceAsync(bool forces = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            AdbClient client = new();
            IEnumerable<DeviceData> devices = await client.GetDevicesAsync().ConfigureAwait(false);
            ConsoleOutputReceiver receiver = new();
            if (devices?.Any() != true) { return false; }
            foreach (DeviceData device in devices)
            {
                if (device == null || forces ? device.State == DeviceState.Offline : device.State != DeviceState.Online) { continue; }
                if (IsOnlyWSA)
                {
                    await client.ExecuteRemoteCommandAsync("getprop ro.boot.hardware", device, receiver).ConfigureAwait(false);
                    if (receiver.ToString().Contains("windows"))
                    {
                        _device = device;
                        return true;
                    }
                }
                else
                {
                    DeviceData data = SettingsHelper.Get<DeviceData>(SettingsHelper.DefaultDevice);
                    if (data != null && data.Name == device.Name && data.Model == device.Model && data.Product == device.Product)
                    {
                        SettingsHelper.Set(SettingsHelper.DefaultDevice, device);
                        _device = device;
                        return true;
                    }
                }
            }
            _device = null;
            return false;
        }

        public Task OpenAPPAsync() => new AdbClient().StartAppAsync(_device.GetValueOrDefault(), ApkInfo?.PackageName);

        public async Task InstallAPPAsync()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            if (this._device is DeviceData _device)
            {
                try
                {
                    AdbClient client = new();
                    VersionInfo info = default;
                    if (ApkInfo != null && !ApkInfo.IsEmpty)
                    {
                        info = await client.GetPackageVersionAsync(_device, ApkInfo.PackageName).ConfigureAwait(false);
                    }
                    if (info != default && info.VersionCode >= int.Parse(ApkInfo.VersionCode))
                    {
                        await Dispatcher.ResumeForegroundAsync();
                        ContentDialog dialog = new()
                        {
                            XamlRoot = _page?.XamlRoot,
                            Content = string.Format(_loader.GetString("HasNewerVersionInfo"), info.VersionName, ApkInfo.VersionName),
                            Title = _loader.GetString("HasNewerVersion"),
                            PrimaryButtonText = _loader.GetString("Reinstall"),
                            CloseButtonText = _loader.GetString("Cancel"),
                            DefaultButton = ContentDialogButton.Close
                        };
                        ContentDialogResult result = await dialog.ShowAsync();
                        await ThreadSwitcher.ResumeBackgroundAsync();
                        if (result != ContentDialogResult.Primary) { return; }
                    }
                    IsInstalling = true;
                    AppxInstallBarIndeterminate = true;
                    ProgressText = _loader.GetString("Installing");
                    CancelOperationButtonText = _loader.GetString("Cancel");
                    CancelOperationVisibility = true;
                    ActionVisibility = SecondaryActionVisibility = TextOutputVisibility = InstallOutputVisibility = false;
                    LaunchWhenReadyVisibility = !string.IsNullOrWhiteSpace(ApkInfo.LaunchableActivity);

                    switch (ApkInfo, IsUploadAPK)
                    {
                        case ({ IsSplit: true }, true):
                            await client.InstallMultiplePackageAsync(_device, [ApkInfo.FullPath], ApkInfo.PackageName, OnInstallProgressChanged, default, "-r", "-t").ConfigureAwait(false);
                            break;
                        case ({ IsSplit: true }, false):
                            using (IRandomAccessStreamWithContentType apk = await StorageFile.GetFileFromPathAsync(ApkInfo.FullPath).AsTask().ContinueWith(x => x.Result.OpenReadAsync().AsTask()).Unwrap().ConfigureAwait(false))
                            {
                                await client.InstallMultipleAsync(_device, [apk], ApkInfo.PackageName, OnInstallProgressChanged, default, "-r", "-t").ConfigureAwait(false);
                            }
                            break;
                        case ({ IsBundle: true }, true) when await SelectSplitAsync(ApkInfo.SplitApks).ConfigureAwait(false) is { Length: > 0 } selects:
                            IEnumerable<string> strings = selects.Select(x => x.FullPath);
                            await client.InstallMultiplePackageAsync(_device, ApkInfo.FullPath, strings, OnInstallProgressChanged, default, "-r", "-t").ConfigureAwait(false);
                            break;
                        case ({ IsBundle: true }, false) when await SelectSplitAsync(ApkInfo.SplitApks).ConfigureAwait(false) is { Length: > 0 } selects:
                            using (IRandomAccessStreamWithContentType apk = await StorageFile.GetFileFromPathAsync(ApkInfo.FullPath).AsTask().ContinueWith(x => x.Result.OpenReadAsync().AsTask()).Unwrap().ConfigureAwait(false))
                            {
                                IRandomAccessStreamWithContentType[] splits = await Task.WhenAll(selects.Select(x => StorageFile.GetFileFromPathAsync(x.FullPath).AsTask().ContinueWith(x => x.Result.OpenReadAsync().AsTask()).Unwrap())).ConfigureAwait(false);
                                await client.InstallMultipleAsync(_device, apk, splits, OnInstallProgressChanged, default, "-r", "-t").ConfigureAwait(false);
                                Array.ForEach(splits, x => x.Dispose());
                            }
                            break;
                        case ({ IsBundle: true }, _):
                            goto default;
                        case (not null, true):
                            await client.InstallPackageAsync(_device, ApkInfo.FullPath, OnInstallProgressChanged, default, "-r", "-t").ConfigureAwait(false);
                            break;
                        case (not null, false):
                            using (IRandomAccessStreamWithContentType apk = await StorageFile.GetFileFromPathAsync(ApkInfo.FullPath).AsTask().ContinueWith(x => x.Result.OpenReadAsync().AsTask()).Unwrap().ConfigureAwait(false))
                            {
                                await client.InstallAsync(_device, apk, OnInstallProgressChanged, default, "-r", "-t").ConfigureAwait(false);
                            }
                            break;
                        default:
                            IsInstalling = false;
                            _ = CheckAPKAsync();
                            return;
                    }
                    AppName = string.Format(_loader.GetString("InstalledFormat"), _appLocaleName);
                    if (IsOpenApp && !string.IsNullOrWhiteSpace(ApkInfo?.LaunchableActivity))
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(1000).ConfigureAwait(false); // 据说如果安装完直接启动会崩溃。。。
                            await OpenAPPAsync().ConfigureAwait(false);
                            if (IsCloseAPP)
                            {
                                await Task.Delay(5000).ConfigureAwait(false);
                                await ExitApplicationAsync().ConfigureAwait(false);
                            }
                        });
                    }
                    SendResults();
                    IsInstalling = false;
                    AppxInstallBarValue = 0;
                    AppxInstallBarIndeterminate = true;
                    ActionButtonText = _loader.GetString("Reinstall");
                    SecondaryActionButtonText = _loader.GetString("Launch");
                    ActionVisibility = true;
                    CancelOperationVisibility = LaunchWhenReadyVisibility = false;
                    SecondaryActionVisibility = !string.IsNullOrWhiteSpace(ApkInfo?.LaunchableActivity);
                }
                catch (Exception ex)
                {
                    SendResults(ex);
                    IsInstalling = false;
                    TextOutput = ex.Message;
                    TextOutputVisibility = InstallOutputVisibility = true;
                    SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Error(ex.ExceptionToMessage(), ex);
                    ActionVisibility = SecondaryActionVisibility = CancelOperationVisibility = LaunchWhenReadyVisibility = false;
                }
            }

            _page.CancelFlyout.Hide();

            void OnInstallProgressChanged(InstallProgressEventArgs e)
            {
                switch (e.State)
                {
                    case PackageInstallProgressState.Uploading:
                        AppxInstallBarIndeterminate = false;
                        AppxInstallBarValue = e.UploadProgress;
                        ProgressText = string.Format(_loader.GetString("UploadingFormat"), $"{e.UploadProgress:N0}%", e.PackageFinished, e.PackageRequired);
                        break;
                    case PackageInstallProgressState.CreateSession:
                        AppxInstallBarIndeterminate = true;
                        ProgressText = _loader.GetString("CreateSession");
                        break;
                    case PackageInstallProgressState.WriteSession:
                        double value = e.PackageFinished * 100 / Math.Max(e.PackageRequired, 1);
                        AppxInstallBarValue = value;
                        AppxInstallBarIndeterminate = false;
                        ProgressText = string.Format(_loader.GetString("WriteSessionFormat"), e.PackageFinished, e.PackageRequired);
                        break;
                    case PackageInstallProgressState.Installing:
                        AppxInstallBarIndeterminate = true;
                        ProgressText = _loader.GetString("Installing");
                        break;
                    case PackageInstallProgressState.PostInstall:
                        value = e.PackageFinished * 100 / Math.Max(e.PackageRequired, 1);
                        AppxInstallBarValue = value;
                        AppxInstallBarIndeterminate = false;
                        ProgressText = string.Format(_loader.GetString("PostInstallFormat"), e.PackageFinished, e.PackageRequired);
                        break;
                    case PackageInstallProgressState.Finished:
                        AppxInstallBarValue = 100;
                        AppxInstallBarIndeterminate = false;
                        ProgressText = _loader.GetString("Finished");
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task<ApkInfo[]> SelectSplitAsync(ICollection<ApkInfo> apks)
        {
            if (this._device is DeviceData _device)
            {
                AdbClient client = new();
                SplitAPKSelector[] results = apks.Select(x => new SplitAPKSelector(x)).ToArray();
                try
                {
                    int? density = null;
                    string abi = null, locale = null;
                    List<SplitAPKSelector> densities = [];
                    foreach (SplitAPKSelector selector in results)
                    {
                        ApkInfo apk = selector.Package;
                        switch (apk)
                        {
                            case { SupportedABIs.Count: > 0 } when await IsIncludeABIAsync(apk).ConfigureAwait(false):
                                goto default;
                            case { SupportedABIs.Count: > 0 }:
                                continue;
                            case { SupportLocales.Count: > 0 } when await IsIncludeLocaleAsync(apk).ConfigureAwait(false):
                                goto default;
                            case { SupportLocales.Count: > 0 }:
                                continue;
                            case { SupportDensities.Count: > 0 } when !apk.SplitName.EndsWith("dpi", StringComparison.OrdinalIgnoreCase):
                                goto default;
                            case { SupportDensities.Count: > 0 }:
                                await ProcessDensityAsync(selector).ConfigureAwait(false);
                                continue;
                            default:
                                selector.IsSelected = true;
                                continue;
                        }

                        async Task<bool> IsIncludeABIAsync(ApkInfo apk)
                        {
                            if (abi == null)
                            {
                                ConsoleOutputReceiver receiver = new();
                                await client.ExecuteRemoteCommandAsync("getprop ro.product.cpu.abi", _device, receiver).ConfigureAwait(false);
                                abi = receiver.ToString().Trim();
                            }
                            return apk.SupportedABIs.Exists(x => x.Equals(abi, StringComparison.OrdinalIgnoreCase));
                        }

                        async Task<bool> IsIncludeLocaleAsync(ApkInfo apk)
                        {
                            if (locale == null)
                            {
                                ConsoleOutputReceiver receiver = new();
                                await client.ExecuteRemoteCommandAsync("getprop persist.sys.locale", _device, receiver).ConfigureAwait(false);
                                locale = receiver.ToString().Trim();
                            }
                            string country = locale.Split('-').FirstOrDefault();
                            return !string.IsNullOrWhiteSpace(country) && apk.SupportLocales.Exists(x => x.StartsWith(country, StringComparison.OrdinalIgnoreCase));
                        }

                        async Task ProcessDensityAsync(SplitAPKSelector selector)
                        {
                            if (density is not int num)
                            {
                                ConsoleOutputReceiver receiver = new();
                                await client.ExecuteRemoteCommandAsync("getprop ro.sf.lcd_density", _device, receiver).ConfigureAwait(false);
                                density = int.TryParse(receiver.ToString().Trim(), out num) ? num : 0;
                            }
                            if (selector.Package.SupportDensities.Exists(x => int.TryParse(x, out int density) && density >= num))
                            {
                                densities.Add(selector);
                            }
                        }
                    }

                    static Density GetDeviceDensity(int density)
                    {
                        Density selectedDensity = Density.MDPI;
                        foreach (Density item in Enum.GetValues(typeof(Density)).OfType<Density>())
                        {
                            int currentABS = Math.Abs(density - ((int)item));
                            int previousABS = Math.Abs(density - ((int)selectedDensity));

                            switch (currentABS.CompareTo(previousABS))
                            {
                                case 1:
                                    break;
                                case 0:
                                    selectedDensity = item > selectedDensity ? item : selectedDensity;
                                    break;
                                case -1:
                                    selectedDensity = item;
                                    break;
                            }
                        }
                        return selectedDensity;
                    }

                    if (densities.Count > 0)
                    {
                        int num = density ?? 0;
                        Density dpi = GetDeviceDensity(num);
                        foreach (SplitAPKSelector item in densities)
                        {
                            ApkInfo apk = item.Package;

                            if (apk.SplitName.Substring(apk.SplitName.Length - dpi.ToString().Length).Equals(dpi.ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                item.IsSelected = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Error(ex.ExceptionToMessage(), ex);
                }
                await Dispatcher.ResumeForegroundAsync();
                SplitAPKDialog dialog = new SplitAPKDialog(results)
                {
                    Title = _loader.GetString("SelectSplitPackage"),
                    CloseButtonText = _loader.GetString("Cancel"),
                    PrimaryButtonText = _loader.GetString("Install")
                }.SetXAMLRoot(_page);
                ContentDialogResult result = await dialog.ShowAsync();
                await ThreadSwitcher.ResumeBackgroundAsync();
                return result == ContentDialogResult.Primary
                    ? [.. results.Where(x => x.IsSelected).Select(x => x.Package)]
                    : [];
            }
            return null;
        }

        private enum Density
        {
            LDPI = 120,
            MDPI = 160,
            HDPI = 240,
            XHDPI = 320,
            XXHDPI = 480,
            XXXHDPI = 640,
            TVDPI = 213
        } 

        public async Task OpenAPKAsync(StorageFile file)
        {
            if (file != null)
            {
                _file = file;
                await Refresh().ConfigureAwait(false);
            }
        }

        public async Task OpenAPKAsync()
        {
            await Dispatcher.ResumeForegroundAsync();

            FileOpenPicker FileOpen = new()
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            FileOpen.FileTypeFilter.Add(".apk");
            FileOpen.FileTypeFilter.Add(".apks");
            FileOpen.FileTypeFilter.Add(".apkm");
            FileOpen.FileTypeFilter.Add(".xapk");

            StorageFile file = await FileOpen.PickSingleFileAsync();
            if (file != null)
            {
                _file = file;
                await Refresh().ConfigureAwait(false);
            }
        }

        public async Task OpenAPKAsync(IReadOnlyList<IStorageItem> items)
        {
            WaitProgressText = _loader.GetString("CheckingPath");
            IsInitialized = false;
            if (items.Count == 1)
            {
                IStorageItem storageItem = items.FirstOrDefault();
                await OpenPathAsync(storageItem).ConfigureAwait(false);
                return;
            }
            else if (items.Count >= 1)
            {
                await CreateAPKSAsync(items).ConfigureAwait(false);
                return;
            }
            IsInitialized = true;
        }

        public async Task OpenAPKAsync(DataPackageView data)
        {
            if (data == null) { return; }

            WaitProgressText = _loader.GetString("CheckingPath");
            IsInitialized = false;

            await ThreadSwitcher.ResumeBackgroundAsync();
            if (data.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await data.GetStorageItemsAsync();
                if (items.Count == 1)
                {
                    IStorageItem storageItem = items.FirstOrDefault();
                    await OpenPathAsync(storageItem).ConfigureAwait(false);
                    return;
                }
                else if (items.Count >= 1)
                {
                    await CreateAPKSAsync(items).ConfigureAwait(false);
                    return;
                }
            }
            else if (data.Contains(StandardDataFormats.Text))
            {
                string path = await data.GetTextAsync();
                if (Directory.Exists(path))
                {
                    StorageFolder storageItem = await StorageFolder.GetFolderFromPathAsync(path);
                    await OpenPathAsync(storageItem).ConfigureAwait(false);
                    return;
                }
                else if (File.Exists(path))
                {
                    StorageFile storageItem = await StorageFile.GetFileFromPathAsync(path);
                    await OpenPathAsync(storageItem).ConfigureAwait(false);
                    return;
                }
            }
            IsInitialized = true;
        }

        private async Task OpenPathAsync(IStorageItem storageItem)
        {
            switch (storageItem)
            {
                case StorageFolder folder:
                    IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
                    await CreateAPKSAsync(files).ConfigureAwait(false);
                    return;
                case StorageFile file:
                    if (file.FileType.Equals(".apk", StringComparison.OrdinalIgnoreCase))
                    {
                        await OpenAPKAsync(file).ConfigureAwait(false);
                        return;
                    }
                    try
                    {
                        using Stream stream = await file.OpenStreamForReadAsync().ConfigureAwait(false);
                        using (ZipArchive archive = new(stream, ZipArchiveMode.Read))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries.Where(x => !x.FullName.Contains('/')))
                            {
                                if (entry.Name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                                {
                                    await OpenAPKAsync(file).ConfigureAwait(false);
                                    return;
                                }
                            }
                        }
                        IsInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Info(ex.ExceptionToMessage(), ex);
                    }
                    break;
            }
            IsInitialized = true;
        }

        private async Task CreateAPKSAsync(IReadOnlyList<IStorageItem> items)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            List<StorageFile> apkList = [];
            foreach (StorageFile file in items.OfType<StorageFile>())
            {
                if (file.FileType.Equals(".apk", StringComparison.OrdinalIgnoreCase))
                {
                    apkList.Add(file);
                    continue;
                }
                try
                {
                    using Stream stream = await file.OpenStreamForReadAsync().ConfigureAwait(false);
                    using ZipArchive archive = new(stream, ZipArchiveMode.Read);
                    foreach (ZipArchiveEntry entry in archive.Entries.Where(x => !x.FullName.Contains('/')))
                    {
                        if (entry.Name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                        {
                            await OpenAPKAsync(file).ConfigureAwait(false);
                            return;
                        }
                    }
                    continue;
                }
                catch (Exception ex)
                {
                    SettingsHelper.LogManager.GetLogger(nameof(InstallViewModel)).Info(ex.ExceptionToMessage(), ex);
                    continue;
                }
            }

            if (apkList.Count == 1)
            {
                await OpenAPKAsync(apkList.FirstOrDefault()).ConfigureAwait(false);
                return;
            }
            else if (apkList.Count >= 1)
            {
                string tempBase = CachesHelper.TempPath;
                string temp = Path.Combine(tempBase, $"TempSplitAPK.apks");

                if (!Directory.Exists(tempBase))
                {
                    _ = Directory.CreateDirectory(tempBase);
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
                    using ZipArchive zipWriter = new(zip, ZipArchiveMode.Create);
                    foreach (StorageFile apk in apkList)
                    {
                        using Stream stream = await apk.OpenStreamForReadAsync().ConfigureAwait(false);
                        using Stream entry = zipWriter.CreateEntry(apk.Name).Open();
                        await stream.CopyToAsync(entry);
                    }
                }

                await StorageFile.GetFileFromPathAsync(temp)
                                 .AsTask()
                                 .ContinueWith(x => OpenAPKAsync(x.Result))
                                 .Unwrap()
                                 .ConfigureAwait(false);
                return;
            }

            IsInitialized = true;
        }

        private void SendResults(Exception exception = null)
        {
            if (_operation == null) { return; }
            ValueSet results = new()
            {
                ["Result"] = exception != null,
                ["Exception"] = exception.ExceptionToMessage()
            };
            _operation.ReportCompleted(results);
        }

        public async Task CloseAPPAsync()
        {
            SendResults(new Exception($"{_loader.GetString("Install")} {_loader.GetString("Cancel")}"));
            await ExitApplicationAsync().ConfigureAwait(false);
        }

        private async Task ExitApplicationAsync()
        {
            await Dispatcher.ResumeForegroundAsync();
            if (MainPage.WindowCount > 1)
            {
                if (await ApplicationView.GetForCurrentView().TryConsolidateAsync())
                {
                    MainPage.WindowCount--;
                }
            }
            else
            {
                Application.Current.Exit();
            }
        }
    }
}
