using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using APKInstaller.Controls;
using APKInstaller.Helpers;
using APKInstaller.ViewModels.ToolsPages;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace APKInstaller.Pages.ToolsPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ApplicationsPage : Page
    {
        private readonly ApplicationsViewModel Provider;

        public ApplicationsPage()
        {
            InitializeComponent();
            Provider = new ApplicationsViewModel(Dispatcher) { TitleBar = TitleBar };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ADBHelper.Monitor.DeviceChanged += OnDeviceChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ADBHelper.Monitor.DeviceChanged -= OnDeviceChanged;
        }

        private void OnDeviceChanged(object sender, DeviceDataEventArgs e) => _ = Provider.GetDevicesAsync();

        private void TitleBar_BackRequested(TitleBar sender, object e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = Provider.GetAppsAsync();
        }

        private void TitleBar_RefreshEvent(TitleBar sender, object e)
        {
            _ = Provider.GetDevicesAsync().ContinueWith(x => Provider.GetAppsAsync()).Unwrap();
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement Button = sender as FrameworkElement;
            switch (Button.Name)
            {
                case "Stop":
                    _ = new AdbClient().StopAppAsync(Provider.devices[DeviceComboBox.SelectedIndex], Button.Tag.ToString());
                    break;
                case "Start":
                    _ = new AdbClient().StartAppAsync(Provider.devices[DeviceComboBox.SelectedIndex], Button.Tag.ToString());
                    break;
                case "Uninstall":
                    break;
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            Provider.DeviceComboBox = sender as ComboBox;
            _ = Provider.GetDevicesAsync();
        }
    }
}
