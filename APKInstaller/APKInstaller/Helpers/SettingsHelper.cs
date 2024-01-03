using AdvancedSharpAdbClient.Models;
using MetroLog;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using IObjectSerializer = Microsoft.Toolkit.Helpers.IObjectSerializer;

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
                LocalObject.Save(IsOnlyWSA, OperatingSystemVersion.Build >= 22000);
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
        public static UISettings UISettings { get; } = new();
        public static ILogManager LogManager { get; } = LogManagerFactory.CreateLogManager();
        public static OSVersion OperatingSystemVersion => SystemInformation.Instance.OperatingSystemVersion;
        public static ApplicationDataStorageHelper LocalObject { get; } = ApplicationDataStorageHelper.GetCurrent(new NewtonsoftJsonObjectSerializer());

        static SettingsHelper() => SetDefaultSettings();

        public static void CheckAssembly()
        {
            LogManager.GetLogger("Hello World!").Info("\nThis is a hello from the author @wherewhere.\nIf you can't find this hello in your installed version, that means you have installed a piracy one.\nRemember, the author is @wherewhere. If not, you possible install a modified one too.");
            AssemblyName Info = Assembly.GetExecutingAssembly().GetName();
            if (Info.Name != $"{"APK"}{"Installer"}") { LogManager.GetLogger("Check Assembly").Error($"\nAssembly name is wrong.\nThe wrong name is {Info.Name}.\nIt should be {$"{"APK"}{"Installer"}"}."); };
            if (Info.Version.Major != Package.Current.Id.Version.Major || Info.Version.Minor != Package.Current.Id.Version.Minor || Info.Version.Build != Package.Current.Id.Version.Build) { LogManager.GetLogger("CheckAssembly").Error($"\nAssembly version is wrong.\nThe wrong version is {Info.Version}.\nIt should be {Package.Current.Id.Version.ToFormattedString()}."); };
        }
    }

    public class NewtonsoftJsonObjectSerializer : IObjectSerializer
    {
        // Specify your serialization settings
        private readonly JsonSerializerSettings settings = new() { DefaultValueHandling = DefaultValueHandling.Ignore };

        public string Serialize<T>(T value) => JsonConvert.SerializeObject(value, typeof(T), Formatting.Indented, settings);

        public T Deserialize<T>(string value) => JsonConvert.DeserializeObject<T>(value, settings);
    }
}
