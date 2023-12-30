#pragma once

using namespace winrt;

static unsigned int _conReleaseCount = 0;

struct Factory : winrt::implements<Factory, Windows::Foundation::IActivationFactory, IClassFactory>
{
    static const CLSID& GetCLSID();
    static Windows::Foundation::IAsyncAction ReleaseServerAsync();

    // IActivationFactory
    Windows::Foundation::IInspectable ActivateInstance();

    // IClassFactory
    HRESULT STDMETHODCALLTYPE CreateInstance(::IUnknown* pUnkOuter, REFIID riid, void** ppvObject);
    HRESULT STDMETHODCALLTYPE LockServer(BOOL fLock);
};
