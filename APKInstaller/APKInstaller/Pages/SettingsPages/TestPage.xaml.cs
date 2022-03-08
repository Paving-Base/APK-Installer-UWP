using APKInstaller.Helpers;
using APKInstaller.Pages.ToolsPages;
using System.Globalization;
using Windows.ApplicationModel.Core;
using Windows.Globalization;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace APKInstaller.Pages.SettingsPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class TestPage : Page
    {
        internal bool IsExtendsTitleBar
        {
            get => CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
            set
            {
                if (IsExtendsTitleBar != value)
                {
                    CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = value;
                }
            }
        }

        public TestPage() => InitializeComponent();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag as string)
            {
                case "OutPIP":
                    _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                    break;
                case "EnterPIP":
                    if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay)) { _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay); }
                    break;
                case "Processes":
                    _ = Frame.Navigate(typeof(ProcessesPage));
                    break;
                case "Applications":
                    _ = Frame.Navigate(typeof(ApplicationsPage));
                    break;
                default:
                    break;
            }
        }

        private void TitleBar_BackRequested(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            string lang = SettingsHelper.Get<string>(SettingsHelper.CurrentLanguage);
            lang = lang == LanguageHelper.AutoLanguageCode ? LanguageHelper.GetCurrentLanguage() : lang;
            CultureInfo culture = new CultureInfo(lang);
            (sender as ComboBox).SelectedItem = culture;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CultureInfo culture = (sender as ComboBox).SelectedItem as CultureInfo;
            if (culture.Name != LanguageHelper.GetCurrentLanguage())
            {
                ApplicationLanguages.PrimaryLanguageOverride = culture.Name;
                SettingsHelper.Set(SettingsHelper.CurrentLanguage, culture.Name);
            }
            else
            {
                ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
                SettingsHelper.Set(SettingsHelper.CurrentLanguage, LanguageHelper.AutoLanguageCode);
            }
        }
    }
}
