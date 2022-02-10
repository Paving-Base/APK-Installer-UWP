using APKInstaller.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace APKInstaller.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class InstallPage : Page
    {
        internal InstallViewModel Provider;

        public InstallPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string _path = string.Empty;
            //AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            //switch (args.Kind)
            //{
            //    case ExtendedActivationKind.File:
            //        _path = (args.Data as IFileActivatedEventArgs).Files.First().Path;
            //        Provider = new InstallViewModel(_path, this);
            //        break;
            //    case ExtendedActivationKind.Protocol:
            //        Provider = new InstallViewModel((args.Data as IProtocolActivatedEventArgs).Uri, this);
            //        break;
            //    default:
            //        Provider = new InstallViewModel(_path, this);
            //        break;
            //}
            Provider = new InstallViewModel(_path, this);
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
                    //Frame.Navigate(typeof(SettingsPage));
                    break;
                case "SecondaryActionButton":
                    //Provider.OpenAPP();
                    break;
                case "CancelOperationButton":
                    Application.Current.Exit();
                    break;
            }
        }

        private async void InitialLoadingUI_Loaded(object sender, RoutedEventArgs e)
        {
            await Provider.Refresh();
        }
    }
}
