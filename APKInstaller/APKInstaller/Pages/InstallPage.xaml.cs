using APKInstaller.Helpers;
using APKInstaller.Pages.SettingsPages;
using APKInstaller.Pages.ToolsPages;
using APKInstaller.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace APKInstaller.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class InstallPage : Page
    {
        #region Provider

        /// <summary>
        /// Identifies the <see cref="Provider"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ProviderProperty =
            DependencyProperty.Register(
                nameof(Provider),
                typeof(InstallViewModel),
                typeof(InstallPage),
                null);

        /// <summary>
        /// Get the <see cref="InstallViewModel"/> of current <see cref="Page"/>.
        /// </summary>
        public InstallViewModel Provider
        {
            get => (InstallViewModel)GetValue(ProviderProperty);
            private set => SetValue(ProviderProperty, value);
        }

        #endregion

        public InstallPage() => InitializeComponent();

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (Provider != null)
            {
                _ = Provider.Refresh(false);
                _ = Provider.RegisterDeviceMonitor();
                return;
            }
            else if (e.Parameter is IActivatedEventArgs args)
            {
                switch (args?.Kind)
                {
                    case ActivationKind.File when args is IFileActivatedEventArgs fileActivatedEventArgs:
                        Provider = new InstallViewModel(file: null, this);
                        _ = Provider.OpenAPKAsync(fileActivatedEventArgs.Files);
                        return;
                    case ActivationKind.ShareTarget when args is IShareTargetActivatedEventArgs shareTargetEventArgs:
                        Provider = new InstallViewModel(file: null, this);
                        _ = Provider.OpenAPKAsync(shareTargetEventArgs.ShareOperation.Data);
                        return;
                    case ActivationKind.Protocol when args is ProtocolActivatedEventArgs protocolArgs:
                        ValueSet protocolData = protocolArgs.Data;
                        if (protocolData?.Count >= 1)
                        {
                            if (protocolData.TryGetValue("Url", out object url))
                            {
                                Provider = new InstallViewModel(url as Uri, this);
                                break;

                            }
                            else if (protocolData.TryGetValue("FilePath", out object filePath))
                            {
                                try
                                {
                                    StorageFile file = await StorageFile.GetFileFromPathAsync(filePath?.ToString());
                                    Provider = new InstallViewModel(file, this);
                                    break;
                                }
                                catch(Exception ex)
                                {
                                    SettingsHelper.LoggerFactory.CreateLogger<InstallPage>().LogError(ex, "Failed to get file from path in protocol activation. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                                }
                            }
                        }
                        Provider = new InstallViewModel(protocolArgs.Uri, this);
                        break;
                    case ActivationKind.ProtocolForResults when args is ProtocolForResultsActivatedEventArgs protocolForResultsArgs:
                        ValueSet ProtocolForResultsData = protocolForResultsArgs.Data;
                        if (ProtocolForResultsData?.Count >= 1)
                        {
                            if (ProtocolForResultsData.TryGetValue("Url", out object url))
                            {
                                Provider = new InstallViewModel(url as Uri, this, protocolForResultsArgs.ProtocolForResultsOperation);
                                break;
                            }
                            else if (ProtocolForResultsData.TryGetValue("FilePath", out object filePath))
                            {
                                try
                                {
                                    StorageFile file = await StorageFile.GetFileFromPathAsync(filePath?.ToString());
                                    Provider = new InstallViewModel(file, this, protocolForResultsArgs.ProtocolForResultsOperation);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    SettingsHelper.LoggerFactory.CreateLogger<InstallPage>().LogError(ex, "Failed to get file from path in protocol activation. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                                }
                            }
                        }
                        Provider = new InstallViewModel(protocolForResultsArgs.Uri, this, protocolForResultsArgs.ProtocolForResultsOperation);
                        break;
                    case ActivationKind.CommandLineLaunch when args is ICommandLineActivatedEventArgs commandLineActivatedEventArgs:
                        try
                        {
                            CommandLineActivationOperation operation = commandLineActivatedEventArgs.Operation;
                            string[] arguments = operation.Arguments?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (arguments is [.., { Length: > 0 } path])
                            {
                                path = path.Trim('\'', '"');
                                if (!string.IsNullOrWhiteSpace(path))
                                {
                                    try
                                    {
                                        string basePath = operation.CurrentDirectoryPath;
                                        if (basePath[^1] != '\\')
                                        {
                                            basePath += "\\";
                                        }
                                        if (Uri.TryCreate(basePath, UriKind.Absolute, out Uri baseUri) && baseUri.IsFile)
                                        {
                                            if (Uri.TryCreate(baseUri, path, out Uri uri) && uri.IsFile)
                                            {
                                                StorageFile file = await StorageFile.GetFileFromPathAsync(uri.AbsoluteUri);
                                                Provider = new InstallViewModel(file, this);
                                            }
                                            else if (Uri.TryCreate(path, UriKind.Absolute, out uri) && uri.Scheme is "http" or "https")
                                            {
                                                Provider = new InstallViewModel(uri, this);
                                            }
                                        }
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        SettingsHelper.LoggerFactory.CreateLogger<InstallPage>().LogError(ex, "Failed to get file from path in command line activation. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            commandLineActivatedEventArgs.CompleteDeferral();
                        }
                        goto default;
                    default:
                        Provider = new InstallViewModel(file: null, this);
                        break;
                }
            }
            else
            {
                Provider = new InstallViewModel(file: null, this);
            }
            _ = Provider.Refresh(true);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _ = Provider.UnregisterDeviceMonitor();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element) { return; }
            switch (element.Name)
            {
                case nameof(ActionButton) when ADBHelper.IsRunning:
                    _ = Provider.InstallAPPAsync();
                    break;
                case "ActionButton":
                    _ = Provider.InitADBFileAsync();
                    break;
                case nameof(DownloadButton):
                    _ = Provider.LoadNetAPKAsync();
                    break;
                case nameof(FileSelectButton):
                    _ = Provider.OpenAPKAsync();
                    break;
                case nameof(MoreInfoFlyoutItem):
                    _ = Frame.Navigate(typeof(InformationPage), Provider.ApkInfo);
                    break;
                case nameof(DeviceSelectButton):
                    _ = Frame.Navigate(typeof(SettingsPage));
                    break;
                case nameof(CancelConfirmButton):
                    CancelFlyout.Hide();
                    _ = Provider.CloseAPPAsync();
                    break;
                case nameof(CancelContinueButton):
                    CancelFlyout.Hide();
                    break;
                case nameof(SecondaryActionButton):
                    _ = Provider.OpenAPPAsync();
                    break;
            }
        }

        private void CopyFileItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem element) { return; }
            DataTransferHelper.CopyFile(element.Tag?.ToString(), element.Text);
        }

        private void CopyStringItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem element) { return; }
            DataTransferHelper.CopyText(element.Tag?.ToString(), element.Text);
        }

        private void CopyBitmapItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem element) { return; }
            DataTransferHelper.CopyBitmap(element.Tag?.ToString(), element.Text);
        }

        private void ShareFileItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem element) { return; }
            DataTransferHelper.ShareFile(element.Tag?.ToString(), element.Text, element.Tag?.ToString());
        }

        private void ShareUrlItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem element) { return; }
            DataTransferHelper.ShareURL(element.Tag?.ToString().TryGetUri(), element.Text, element.Tag?.ToString());
        }

        private void Page_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private void Page_Drop(object sender, DragEventArgs e)
        {
            _ = Provider.OpenAPKAsync(e.DataView);
            e.Handled = true;
        }
    }
}
