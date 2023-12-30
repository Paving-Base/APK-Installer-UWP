#include "pch.h"
#include "APKInstallerProjectionFactory.h"
#include "APKInstallerProjectionFactory.g.cpp"

namespace winrt::APKInstaller::Projection::implementation
{
    static const CLSID CLSID_ServerManager = { 0x4036b695, 0xca92, 0x45ea, { 0x89, 0x65, 0xce, 0x19, 0x47, 0xa6, 0xb2, 0x69 } }; // 4036B695-CA92-45EA-8965-CE1947A6B269

    ServerManager APKInstallerProjectionFactory::ServerManager()
    {
        return try_create_instance<::ServerManager>(CLSID_ServerManager, CLSCTX_ALL);
    }
}
