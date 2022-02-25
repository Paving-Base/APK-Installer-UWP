using ApkInstaller;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace APKInstaller.Helpers
{
    internal static partial class UIHelper
    {
        public static bool HasTitleBar => !CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
        public static bool HasStatusBar => ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar");
        public static double TitleBarHeight => 32;
        public static double PageTitleHeight => HasStatusBar || HasTitleBar ? 48 : 48 + TitleBarHeight;
        public static Thickness StackPanelMargin => new Thickness(0, PageTitleHeight, 0, 0);
        public static Thickness ScrollViewerMargin => new Thickness(0, PageTitleHeight, 0, 0);
        public static Thickness ScrollViewerPadding => new Thickness(0, -PageTitleHeight, 0, 0);

        private static DispatcherQueue _dispatcherQueue;
        public static DispatcherQueue DispatcherQueue
        {
            get => _dispatcherQueue;
            set
            {
                if (_dispatcherQueue == null)
                {
                    _dispatcherQueue = value;
                }
            }
        }

        public static bool IsDarkTheme() => IsDarkTheme(SettingsHelper.Theme);

        public static bool IsDarkTheme(ElementTheme theme) => theme == ElementTheme.Default ? Application.Current.RequestedTheme == ApplicationTheme.Dark : theme == ElementTheme.Dark;

        public static void CheckTheme()
        {
            bool IsDark = IsDarkTheme(SettingsHelper.Theme);
            CheckTheme(IsDark, false);
        }

        public static void CheckTheme(bool IsDark, bool IsInvoke = false)
        {
            Color ForegroundColor = IsDark ? Colors.White : Colors.Black;
            Color BackgroundColor = new AccessibilitySettings().HighContrast ? Color.FromArgb(255, 0, 0, 0) : IsDark ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);

            if (HasStatusBar)
            {
                StatusBar StatusBar = StatusBar.GetForCurrentView();
                StatusBar.ForegroundColor = ForegroundColor;
                StatusBar.BackgroundColor = BackgroundColor;
                StatusBar.BackgroundOpacity = 0; // 透明度
            }
            else
            {
                ApplicationViewTitleBar TitleBar = ApplicationView.GetForCurrentView().TitleBar;
                TitleBar.ForegroundColor = TitleBar.ButtonForegroundColor = ForegroundColor;
                TitleBar.BackgroundColor = TitleBar.InactiveBackgroundColor = BackgroundColor;
                TitleBar.ButtonBackgroundColor = TitleBar.ButtonInactiveBackgroundColor = HasTitleBar ? BackgroundColor : Colors.Transparent;
            }
        }
    }

    internal static partial class UIHelper
    {
        public static MainPage MainPage;

        public static void Navigate(Type pageType, NavigationTransitionInfo TransitionInfo, object e = null)
        {
            DispatcherQueue?.EnqueueAsync(() =>
            {
                _ = (MainPage?.CoreAppFrame.Navigate(pageType, e, TransitionInfo));
            });
        }
    }

    internal static partial class UIHelper
    {
        public static string GetSizeString(this double size)
        {
            int index = 0;
            while (true)
            {
                index++;
                size /= 1024;
                if (size is > 0.7 and < 716.8) { break; }
                else if (size >= 716.8) { continue; }
                else if (size <= 0.7)
                {
                    size *= 1024;
                    index--;
                    break;
                }
            }
            string str = string.Empty;
            switch (index)
            {
                case 0: str = "B"; break;
                case 1: str = "KB"; break;
                case 2: str = "MB"; break;
                case 3: str = "GB"; break;
                case 4: str = "TB"; break;
                default:
                    break;
            }
            return $"{size:N2}{str}";
        }

        public static string GetPermissionName(this string permission)
        {
            ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse("Permissions");
            try
            {
                string name = _loader.GetString(permission) ?? string.Empty;
                return string.IsNullOrEmpty(name) ? permission : name;
            }
            catch
            {
                return permission;
            }
        }

        public static double GetProgressValue<T>(this List<T> lists, T list)
        {
            return (double)(lists.IndexOf(list) + 1) * 100 / lists.Count;
        }

        public static double GetProgressValue<T>(this IEnumerable<T> lists, T list)
        {
            return (double)(lists.ToList().IndexOf(list) + 1) * 100 / lists.Count();
        }

        public static Uri ValidateAndGetUri(this string uriString)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(uriString.Contains("://") ? uriString : uriString.Contains("//") ? uriString.Replace("//", "://") : $"http://{uriString}");
            }
            catch (FormatException e)
            {
#if DEBUG
                Debug.WriteLine(e.Message);
#endif
            }
            return uri;
        }
    }
}
