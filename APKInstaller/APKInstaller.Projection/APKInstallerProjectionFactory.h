#pragma once

#include "APKInstallerProjectionFactory.g.h"
#include "winrt/APKInstaller.Metadata.h"

using namespace winrt::APKInstaller::Metadata;

namespace winrt::APKInstaller::Projection::implementation
{
    struct APKInstallerProjectionFactory : APKInstallerProjectionFactoryT<APKInstallerProjectionFactory>
    {
        static ServerManager ServerManager();
    };
}

namespace winrt::APKInstaller::Projection::factory_implementation
{
    struct APKInstallerProjectionFactory : APKInstallerProjectionFactoryT<APKInstallerProjectionFactory, implementation::APKInstallerProjectionFactory>
    {
    };
}
