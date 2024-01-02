using APKInstaller.Helpers;
using APKInstaller.Pages.SettingsPages;
using APKInstaller.Pages.ToolsPages;
using APKInstaller.ViewModels;
using System;
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
            }
            else if (e.Parameter is IActivatedEventArgs args)
            {
                switch (args?.Kind)
                {
                    case ActivationKind.File when args is IFileActivatedEventArgs fileActivatedEventArgs:
                        Provider = new InstallViewModel(file: null, this);
                        _ = Provider.OpenAPKAsync(fileActivatedEventArgs.Files);
                        break;
                    case ActivationKind.ShareTarget when args is IShareTargetActivatedEventArgs shareTargetEventArgs:
                        shareTargetEventArgs.ShareOperation.DismissUI();
                        Provider = new InstallViewModel(file: null, this);
                        _ = Provider.OpenAPKAsync(shareTargetEventArgs.ShareOperation.Data);
                        break;
                    case ActivationKind.Protocol when args is ProtocolActivatedEventArgs protocolArgs:
                        ValueSet protocolData = protocolArgs.Data;
                        Provider = protocolData?.Any() != true
                            ? new InstallViewModel(protocolArgs.Uri, this)
                            : protocolData.TryGetValue("Url", out object url)
                                ? new InstallViewModel(url as Uri, this)
                                : protocolData.TryGetValue("FilePath", out object filePath)
                                    ? new InstallViewModel(await StorageFile.GetFileFromPathAsync(filePath?.ToString()), this)
                                    : new InstallViewModel(protocolArgs.Uri, this);
                        break;
                    case ActivationKind.ProtocolForResults when args is ProtocolForResultsActivatedEventArgs protocolForResultsArgs:
                        ValueSet ProtocolForResultsData = protocolForResultsArgs.Data;
                        Provider = ProtocolForResultsData?.Any() != true
                            ? new InstallViewModel(protocolForResultsArgs.Uri, this, protocolForResultsArgs.ProtocolForResultsOperation)
                            : ProtocolForResultsData.TryGetValue("Url", out url)
                                ? new InstallViewModel(url as Uri, this, protocolForResultsArgs.ProtocolForResultsOperation)
                                : ProtocolForResultsData.TryGetValue("FilePath", out filePath)
                                    ? new InstallViewModel(await StorageFile.GetFileFromPathAsync(filePath?.ToString()), this, protocolForResultsArgs.ProtocolForResultsOperation)
                                    : new InstallViewModel(protocolForResultsArgs.Uri, this, protocolForResultsArgs.ProtocolForResultsOperation);
                        break;
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
                case nameof(ActionButton):
                    _ = Provider.InstallAPPAsync();
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
            DataTransferHelper.ShareURL(new Uri(element.Tag?.ToString()), element.Text, element.Tag?.ToString());
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
