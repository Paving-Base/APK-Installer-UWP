using APKInstaller.Metadata;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
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

        public void CreateInstance(nint pUnkOuter, in Guid riid, out nint ppvObject)
        {
            ppvObject = 0;

            if (pUnkOuter != 0)
            {
                Marshal.ThrowExceptionForHR(CLASS_E_NOAGGREGATION);
            }

            if (riid == _iid || riid == Factory.CLSID_IUnknown)
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

        public void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock)
        {
        }

        public void RevokeClassObject()
        {
            int hresult = Factory.CoRevokeClassObject(co_cookie);
            if (hresult < 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }
        }

        public void RegisterClassObject()
        {
            int hresult = CoRegisterClassObject(
                Factory.CLSID_IServerManager,
                this,
                CLSCTX_LOCAL_SERVER,
                REGCLS_MULTIPLEUSE,
                out co_cookie);
            if (hresult < 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }
        }

        [LibraryImport("api-ms-win-core-com-l1-1-0.dll")]
        private static partial int CoRegisterClassObject(in Guid rclsid, IClassFactory pUnk, uint dwClsContext, int flags, out uint lpdwRegister);

        private delegate nint DllGetActivationFactory([In] nint activatableClassId, [Out] out nint factory);
    }

    public static partial class Factory
    {
        /// <summary>
        /// The CLSCTX enumeration specifies the context in which the code that manages a class object will run.
        /// </summary>
        private const uint CLSCTX_ALL = 1 | 2 | 4 | 16;

        public static readonly Guid CLSID_IServerManager = new("4036B695-CA92-45EA-8965-CE1947A6B269");
        public static readonly Guid CLSID_IUnknown = new("00000000-0000-0000-C000-000000000046");

        private static bool IsAlive() => true;

        public static IServerManager TryCreateServerManager() =>
            TryCreateInstance<IServerManager>(CLSID_IServerManager, CLSCTX_ALL, TimeSpan.FromSeconds(30));

        internal static T TryCreateInstance<T>(in Guid rclsid, uint dwClsContext = 1)
        {
            int hresult = CoCreateInstance(rclsid, 0, dwClsContext, CLSID_IUnknown, out nint result);
            if (hresult < 0)
            {
                return default;
            }
            return Marshaler<T>.FromAbi(result);
        }

        internal static T TryCreateInstance<T>(in Guid rclsid, uint dwClsContext, in TimeSpan period) where T : ISetMonitor
        {
            T results = TryCreateInstance<T>(rclsid, dwClsContext);
            results?.SetMonitor(IsAlive, period);
            return results;
        }

        [LibraryImport("api-ms-win-core-com-l1-1-0.dll")]
        private static partial int CoCreateInstance(in Guid rclsid, nint pUnkOuter, uint dwClsContext, in Guid riid, out nint ppv);

        [LibraryImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static partial int CoRevokeClassObject(uint dwRegister);
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

    // https://docs.microsoft.com/windows/win32/api/unknwn/nn-unknwn-iclassfactory
    [GeneratedComInterface]
    [Guid("00000001-0000-0000-C000-000000000046")]
    public partial interface IClassFactory
    {
        void CreateInstance(nint pUnkOuter, in Guid riid, out nint ppvObject);

        void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
    }
}
