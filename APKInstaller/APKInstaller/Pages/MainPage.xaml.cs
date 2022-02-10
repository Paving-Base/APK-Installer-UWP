using AAPTForUWP;
using AAPTForUWP.Models;
using APKInstaller.Helpers;
using APKInstaller.Pages;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace ApkInstaller
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool HasBeenSmail;

        public MainPage()
        {
            InitializeComponent();
            //UIHelper.MainPage = this;
            UIHelper.DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            CoreApplicationViewTitleBar TitleBar = CoreApplication.GetCurrentView().TitleBar;
            TitleBar.ExtendViewIntoTitleBar = true;
            _ = CoreAppFrame.Navigate(typeof(InstallPage));
            UIHelper.CheckTheme();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Name)
            {
                case "AboutButton":
                    //_ = CoreAppFrame.Navigate(typeof(SettingsPage));
                    break;
                default:
                    break;
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (UIHelper.HasTitleBar)
                {
                    if (XamlRoot.Size.Width <= 240)
                    {
                        if (!HasBeenSmail)
                        {
                            HasBeenSmail = true;
                        }
                    }
                    else if (HasBeenSmail)
                    {
                        HasBeenSmail = false;
                    }
                    CustomTitleBar.Width = XamlRoot.Size.Width - 120;
                }
            }
            catch { }
        }
    }
}
