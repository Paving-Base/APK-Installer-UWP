using APKInstaller.Metadata;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Windows.ApplicationModel;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace APKInstaller.Common
{
    public partial class Loopback : ILoopback
    {
        public static ILoopback Instance { get; } = new Loopback();

        public bool CreateFileSymbolic(string lpFileName, string lpExistingFileName)
        {
            if (PInvoke.CreateHardLink(lpFileName, lpExistingFileName))
            {
                Span<char> sidString = GetCurrentPackageSid(out _);
                if (!sidString.IsEmpty)
                {
                    FileInfo info = new(lpFileName);
                    FileSecurity security = info.GetAccessControl();
                    security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sidString.ToString()), FileSystemRights.ReadAndExecute, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
                    info.SetAccessControl(security);
                }
                return true;
            }
            return false;
        }

        public unsafe bool EnableLoopback()
        {
            if (PInvoke.NetworkIsolationGetAppContainerConfig(out uint size, out SID_AND_ATTRIBUTES* arrayValue) == 0)
            {
                ReadOnlySpan<SID_AND_ATTRIBUTES> list = new(arrayValue, (int)size);
                Span<char> currentSid = GetCurrentPackageSid(out nint sid);
                foreach (SID_AND_ATTRIBUTES sidAndAttributes in list)
                {
                    if (PInvoke.ConvertSidToStringSid(sidAndAttributes.Sid, out PWSTR stringSid))
                    {
                        if (((ReadOnlySpan<char>)currentSid).Equals(stringSid.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                return PInvoke.NetworkIsolationSetAppContainerConfig([
                    ..list,
                    new SID_AND_ATTRIBUTES
                    {
                        Sid = new PSID(sid),
                        Attributes = 0
                    }
                ]) == 0;
            }
            return false;
        }

        private static Span<char> GetCurrentPackageSid(out nint sid)
        {
            if (PackageSidFromFamilyName(Package.Current.Id.FamilyName, out sid) == 0)
            {
                if (PInvoke.ConvertSidToStringSid(new PSID(sid), out PWSTR stringSid))
                {
                    return stringSid.AsSpan();
                }
            }
            return [];
        }

        [LibraryImport("kernel.appcore.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial uint PackageSidFromFamilyName(string packageFamilyName, out nint sid);
    }
}
