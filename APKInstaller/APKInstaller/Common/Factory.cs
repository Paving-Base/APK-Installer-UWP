using APKInstaller.Metadata;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using WinRT;

namespace APKInstaller.Common
{
    [GeneratedComClass]
    public sealed partial class ServerManagerFactory : IClassFactory
    {
        private const int E_NOINTERFACE = unchecked((int)0x80004002);
        private const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);

        /// <summary>
        /// The EXE code that creates and manages objects of this class runs on same machine but is loaded in a separate process space.
        /// </summary>
        private const uint CLSCTX_LOCAL_SERVER = 0x4;

        /// <summary>
        /// Multiple applications can connect to the class object through calls to CoGetClassObject. If both the REGCLS_MULTIPLEUSE and CLSCTX_LOCAL_SERVER are set in a call to CoRegisterClassObject, the class object is also automatically registered as an in-process server, whether CLSCTX_INPROC_SERVER is explicitly set.
        /// </summary>
        private const int REGCLS_MULTIPLEUSE = 1;

        private static readonly Guid _iid = typeof(IServerManager).GUID;

        private uint co_cookie;

        public unsafe void CreateInstance(object pUnkOuter, Guid* riid, out nint ppvObject)
        {
            ppvObject = 0;

            if (pUnkOuter != null)
            {
                Marshal.ThrowExceptionForHR(CLASS_E_NOAGGREGATION);
            }

            if (*riid == _iid || *riid == Factory.CLSID_IUnknown)
            {
                // Create the instance of the .NET object
                ppvObject = MarshalInspectable<IServerManager>.FromManaged(new ServerManager());
            }
            else
            {
                // The object that ppvObject points to does not support the
                // interface identified by riid.
                Marshal.ThrowExceptionForHR(E_NOINTERFACE);
            }
        }

        void IClassFactory.LockServer(BOOL fLock) { }

        public void RevokeClassObject() => PInvoke.CoRevokeClassObject(co_cookie).ThrowOnFailure();

        public void RegisterClassObject()
        {
            int hresult = PInvoke.CoRegisterClassObject(
                Factory.CLSID_IServerManager,
                this,
                CLSCTX.CLSCTX_LOCAL_SERVER,
                REGCLS.REGCLS_MULTIPLEUSE,
                out co_cookie);
            if (hresult < 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }
        }

        private delegate nint DllGetActivationFactory([In] nint activatableClassId, [Out] out nint factory);
    }

    public static partial class Factory
    {
        public static readonly Guid CLSID_IServerManager = new("4036B695-CA92-45EA-8965-CE1947A6B269");
        public static readonly Guid CLSID_IUnknown = new("00000000-0000-0000-C000-000000000046");

        private static bool IsAlive() => true;

        public static IServerManager TryCreateServerManager() =>
            TryCreateInstance<IServerManager>(CLSID_IServerManager, CLSCTX.CLSCTX_ALL, TimeSpan.FromSeconds(30));

        internal static T TryCreateInstance<T>(in Guid rclsid, CLSCTX dwClsContext = CLSCTX.CLSCTX_INPROC_SERVER)
        {
            HRESULT hresult = PInvoke.CoCreateInstance(rclsid, null, dwClsContext, CLSID_IUnknown, out nint result);
            return hresult.Succeeded ? Marshaler<T>.FromAbi(result) : default;
        }

        internal static T TryCreateInstance<T>(in Guid rclsid, CLSCTX dwClsContext, in TimeSpan period) where T : ISetMonitor
        {
            T results = TryCreateInstance<T>(rclsid, dwClsContext);
            results?.SetMonitor(IsAlive, period);
            return results;
        }
    }

    /// <summary>
    /// Represents a monitor that checks if a remote object is alive.
    /// </summary>
    public sealed partial class RemoteMonitor : IDisposable
    {
        private bool disposed;
        private readonly Timer _timer;
        private readonly Action _dispose;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteMonitor"/> class.
        /// </summary>
        /// <param name="handler">The handler to check if the remote object is alive.</param>
        /// <param name="dispose">The action to dispose the remote object.</param>
        /// <param name="period">The period to check if the remote object is alive.</param>
        public RemoteMonitor(IsAliveHandler handler, Action dispose, in TimeSpan period)
        {
            ArgumentNullException.ThrowIfNull(handler);
            ArgumentNullException.ThrowIfNull(dispose);
            _dispose = dispose;
            _timer = new(_ =>
            {
                bool isAlive = false;
                try
                {
                    isAlive = handler();
                }
                catch
                {
                    isAlive = false;
                }
                finally
                {
                    if (!isAlive)
                    {
                        Dispose();
                    }
                }
            }, null, TimeSpan.Zero, period);
        }

        /// <summary>
        /// Finalizes the instance of the <see cref="RemoteMonitor"/> class.
        /// </summary>
        ~RemoteMonitor() => Dispose();

        /// <summary>
        /// Stops the monitor.
        /// </summary>
        public void Stop()
        {
            if (!disposed)
            {
                disposed = true;
                _timer.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                _timer.Dispose();
                _dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
