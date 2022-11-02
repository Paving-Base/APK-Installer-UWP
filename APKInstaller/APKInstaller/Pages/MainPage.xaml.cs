using AdvancedSharpAdbClient;
using APKInstaller.Helpers;
using APKInstaller.Pages.SettingsPages;
using Windows.ApplicationModel;
using Windows.System;
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
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            UIHelper.MainPage = this;
            Window.Current.SetTitleBar(CustomTitleBar);
            UIHelper.DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequested;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
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

        private void OnCloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            if (AppInstance.GetInstances().Count <= 1)
            {
                CachesHelper.CleanAllCaches(true);

                if (SettingsHelper.Get<bool>(SettingsHelper.IsCloseADB))
                {
                    try { new AdvancedAdbClient().KillAdb(); } catch { }
                }
            }
            else
            {
                CachesHelper.CleanAllCaches(false);
            }
        }
    }
}
