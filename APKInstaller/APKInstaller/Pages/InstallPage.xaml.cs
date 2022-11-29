using APKInstaller.Helpers;
using APKInstaller.Pages.SettingsPages;
using APKInstaller.ViewModels;
using System;
using System.Linq;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
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
        private bool IsCaches;
        internal InstallViewModel Provider;

        public InstallPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (InstallViewModel.Caches != null)
            {
                IsCaches = true;
                Provider = InstallViewModel.Caches;
            }
            else
            {
                IsCaches = false;
                string _path = string.Empty;
                if (e.Parameter is IActivatedEventArgs args)
                {
                    switch (args?.Kind)
                    {
                        case ActivationKind.File:
                            _path = (args as IFileActivatedEventArgs).Files.First().Path;
                            Provider = new InstallViewModel(_path, this);
                            break;
                        case ActivationKind.Protocol:
                            Provider = new InstallViewModel((args as IProtocolActivatedEventArgs).Uri, this);
                            break;
                        default:
                            Provider = new InstallViewModel(_path, this);
                            break;
                    }
                }
                else
                {
                    Provider = new InstallViewModel(_path, this);
                }
            }
            DataContext = Provider;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Provider.Dispose();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Name)
            {
                case "ActionButton":
                    Provider.InstallAPP();
                    break;
                case "DownloadButton":
                    Provider.LoadNetAPK();
                    break;
                case "FileSelectButton":
                    Provider.OpenAPK();
                    break;
                case "DeviceSelectButton":
                    Frame.Navigate(typeof(SettingsPage));
                    break;
                case "SecondaryActionButton":
                    Provider.OpenAPP();
                    break;
                case "CancelOperationButton":
                    Provider.CloseAPP();
                    break;
            }
        }

        private async void InitialLoadingUI_Loaded(object sender, RoutedEventArgs e)
        {
            await Provider.Refresh(!IsCaches);
        }

        private void CopyFileItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem element = sender as MenuFlyoutItem;
            DataTransferHelper.CopyFile(element.Tag.ToString(), element.Text);
        }

        private void CopyStringItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem element = sender as MenuFlyoutItem;
            DataTransferHelper.CopyText(element.Tag.ToString(), element.Text);
        }

        private void CopyBitmapItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem element = sender as MenuFlyoutItem;
            DataTransferHelper.CopyBitmap(element.Tag.ToString(), element.Text);
        }

        private void ShareFileItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem element = sender as MenuFlyoutItem;
            DataTransferHelper.ShareFile(element.Tag.ToString(), element.Text, element.Tag.ToString());
        }

        private void ShareUrlItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem element = sender as MenuFlyoutItem;
            DataTransferHelper.ShareURL(new Uri(element.Tag.ToString()), element.Text, element.Tag.ToString());
        }

        private void Page_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private void Page_Drop(object sender, DragEventArgs e)
        {
            Provider.OpenAPK(e.DataView);
            e.Handled = true;
        }
    }
}
