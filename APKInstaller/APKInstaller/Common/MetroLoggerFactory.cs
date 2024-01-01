using AdvancedSharpAdbClient.Logs;
using APKInstaller.Helpers;
using System;
using System.ComponentModel;

namespace APKInstaller.Common
{
    public class MetroLoggerFactory : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName) => new MetroLogger(categoryName);

        public ILogger<T> CreateLogger<T>() => new MetroLogger<T>();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MetroLogger(string name) : ILogger
    {
        public virtual MetroLog.ILogger Logger { get; } = SettingsHelper.LogManager.GetLogger(name);

        public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    Logger.Trace(string.Format(message, args), exception);
                    break;
                case LogLevel.Debug:
                    Logger.Debug(string.Format(message, args), exception);
                    break;
                case LogLevel.Information:
                    Logger.Info(string.Format(message, args), exception);
                    break;
                case LogLevel.Warning:
                    Logger.Warn(string.Format(message, args), exception);
                    break;
                case LogLevel.Error:
                    Logger.Error(string.Format(message, args), exception);
                    break;
                case LogLevel.Critical:
                    Logger.Fatal(string.Format(message, args), exception);
                    break;
                case LogLevel.None:
                default:
                    break;
            }
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MetroLogger<T> : MetroLogger, ILogger<T>
    {
        public override MetroLog.ILogger Logger { get; } = SettingsHelper.LogManager.GetLogger<T>();

        public MetroLogger() : base(typeof(T).Name) { }
    }
}
