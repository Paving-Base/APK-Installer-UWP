using AAPTForNet.Models;
using APKInstaller.Controls;
using APKInstaller.Helpers;
using APKInstaller.Pages.SettingsPages;
using APKInstaller.ViewModels.SettingsPages;
using APKInstaller.ViewModels.ToolsPages;
using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace APKInstaller.Pages.ToolsPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class InfosPage : Page
    {
        #region Provider

        /// <summary>
        /// Identifies the <see cref="Provider"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ProviderProperty =
            DependencyProperty.Register(
                nameof(Provider),
                typeof(InfosViewModel),
                typeof(InfosPage),
                null);

        /// <summary>
        /// Get the <see cref="InfosViewModel"/> of current <see cref="Page"/>.
        /// </summary>
        public InfosViewModel Provider
        {
            get => (InfosViewModel)GetValue(ProviderProperty);
            private set => SetValue(ProviderProperty, value);
        }

        #endregion

        public InfosPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ApkInfo info)
            {
                Provider = new InfosViewModel(info);
            }
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag as string)
            {
                case "SharePackage":
                    DataTransferHelper.ShareFile(Provider.ApkInfo.PackagePath, Provider.ApkInfo.AppName);
                    break;
                case "OpenPackageFolder":
                    _ = await Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(Provider.ApkInfo.PackagePath[..Provider.ApkInfo.PackagePath.LastIndexOf(@"\")]));
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
