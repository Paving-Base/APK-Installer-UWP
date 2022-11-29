﻿using AdvancedSharpAdbClient;
using MetroLog;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.UI.Xaml;
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
        public const string ADBPath = "ADBPath";
        public const string IsOpenApp = "IsOpenApp";
        public const string IsOnlyWSA = "IsOnlyWSA";
        public const string UpdateDate = "UpdateDate";
        public const string IsFirstRun = "IsFirstRun";
        public const string IsCloseADB = "IsCloseADB";
        public const string IsCloseAPP = "IsCloseAPP";
        public const string ShowDialogs = "ShowDialogs";
        public const string ShowMessages = "ShowMessages";
        public const string AutoGetNetAPK = "AutoGetNetAPK";
        public const string DefaultDevice = "DefaultDevice";
        public const string CurrentLanguage = "CurrentLanguage";
        public const string SelectedAppTheme = "SelectedAppTheme";
        public const string SelectedBackdrop = "SelectedBackdrop";

        public static Type Get<Type>(string key) => LocalObject.Read<Type>(key);
        public static void Set(string key, object value) => LocalObject.Save(key, value);
        public static void SetFile(string key, object value) => LocalObject.CreateFileAsync(key, value);
        public static async Task<Type> GetFile<Type>(string key) => await LocalObject.ReadFileAsync<Type>(key);

        public static void SetDefaultSettings()
        {
            if (!LocalObject.KeyExists(ADBPath))
            {
                LocalObject.Save(ADBPath, Path.Combine(ApplicationData.Current.LocalFolder.Path, @"platform-tools\adb.exe"));
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
                LocalObject.Save(UpdateDate, new DateTime());
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
            if (!LocalObject.KeyExists(SelectedAppTheme))
            {
                LocalObject.Save(SelectedAppTheme, ElementTheme.Default);
            }
        }
    }

    public enum UISettingChangedType
    {
        LightMode,
        DarkMode,
        NoPicChanged,
    }

    public static partial class SettingsHelper
    {
        public static readonly UISettings UISettings = new();
        public static readonly ILogManager LogManager = LogManagerFactory.CreateLogManager();
        public static OSVersion OperatingSystemVersion => SystemInformation.Instance.OperatingSystemVersion;
        private static readonly ApplicationDataStorageHelper LocalObject = ApplicationDataStorageHelper.GetCurrent(new SystemTextJsonObjectSerializer());

        static SettingsHelper()
        {
            SetDefaultSettings();
        }

        public static void CheckAssembly()
        {
            LogManager.GetLogger("Hello World!").Info("\nThis is a hello from the author @wherewhere.\nIf you can't find this hello in your installed version, that means you have installed a piracy one.\nRemember, the author is @wherewhere. If not, you possible install a modified one too.");
            AssemblyName Info = Assembly.GetExecutingAssembly().GetName();
            if (Info.Name != $"{"APK"}{"Installer"}") { LogManager.GetLogger("Check Assembly").Error($"\nAssembly name is wrong.\nThe wrong name is {Info.Name}.\nIt should be {$"{"APK"}{"Installer"}"}."); };
            if (Info.Version.Major != Package.Current.Id.Version.Major || Info.Version.Minor != Package.Current.Id.Version.Minor || Info.Version.Build != Package.Current.Id.Version.Build) { LogManager.GetLogger("CheckAssembly").Error($"\nAssembly version is wrong.\nThe wrong version is {Info.Version}.\nIt should be {Package.Current.Id.Version.ToFormattedString()}."); };
        }
    }

    public class SystemTextJsonObjectSerializer : IObjectSerializer
    {
        // Specify your serialization settings
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore };

        string IObjectSerializer.Serialize<T>(T value) => JsonConvert.SerializeObject(value, typeof(T), Formatting.Indented, settings);

        public T Deserialize<T>(string value) => JsonConvert.DeserializeObject<T>(value, settings);
    }
}
