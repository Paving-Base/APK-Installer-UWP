using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace APKInstaller.Helpers
{
    /// <summary>
    /// Helpers class to allow the app to find the Window that contains an
    /// arbitrary <see cref="UIElement"/> (GetWindowForElement(UIElement)).
    /// To do this, we keep track of all active Windows. The app code must call
    /// <see cref="CreateWindowAsync(Action{Window})"/> rather than "new <see cref="Window"/>()"
    /// so we can keep track of all the relevant windows.
    /// </summary>
    public static class WindowHelper
    {
        public static bool IsXamlRootSupported { get; } = ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "XamlRoot");

        public static async Task<bool> CreateWindowAsync(Action<Window> launched)
        {
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = await newView.Dispatcher.AwaitableRunAsync(() =>
            {
                Window window = Window.Current;
                TrackWindow(window);
                launched(window);
                window.Activate();
                return ApplicationView.GetForCurrentView().Id;
            });
            return await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }

        public static void TrackWindow(this Window window)
        {
            if (!ActiveWindows.ContainsKey(window.Dispatcher))
            {
                window.Closed += (sender, args) =>
                {
                    ActiveWindows.Remove(window.Dispatcher);
                    window = null;
                };
                ActiveWindows[window.Dispatcher] = window;
            }
        }

        public static T SetXAMLRoot<T>(this T element, UIElement target) where T : UIElement
        {
            if (IsXamlRootSupported)
            {
                element.XamlRoot = target?.XamlRoot;
            }
            return element;
        }

        public static Dictionary<CoreDispatcher, Window> ActiveWindows { get; } = [];
    }
}
