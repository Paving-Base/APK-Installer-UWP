#pragma once

#include "ServerManager.g.h"

using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;

namespace winrt::APKInstaller::Metadata::implementation
{
    struct ServerManager : ServerManagerT<ServerManager>
    {
        ServerManager() = default;
        ~ServerManager();

        bool IsServerRunning() const { return true; }

        event_token ServerManagerDestructed(EventHandler<bool> const& handler);
        void ServerManagerDestructed(event_token const& token);

        unsigned int RunProcess(hstring filename, hstring command, IVector<hstring> errorOutput, IVector<hstring> standardOutput);
        IAsyncOperation<unsigned int> RunProcessAsync(hstring filename, hstring command, IVector<hstring> errorOutput, IVector<hstring> standardOutput);

    private:
        event<EventHandler<bool>> m_serverManagerDestructedEvent;

        static void CreatePipeWithSecurityAttributes(PHANDLE hReadPipe, PHANDLE hWritePipe, LPSECURITY_ATTRIBUTES lpPipeAttributes, int nSize)
        {
            const bool ret = ::CreatePipe(hReadPipe, hWritePipe, lpPipeAttributes, nSize);
            if (!ret || !hReadPipe || !hWritePipe)
            {
                throw GetLastError();
            }
        }

        /// <summary>
        /// Using synchronous Anonymous pipes for process input/output redirection means we would end up
        /// wasting a worker threadpool thread per pipe instance. Overlapped pipe IO is desirable, since
        /// it will take advantage of the NT IO completion port infrastructure. But we can't really use
        /// Overlapped I/O for process input/output as it would break Console apps (managed Console class
        /// methods such as WriteLine as well as native CRT functions like printf) which are making an
        /// assumption that the console standard handles (obtained via GetStdHandle()) are opened
        /// for synchronous I/O and hence they can work fine with ReadFile/WriteFile synchronously!
        /// </summary>
        static void CreatePipe(PHANDLE parentHandle, PHANDLE childHandle, const bool parentInputs)
        {
            SECURITY_ATTRIBUTES securityAttributesParent = { 0 };
            securityAttributesParent.bInheritHandle = true;

            HANDLE hTmp = nullptr;
            try
            {
                if (parentInputs)
                {
                    CreatePipeWithSecurityAttributes(childHandle, &hTmp, &securityAttributesParent, 0);
                }
                else
                {
                    CreatePipeWithSecurityAttributes(&hTmp,
                        childHandle,
                        &securityAttributesParent,
                        0);
                }
                // Duplicate the parent handle to be non-inheritable so that the child process
                // doesn't have access. This is done for correctness sake, exact reason is unclear.
                // One potential theory is that child process can do something brain dead like
                // closing the parent end of the pipe and there by getting into a blocking situation
                // as parent will not be draining the pipe at the other end anymore.
                const HANDLE currentProcHandle = GetCurrentProcess();
                if (!DuplicateHandle(currentProcHandle,
                    hTmp,
                    currentProcHandle,
                    parentHandle,
                    0,
                    false,
                    DUPLICATE_SAME_ACCESS))
                {
                    throw GetLastError();
                }
            }
            catch (...) {}

            if (hTmp)
            {
                CloseHandle(hTmp);
            }
        }

        inline const hstring BuildCommandLine(hstring fileName, hstring command)
        {
            // Construct a StringBuilder with the appropriate command line
            // to pass to CreateProcess.  If the filename isn't already
            // in quotes, we quote it here.  This prevents some security
            // problems (it specifies exactly which part of the string
            // is the file to execute).
            const bool fileNameIsQuoted = !fileName.empty() && fileName.starts_with('"') && fileName.ends_with('"');
            return fileNameIsQuoted ? fileName + ' ' + command : '"' + fileName + L"\" " + command;
        }
    };
}

namespace winrt::APKInstaller::Metadata::factory_implementation
{
    struct ServerManager : ServerManagerT<ServerManager, implementation::ServerManager>
    {
    };
}
