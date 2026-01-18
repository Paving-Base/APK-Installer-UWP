using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;

namespace APKInstaller.Helpers
{
    public static partial class UIHelper
    {
        [SupportedOSPlatformGuard("windows10.0.10240.0")]
        public static bool IsWindows10OrGreater { get; } = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240);
        public static bool HasTitleBar => !CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
    }

    public static partial class UIHelper
    {
        public static string GetSizeString(this double size)
        {
            int index = 0;
            while (index <= 11)
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
            string str = index switch
            {
                0 => "B",
                1 => "KB",
                2 => "MB",
                3 => "GB",
                4 => "TB",
                5 => "PB",
                6 => "EB",
                7 => "ZB",
                8 => "YB",
                9 => "BB",
                10 => "NB",
                11 => "DB",
                _ => string.Empty,
            };
            return $"{size:0.##}{str}";
        }

        public static string GetPermissionName(this string permission)
        {
            ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse("Permissions");
            try
            {
                string name = _loader.GetString(permission) ?? string.Empty;
                return string.IsNullOrEmpty(name) ? permission : name;
            }
            catch (Exception e)
            {
                SettingsHelper.LoggerFactory.CreateLogger(typeof(UIHelper)).LogWarning(e, "\"{permission}\" is not found. {message} (0x{hResult:X})", permission, e.GetMessage(), e.HResult);
                return permission;
            }
        }

        public static double GetProgressValue<T>(this IList<T> lists, T list)
        {
            return (double)(lists.IndexOf(list) + 1) * 100 / lists.Count;
        }

        public static Uri TryGetUri(this string url)
        {
            url.TryGetUri(out Uri uri);
            return uri;
        }

        public static bool TryGetUri(this string url, out Uri uri)
        {
            uri = default;
            if (string.IsNullOrWhiteSpace(url)) { return false; }
            try
            {
                return Uri.TryCreate(
                    url.Contains("://") ? url
                    : url.Contains("//") ? url.Replace("//", "://")
                    : $"http://{url}", UriKind.RelativeOrAbsolute, out uri);
            }
            catch (FormatException ex)
            {
                SettingsHelper.LoggerFactory.CreateLogger(typeof(UIHelper)).LogWarning(ex, "\"{url}\" is not a URL. {message} (0x{hResult:X})", url, ex.GetMessage(), ex.HResult);
            }
            return false;
        }

        public static object GetMessage(this Exception ex) => ex.Message is { Length: > 0 } message ? message : ex.GetType();

        public static TResult AwaitByTaskCompleteSource<TResult>(this Task<TResult> function, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<TResult> taskCompletionSource = new();
            Task<TResult> task = taskCompletionSource.Task;
            _ = Task.Run(async () =>
            {
                try
                {
                    TResult result = await function.ConfigureAwait(false);
                    _ = taskCompletionSource.TrySetResult(result);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            }, cancellationToken);
            TResult taskResult = task.Result;
            return taskResult;
        }

        public static TResult AwaitByTaskCompleteSource<TResult>(this IAsyncOperation<TResult> function, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<TResult> taskCompletionSource = new();
            Task<TResult> task = taskCompletionSource.Task;
            _ = Task.Run(async () =>
            {
                try
                {
                    TResult result = await function.AsTask(cancellationToken);
                    _ = taskCompletionSource.TrySetResult(result);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            }, cancellationToken);
            TResult taskResult = task.Result;
            return taskResult;
        }
    }
}
