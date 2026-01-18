using AdvancedSharpAdbClient;
using APKInstaller.Helpers;
using APKInstaller.Pages.SettingsPages;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace APKInstaller.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public static uint WindowCount { get; set; } = 0;

        public readonly string GetAppTitleFromSystem = ResourceLoader.GetForViewIndependentUse()?.GetString("AppName") ?? Package.Current.DisplayName;

        internal double TitleBarHeight => !UIHelper.HasTitleBar ? 32 : 0;

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (name != null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }

        public MainPage()
        {
            InitializeComponent();
            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequested;
            WindowCount++;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Window.Current.SetTitleBar(CustomTitleBar);
            _ = CoreAppFrame.Navigate(typeof(InstallPage), e.Parameter);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Name)
            {
                case "AboutButton":
                    _ = CoreAppFrame.Navigate(typeof(SettingsPage));
                    break;
                default:
                    break;
            }
        }

        private async void OnCloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            if (--WindowCount != 0) { return; }
            Deferral deferral = e.GetDeferral();
            await CleanCachesAsync();
            deferral.Complete();
        }

        private static async Task CleanCachesAsync()
        {
            if (ApiInformation.IsTypePresent("Windows.ApplicationModel.AppInstance")
                && AppInstance.GetInstances().Count > 1)
            {
                CachesHelper.CleanAllCaches(false);
            }
            else
            {
                CachesHelper.CleanAllCaches(true);
                if (ADBHelper.IsRunning && SettingsHelper.Get<bool>(SettingsHelper.IsCloseADB))
                {
                    try { await new AdbClient().KillAdbAsync(); } catch { }
                }
            }
        }

        internal void UpdateTitleBarHeight() => RaisePropertyChangedEvent(nameof(TitleBarHeight));
    }
}
