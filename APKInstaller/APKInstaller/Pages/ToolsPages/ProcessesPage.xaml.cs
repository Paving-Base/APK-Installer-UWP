using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using APKInstaller.Helpers;
using APKInstaller.ViewModels.ToolsPages;
using Microsoft.Toolkit.Uwp;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace APKInstaller.Pages.ToolsPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ProcessesPage : Page
    {
        private ProcessesViewModel Provider;

        public ProcessesPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Provider = new ProcessesViewModel(this);
            DataContext = Provider;
            Provider.TitleBar = TitleBar;
            ADBHelper.Monitor.DeviceChanged += OnDeviceChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ADBHelper.Monitor.DeviceChanged -= OnDeviceChanged;
        }

        private void OnDeviceChanged(object sender, DeviceDataEventArgs e) => _ = UIHelper.DispatcherQueue.EnqueueAsync(() => Provider.GetDevices());

        private void TitleBar_BackRequested(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AdvancedAdbClient client = new AdvancedAdbClient();
            Provider.Processes = DeviceExtensions.ListProcesses(client, Provider.devices[(sender as ComboBox).SelectedIndex]);
        }

        private async void TitleBar_RefreshEvent(object sender, RoutedEventArgs e)
        {
            TitleBar.ShowProgressRing();
            await UIHelper.DispatcherQueue.EnqueueAsync(() =>
            {
                Provider.GetDevices();
                TitleBar.ShowProgressRing();
                AdvancedAdbClient client = new AdvancedAdbClient();
                Provider.Processes = DeviceExtensions.ListProcesses(client, Provider.devices[DeviceComboBox.SelectedIndex]);
            });
            TitleBar.HideProgressRing();
        }

        private async void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            Provider.DeviceComboBox = sender as ComboBox;
            await UIHelper.DispatcherQueue.EnqueueAsync(() => Provider.GetDevices());
        }
    }
}
