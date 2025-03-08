#include "pch.h"
#include "Factory.h"
#include "main.h"

using namespace APKInstaller::Metadata;
using namespace std::chrono;

const CLSID& Factory::GetCLSID()
{
    static const CLSID CLSID_ServerManager = { 0x4036b695, 0xca92, 0x45ea, { 0x89, 0x65, 0xce, 0x19, 0x47, 0xa6, 0xb2, 0x69 } }; // 4036B695-CA92-45EA-8965-CE1947A6B269
    return CLSID_ServerManager;
}

Windows::Foundation::IAsyncAction Factory::ReleaseServerAsync()
{
    _conReleaseCount++;
    co_await resume_after(seconds(10));
    _conReleaseCount--;
    if (_conReleaseCount) { co_return; }
    _releaseNotifier();
}

Windows::Foundation::IInspectable Factory::ActivateInstance()
{
    ServerManager result = ServerManager::ServerManager();
    if (result == nullptr) { return nullptr; }
    CoAddRefServerProcess();
    result.ServerManagerDestructed(
        [](Windows::Foundation::IInspectable, bool)
        {
            if (CoReleaseServerProcess() == 0)
            {
                ReleaseServerAsync();
            }
        });
    return result.as<IInspectable>();
}

HRESULT STDMETHODCALLTYPE Factory::CreateInstance(::IUnknown* pUnkOuter, REFIID riid, void** ppvObject) try
{
    if (!ppvObject) { return E_POINTER; }
    *ppvObject = nullptr;
    if (pUnkOuter != nullptr) { return CLASS_E_NOAGGREGATION; }
    ServerManager result = ServerManager::ServerManager();
    if (!result) { return S_FALSE; }
    CoAddRefServerProcess();
    result.ServerManagerDestructed(
        [](Windows::Foundation::IInspectable, bool)
        {
            if (CoReleaseServerProcess() == 0)
            {
                ReleaseServerAsync();
            }
        });
    return result.as(riid, ppvObject);
}
catch (...)
{
    return to_hresult();
}

HRESULT STDMETHODCALLTYPE Factory::LockServer(BOOL fLock) try
{
    if (fLock)
    {
        CoAddRefServerProcess();
    }
    else if (CoReleaseServerProcess() == 0)
    {
        ReleaseServerAsync();
    }
    return S_OK;
}
catch (...)
{
    return to_hresult();
}
