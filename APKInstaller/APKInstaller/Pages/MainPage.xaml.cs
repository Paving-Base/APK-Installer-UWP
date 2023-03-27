using AdvancedSharpAdbClient;
using APKInstaller.Helpers;
using APKInstaller.Pages.SettingsPages;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Core.Preview;
using Windows.UI.WindowManagement;
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
        private static bool IsWindowClosed=false;

        public readonly string GetAppTitleFromSystem = ResourceLoader.GetForViewIndependentUse()?.GetString("AppName") ?? Package.Current.DisplayName;

        internal bool IsExtendsTitleBar => this.IsAppWindow() ? this.GetWindowForElement().TitleBar.ExtendsContentIntoTitleBar : CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
        internal double TitleBarHeight => IsExtendsTitleBar ? 32 : 0;

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (name != null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }

        public MainPage()
        {
            InitializeComponent();
            UIHelper.DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            if (!this.IsAppWindow())
            {
                Window.Current.SetTitleBar(CustomTitleBar);
                SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequested;
            }
            else
            {
                this.GetWindowForElement().Closed += OnWindowClosed;
            }
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

        private void OnWindowClosed(AppWindow sender, AppWindowClosedEventArgs args)
        {
            if (IsWindowClosed && WindowHelper.ActiveWindows.Count <= 1) { CleanCaches(); }
        }

        private void OnCloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            IsWindowClosed = true;
            if (!(WindowHelper.IsSupportedAppWindow && WindowHelper.ActiveWindows.Any())) { CleanCaches(); }
        }

        private void CleanCaches()
        {
            if (ApiInformation.IsTypePresent("Windows.ApplicationModel.AppInstance"))
            {
                if (AppInstance.GetInstances().Count <= 1)
                {
                    CachesHelper.CleanAllCaches(true);
                    if (SettingsHelper.Get<bool>(SettingsHelper.IsCloseADB))
                    {
                        try { new AdbClient().KillAdb(); } catch { }
                    }
                }
                else
                {
                    CachesHelper.CleanAllCaches(false);
                }
            }
            else
            {
                CachesHelper.CleanAllCaches(true);
                if (SettingsHelper.Get<bool>(SettingsHelper.IsCloseADB))
                {
                    try { new AdbClient().KillAdb(); } catch { }
                }
            }
        }

        internal void UpdateTitleBarHeight() => RaisePropertyChangedEvent(nameof(TitleBarHeight));
    }
}
