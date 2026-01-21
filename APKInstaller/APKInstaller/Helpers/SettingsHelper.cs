using AdvancedSharpAdbClient.Models;
using APKInstaller.Models;
using CommunityToolkit.Common.Helpers;
using CommunityToolkit.WinUI.Helpers;
using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml;

namespace APKInstaller.Helpers
{
    public static partial class SettingsHelper
    {
        public const string ADBPath = nameof(ADBPath);
        public const string IsOpenApp = nameof(IsOpenApp);
        public const string IsOnlyWSA = nameof(IsOnlyWSA);
        public const string UpdateDate = nameof(UpdateDate);
        public const string IsFirstRun = nameof(IsFirstRun);
        public const string IsCloseADB = nameof(IsCloseADB);
        public const string IsCloseAPP = nameof(IsCloseAPP);
        public const string IsUploadAPK = nameof(IsUploadAPK);
        public const string ShowDialogs = nameof(ShowDialogs);
        public const string ShowMessages = nameof(ShowMessages);
        public const string AutoGetNetAPK = nameof(AutoGetNetAPK);
        public const string DefaultDevice = nameof(DefaultDevice);
        public const string CurrentLanguage = nameof(CurrentLanguage);
        public const string ScanPairedDevice = nameof(ScanPairedDevice);
        public const string SelectedAppTheme = nameof(SelectedAppTheme);

        public static Type Get<Type>(string key) => LocalObject.Read<Type>(key);
        public static void Set(string key, object value) => LocalObject.Save(key, value);
        public static Task<Type> GetFile<Type>(string key) => LocalObject.ReadFileAsync<Type>($"Settings/{key}");
        public static Task SetFile<Type>(string key, Type value) => LocalObject.CreateFileAsync($"Settings/{key}", value);

        public static void SetDefaultSettings()
        {
            if (!LocalObject.KeyExists(ADBPath))
            {
                LocalObject.Save(ADBPath, @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe");
            }
            if (!LocalObject.KeyExists(IsOpenApp))
            {
                LocalObject.Save(IsOpenApp, true);
            }
            if (!LocalObject.KeyExists(IsOnlyWSA))
            {
                LocalObject.Save(IsOnlyWSA, OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000));
            }
            if (!LocalObject.KeyExists(UpdateDate))
            {
                LocalObject.Save(UpdateDate, new DateTimeOffset());
            }
            if (!LocalObject.KeyExists(IsFirstRun))
            {
                LocalObject.Save(IsFirstRun, true);
            }
            if (!LocalObject.KeyExists(IsCloseADB))
            {
                LocalObject.Save(IsCloseADB, true);
            }
            if (!LocalObject.KeyExists(IsCloseAPP))
            {
                LocalObject.Save(IsCloseAPP, true);
            }
            if (!LocalObject.KeyExists(IsUploadAPK))
            {
                LocalObject.Save(IsUploadAPK, false);
            }
            if (!LocalObject.KeyExists(ShowDialogs))
            {
                LocalObject.Save(ShowDialogs, true);
            }
            if (!LocalObject.KeyExists(ShowMessages))
            {
                LocalObject.Save(ShowMessages, true);
            }
            if (!LocalObject.KeyExists(AutoGetNetAPK))
            {
                LocalObject.Save(AutoGetNetAPK, false);
            }
            if (!LocalObject.KeyExists(DefaultDevice))
            {
                LocalObject.Save(DefaultDevice, new DeviceData());
            }
            if (!LocalObject.KeyExists(CurrentLanguage))
            {
                LocalObject.Save(CurrentLanguage, LanguageHelper.AutoLanguageCode);
            }
            if (!LocalObject.KeyExists(ScanPairedDevice))
            {
                LocalObject.Save(ScanPairedDevice, false);
            }
            if (!LocalObject.KeyExists(SelectedAppTheme))
            {
                LocalObject.Save(SelectedAppTheme, ElementTheme.Default);
            }
        }
    }

    public static partial class SettingsHelper
    {
        public static ILoggerFactory LoggerFactory { get; } = CreateLoggerFactory();
        public static ApplicationDataStorageHelper LocalObject { get; } = ApplicationDataStorageHelper.GetCurrent(new SystemTextJsonObjectSerializer());

        static SettingsHelper() => SetDefaultSettings();

        public static ILoggerFactory CreateLoggerFactory() =>
            Microsoft.Extensions.Logging.LoggerFactory.Create(x => _ = x.AddFile(x =>
            {
                x.RootPath = ApplicationData.Current.LocalFolder.Path;
                x.IncludeScopes = true;
                x.BasePath = "Logs";
                x.Files = [
                    new LogFileOptions()
                    {
                        Path = "Log - <date>.log"
                    }
                ];
            }).AddDebug());

        public static void CheckAssembly()
        {
            LoggerFactory.CreateLogger("Hello World!").LogInformation("\nThis is a hello from the author @wherewhere.\nIf you can't find this hello in your installed version, that means you have installed a piracy one.\nRemember, the author is @wherewhere. If not, you possible install a modified one too.");
            AssemblyName info = Assembly.GetExecutingAssembly().GetName();
            if (info.Name != $"{"APK"}{"Installer"}") { LoggerFactory.CreateLogger("Check Assembly").LogError("\nAssembly name is wrong.\nThe wrong name is {name}.\nIt should be {APK}{Installer}.", info.Name, "APK", "Installer"); };
            if (info.Version.Major != Package.Current.Id.Version.Major || info.Version.Minor != Package.Current.Id.Version.Minor || info.Version.Build != Package.Current.Id.Version.Build) { LoggerFactory.CreateLogger("CheckAssembly").LogError("\nAssembly version is wrong.\nThe wrong version is {assembly}.\nIt should be {package}.", info.Version, Package.Current.Id.Version.ToFormattedString()); };
        }
    }

    public class SystemTextJsonObjectSerializer : IObjectSerializer
    {
        public string Serialize<T>(T value) => value switch
        {
            bool => JsonSerializer.Serialize(value, SourceGenerationContext.Default.Boolean),
            string => JsonSerializer.Serialize(value, SourceGenerationContext.Default.String),
            DeviceData => JsonSerializer.Serialize(value, SourceGenerationContext.Default.DeviceData),
            ElementTheme => JsonSerializer.Serialize(value, SourceGenerationContext.Default.ElementTheme),
            DateTimeOffset => JsonSerializer.Serialize(value, SourceGenerationContext.Default.DateTimeOffset),
            _ => JsonSerializer.Serialize(value, typeof(T), SourceGenerationContext.Default)
        };

        public T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] string value)
        {
            if (string.IsNullOrEmpty(value)) { return default; }
            Type type = typeof(T);
            return type == typeof(bool) ? Deserialize(value, SourceGenerationContext.Default.Boolean)
                : type == typeof(string) ? Deserialize(value, SourceGenerationContext.Default.String)
                : type == typeof(DeviceData) ? Deserialize(value, SourceGenerationContext.Default.DeviceData)
                : type == typeof(ElementTheme) ? Deserialize(value, SourceGenerationContext.Default.ElementTheme)
                : type == typeof(DateTimeOffset) ? Deserialize(value, SourceGenerationContext.Default.DateTimeOffset)
                : JsonSerializer.Deserialize(value, type, SourceGenerationContext.Default) is T result ? result : default;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T Deserialize<TValue>([StringSyntax(StringSyntaxAttribute.Json)] string json, JsonTypeInfo<TValue> jsonTypeInfo)
            {
                TValue value = JsonSerializer.Deserialize(json, jsonTypeInfo);
                return Unsafe.As<TValue, T>(ref value);
            }
        }
    }

    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(DeviceData))]
    [JsonSerializable(typeof(UpdateInfo))]
    [JsonSerializable(typeof(ElementTheme))]
    [JsonSerializable(typeof(DateTimeOffset))]
    public partial class SourceGenerationContext : JsonSerializerContext;
}
