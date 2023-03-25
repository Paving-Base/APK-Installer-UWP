using AAPTForNet.Models;
using APKInstaller.Controls;
using APKInstaller.Helpers;
using APKInstaller.ViewModels.AboutPages;
using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace APKInstaller.Pages.AboutPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class InfosPage : Page
    {
        internal InfosViewModel Provider;

        public InfosPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ApkInfo info)
            {
                Provider = new InfosViewModel(info, this);
            }
            DataContext = Provider;
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag as string)
            {
                case "SharePackage":
                    DataTransferHelper.ShareFile(Provider.ApkInfo.PackagePath, Provider.ApkInfo.AppName);
                    break;
                case "OpenPackageFolder":
                    _ = await Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(Provider.ApkInfo.PackagePath.Substring(0, Provider.ApkInfo.PackagePath.LastIndexOf(@"\"))));
                    break;
                default:
                    break;
            }
        }

        private void TitleBar_BackRequested(TitleBar sender, object e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}
