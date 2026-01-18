using APKInstaller.Metadata;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;

namespace APKInstaller.Common
{
    /// <summary>
    /// Represents a server manager that manages the lifetime of COM objects.
    /// </summary>
    [ComVisible(true)]
    public sealed partial class ServerManager : IServerManager
    {
        private bool disposed;
        private RemoteMonitor _monitor;

        /// <summary>
        /// Gets the reference count of the <see cref="ServerManager"/> class.
        /// </summary>
        public static int RefCount { get; private set; }

        /// <summary>
        /// Gets or sets the timeout in milliseconds for checking the COM reference count.
        /// </summary>
        public static int Timeout { get; set; } = 100;

        /// <summary>
        /// Occurs when all the <see cref="ServerManager"/> is destructed.
        /// </summary>
        public static event Action ServerManagerDestructed;

        /// <summary>
        /// Gets a value indicating whether the server is running.
        /// </summary>
        public bool IsServerRunning => !disposed;

        /// <inheritdoc cref="RunProcess.Instance"/>
        public IRunProcess RunProcess => Common.RunProcess.Instance;

        /// <inheritdoc cref="Loopback.Instance"/>
        public ILoopback Loopback => Common.Loopback.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerManager"/> class.
        /// </summary>
        public ServerManager() => RefCount++;

        /// <summary>
        /// Finalizes the instance of the <see cref="ServerManager"/> class.
        /// </summary>
        ~ServerManager() => Dispose();

        /// <summary>
        /// Sets the monitor to check if the remote object is alive.
        /// </summary>
        /// <param name="handler">The handler to check if the remote object is alive.</param>
        /// <param name="period">The period to check if the remote object is alive.</param>
        public void SetMonitor(IsAliveHandler handler, TimeSpan period)
        {
            _monitor?.Stop();
            if (period.TotalMilliseconds > 0)
            {
                _monitor = new RemoteMonitor(handler, Dispose, period);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                _monitor?.Dispose();
                GC.SuppressFinalize(this);
                if (--RefCount == 0)
                {
                    _ = CheckReferenceAsync();
                }
            }
        }

        /// <summary>
        /// Checks if the reference count is zero and invokes the server manager destructed event.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task CheckReferenceAsync()
        {
            if (Timeout > 0) { await Task.Delay(Timeout); }
            if (RefCount == 0) { ServerManagerDestructed?.Invoke(); }
        }

        /// <inheritdoc/>
        public override string ToString() =>
            new StringBuilder()
                .Append(base.ToString())
                .Append(" running on ")
                .Append(RuntimeInformation.FrameworkDescription)
                .ToString();
    }
}
