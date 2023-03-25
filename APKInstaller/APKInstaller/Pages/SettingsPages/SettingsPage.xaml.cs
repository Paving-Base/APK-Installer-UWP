using AdvancedSharpAdbClient;
using APKInstaller.Controls;
using APKInstaller.Helpers;
using APKInstaller.Models;
using APKInstaller.ViewModels.SettingsPages;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace APKInstaller.Pages.SettingsPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        internal SettingsViewModel Provider;

        public SettingsPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (SettingsViewModel.Caches != null)
            {
                Provider = SettingsViewModel.Caches;
            }
            else
            {
                Provider = new SettingsViewModel(this);
                if (Provider.UpdateDate == DateTime.MinValue) { Provider.CheckUpdate(); }
            }
            if (AdbServer.Instance.GetStatus().IsRunning)
            {
                ADBHelper.Monitor.DeviceChanged += Provider.OnDeviceChanged;
                Provider.DeviceList = new AdbClient().GetDevices().Where(x => x.State == DeviceState.Online);
            }
            DataContext = Provider;
            Provider.GetADBVersion();
            //#if DEBUG
            GoToTestPage.Visibility = Visibility.Visible;
            //#endif
            SelectDeviceBox.SelectionMode = Provider.IsOnlyWSA ? ListViewSelectionMode.None : ListViewSelectionMode.Single;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (AdbServer.Instance.GetStatus().IsRunning) { ADBHelper.Monitor.DeviceChanged -= Provider.OnDeviceChanged; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag as string)
            {
                case "Pair":
                    Provider.PairDevice(ConnectIP.Text, PairCode.Text);
                    break;
                case "Rate":
                    _ = Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9P2JFQ43FPPG"));
                    break;
                case "Group":
                    _ = Launcher.LaunchUriAsync(new Uri("https://t.me/PavingBase"));
                    break;
                case "Reset":
                    Reset.Flyout?.Hide();
                    ApplicationData.Current.LocalSettings.Values.Clear();
                    SettingsHelper.SetDefaultSettings();
                    _ = Frame.Navigate(typeof(SettingsPage));
                    Frame.GoBack();
                    break;
                case "ADBPath":
                    Provider.ChangeADBPath();
                    break;
                case "Connect":
                    Provider.ConnectDevice(ConnectIP.Text);
                    break;
                case "TestPage":
                    _ = Frame.Navigate(typeof(TestPage));
                    break;
                case "PairDevice":
                    _ = Frame.Navigate(typeof(PairDevicePage));
                    break;
                case "CheckUpdate":
                    Provider.CheckUpdate();
                    break;
                case "WindowsColor":
                    _ = Launcher.LaunchUriAsync(new Uri("ms-settings:colors"));
                    break;
                case "CopyConnectInfo":
                    DataTransferHelper.CopyText(Provider.ConnectInfoTitle, "Connect Info");
                    break;
                default:
                    break;
            }
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag as string)
            {
                case "ADBPath":
                    _ = await Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(Provider.ADBPath.Substring(0, Provider.ADBPath.LastIndexOf(@"\"))));
                    break;
                case "LogFolder":
                    _ = await Launcher.LaunchFolderAsync(await ApplicationData.Current.LocalFolder.CreateFolderAsync("MetroLogs", CreationCollisionOption.OpenIfExists));
                    break;
                case "WindowsColor":
                    _ = Launcher.LaunchUriAsync(new Uri("ms-settings:colors"));
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

        private void SelectDeviceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object vs = (sender as ListView).SelectedItem;
            if (vs is not null and DeviceData device)
            {
                SettingsHelper.Set(SettingsHelper.DefaultDevice, device);
            }
        }

        private void InfoBar_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element?.FindDescendant("Title") is TextBlock title)
            {
                title.IsTextSelectionEnabled = true;
            }
            if (element?.FindDescendant("Message") is TextBlock message)
            {
                message.IsTextSelectionEnabled = true;
            }
        }

        private void GotoUpdate_Click(object sender, RoutedEventArgs e) => _ = Launcher.LaunchUriAsync(new Uri((sender as FrameworkElement).Tag.ToString()));

        private void MarkdownText_LinkClicked(object sender, LinkClickedEventArgs e) => _ = Launcher.LaunchUriAsync(new Uri(e.Link));
        
        private void WebXAML_Loaded(object sender, RoutedEventArgs e) => (sender as WebXAML).ContentInfo = new GitInfo("Paving-Base", "APK-Installer-UWP", "screenshots", "Documents/Announcements", "Announcements.xml");
    }
}
