﻿using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Logs;
using APKInstaller.Common;
using APKInstaller.Helpers;
using APKInstaller.Pages;
using APKInstaller.Projection;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Security.Authorization.AppCapabilityAccess;
using Windows.System.Profile;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace APKInstaller
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    public sealed partial class App : Application
    {
        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
            UnhandledException += Application_UnhandledException;
            LoggerProvider.SetLogProvider(new MetroLoggerFactory());
            Factories.AdbCommandLineClientFactory = path => new AdbCommandClient(path);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox") { FocusVisualKind = FocusVisualKind.Reveal; }
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            EnsureWindow(e);
        }

        #region OnActivated

        protected override void OnActivated(IActivatedEventArgs e)
        {
            EnsureWindow(e);
            base.OnActivated(e);
        }

        protected override void OnCachedFileUpdaterActivated(CachedFileUpdaterActivatedEventArgs e)
        {
            EnsureWindow(e);
            base.OnCachedFileUpdaterActivated(e);
        }

        protected override void OnFileActivated(FileActivatedEventArgs e)
        {
            EnsureWindow(e);
            base.OnFileActivated(e);
        }

        protected override void OnFileOpenPickerActivated(FileOpenPickerActivatedEventArgs e)
        {
            EnsureWindow(e);
            base.OnFileOpenPickerActivated(e);
        }

        protected override void OnFileSavePickerActivated(FileSavePickerActivatedEventArgs e)
        {
            EnsureWindow(e);
            base.OnFileSavePickerActivated(e);
        }

        protected override void OnSearchActivated(SearchActivatedEventArgs e)
        {
            EnsureWindow(e);
            base.OnSearchActivated(e);
        }

        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs e)
        {
            EnsureWindow(e);
            base.OnShareTargetActivated(e);
        }

        #endregion

        private async void EnsureWindow(IActivatedEventArgs e)
        {
            if (!isLoaded)
            {
                _ = RequestWifiAccessAsync();
                RegisterExceptionHandlingSynchronizationContext();
                isLoaded = true;
            }

            Window window = Window.Current;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (window.Content is not Frame rootFrame)
            {
                WindowHelper.TrackWindow(window);

                ApplicationView.PreferredLaunchViewSize = new Size(652, 414);
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                window.Content = rootFrame;

                ThemeHelper.Initialize();
            }
            else
            {
                _ = await WindowHelper.CreateWindowAsync(x =>
                {
                    window = x;

                    ApplicationView.PreferredLaunchViewSize = new Size(652, 414);
                    ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

                    CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

                    // 创建要充当导航上下文的框架，并导航到第一页
                    rootFrame = new Frame();

                    rootFrame.NavigationFailed += OnNavigationFailed;

                    // 将框架放在当前窗口中
                    window.Content = rootFrame;

                    ThemeHelper.Initialize(window);
                }).ConfigureAwait(false);
                await window.Dispatcher.ResumeForegroundAsync();
            }

            if (e is LaunchActivatedEventArgs args)
            {
                if (!args.PrelaunchActivated)
                {
                    CoreApplication.EnablePrelaunch(true);
                }
                else { return; }
            }

            if (rootFrame.Content == null)
            {
                // 当导航堆栈尚未还原时，导航到第一页，
                // 并通过将所需信息作为导航参数传入来配置
                // 参数
                rootFrame.Navigate(typeof(MainPage), e);
            }

            // 确保当前窗口处于活动状态
            window.Activate();
        }

        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private static void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            deferral.Complete();
        }

        private static async Task RequestWifiAccessAsync()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            _ = APKInstallerProjectionFactory.ServerManager.EnableLoopback();
            if (ApiInformation.IsMethodPresent("Windows.Security.Authorization.AppCapabilityAccess.AppCapability", "Create"))
            {
                AppCapability wifiData = AppCapability.Create("wifiData");
                switch (wifiData.CheckAccess())
                {
                    case AppCapabilityAccessStatus.DeniedByUser:
                    case AppCapabilityAccessStatus.DeniedBySystem:
                        // Do something
                        await AppCapability.Create("wifiData").RequestAccessAsync();
                        break;
                }
            }
        }

        private static void Application_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            SettingsHelper.LogManager.GetLogger("Unhandled Exception - Application").Error(e.Exception.ExceptionToMessage(), e.Exception);
            e.Handled = true;
        }

        private static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception Exception)
            {
                SettingsHelper.LogManager.GetLogger("Unhandled Exception - CurrentDomain").Error(Exception.ExceptionToMessage(), Exception);
            }
        }

        /// <summary>
        /// Should be called from OnActivated and OnLaunched
        /// </summary>
        private static void RegisterExceptionHandlingSynchronizationContext()
        {
            ExceptionHandlingSynchronizationContext
                .Register()
                .UnhandledException += SynchronizationContext_UnhandledException;
        }

        private static void SynchronizationContext_UnhandledException(object sender, Common.UnhandledExceptionEventArgs e)
        {
            SettingsHelper.LogManager.GetLogger("Unhandled Exception - SynchronizationContext").Error(e.Exception.ExceptionToMessage(), e.Exception);
            e.Handled = true;
        }

        private bool isLoaded;
    }
}
