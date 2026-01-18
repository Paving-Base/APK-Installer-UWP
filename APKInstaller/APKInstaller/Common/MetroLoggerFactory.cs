using AdvancedSharpAdbClient.Logs;
using APKInstaller.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;

namespace APKInstaller.Common
{
    public class MetroLoggerFactory : AdvancedSharpAdbClient.Logs.ILoggerFactory
    {
        public AdvancedSharpAdbClient.Logs.ILogger CreateLogger(string categoryName) => new MetroLogger(categoryName);

        public AdvancedSharpAdbClient.Logs.ILogger<T> CreateLogger<T>() => new MetroLogger<T>();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMetroLogger : AdvancedSharpAdbClient.Logs.ILogger
    {
        public Microsoft.Extensions.Logging.ILogger Logger { get; }

        void AdvancedSharpAdbClient.Logs.ILogger.Log(AdvancedSharpAdbClient.Logs.LogLevel logLevel, Exception exception, string message, params object[] args) =>
            Logger.Log((Microsoft.Extensions.Logging.LogLevel)logLevel, exception, message, args);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MetroLogger(string name) : IMetroLogger
    {
        public Microsoft.Extensions.Logging.ILogger Logger { get; } = SettingsHelper.LoggerFactory.CreateLogger(name);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MetroLogger<T> : IMetroLogger, AdvancedSharpAdbClient.Logs.ILogger<T>
    {
        public Microsoft.Extensions.Logging.ILogger Logger { get; } = SettingsHelper.LoggerFactory.CreateLogger<T>();
    }
}
