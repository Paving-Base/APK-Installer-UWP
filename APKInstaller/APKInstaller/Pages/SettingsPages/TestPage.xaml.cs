using APKInstaller.Controls;
using APKInstaller.Helpers;
using APKInstaller.Pages.ToolsPages;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Globalization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Globalization;
using Windows.System;
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
                    ThemeHelper.UpdateSystemCaptionButtonColors();
                    this.FindAscendant<MainPage>()?.UpdateTitleBarHeight();
                }
            }
        }

        private double progressValue = 0;
        internal double ProgressValue
        {
            get => progressValue;
            set
            {
                if (progressValue != value)
                {
                    TitleBar.SetProgressValue(value);
                    progressValue = value;
                }
            }
        }

        private bool isShowProgressRing = false;
        internal bool IsShowProgressRing
        {
            get => isShowProgressRing;
            set
            {
                if (isShowProgressRing != value)
                {
                    if (value)
                    {
                        TitleBar.ShowProgressRing();
                    }
                    else
                    {
                        TitleBar.HideProgressRing();
                    }
                    isShowProgressRing = value;
                }
            }
        }

        internal bool IsDevelopment { get; } = Package.Current.IsDevelopmentMode
            || Package.Current.SignatureKind != PackageSignatureKind.Store
            || Package.Current.Status.Modified;

        public TestPage() => InitializeComponent();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element) { return; }
            switch (element.Tag?.ToString())
            {
                case "Store":
                    _ = Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?ProductId=9NSHFKJ1D4BF&mode=mini"));
                    break;
                case "OutPIP" when ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.Default):
                    _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                    break;
                case "EnterPIP" when ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay):
                    _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
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

        private void TitleBar_BackRequested(TitleBar sender, object e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox ComboBox = sender as ComboBox;
            switch (ComboBox.Tag as string)
            {
                case "Language":
                    string lang = SettingsHelper.Get<string>(SettingsHelper.CurrentLanguage);
                    lang = lang == LanguageHelper.AutoLanguageCode ? LanguageHelper.GetCurrentLanguage() : lang;
                    CultureInfo culture = new(lang);
                    ComboBox.SelectedItem = culture;
                    break;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox ComboBox = sender as ComboBox;
            switch (ComboBox.Tag as string)
            {
                case "Language":
                    CultureInfo culture = ComboBox.SelectedItem as CultureInfo;
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
                    break;
            }
        }
    }
}
