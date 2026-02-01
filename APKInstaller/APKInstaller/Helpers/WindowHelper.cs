using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
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
        [SupportedOSPlatformGuard("Windows10.0.18362.0")]
        public static bool IsXamlRootSupported { get; } = UIHelper.IsWindows10OrGreater && ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "XamlRoot");

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

        public static void SetDeferral(this IActivatedEventArgs args)
        {
            switch (args.Kind)
            {
                case ActivationKind.CommandLineLaunch when args is CommandLineActivatedEventArgs commandLineActivatedEventArgs:
                    Deferrals.AddOrUpdate(args, commandLineActivatedEventArgs.Operation.GetDeferral());
                    break;
            }
        }

        public static void CompleteDeferral(this IActivatedEventArgs args)
        {
            if (Deferrals.TryGetValue(args, out Deferral deferral))
            {
                deferral?.Complete();
            }
        }

        public static Dictionary<CoreDispatcher, Window> ActiveWindows { get; } = [];

        public static ConditionalWeakTable<IActivatedEventArgs, Deferral> Deferrals { get; } = [];
    }
}
