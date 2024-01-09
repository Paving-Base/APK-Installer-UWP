#include "pch.h"
#include "ServerManager.h"
#include "ServerManager.g.cpp"
#include "networkisolation.h"
#include "sddl.h"
#include "winrt/Windows.ApplicationModel.h"

using namespace std::chrono;
using namespace Windows::ApplicationModel;

namespace winrt::APKInstaller::Metadata::implementation
{
    ServerManager::~ServerManager()
    {
        m_serverManagerDestructedEvent(*this, true);
    }

    event_token ServerManager::ServerManagerDestructed(EventHandler<bool> const& handler)
    {
        return m_serverManagerDestructedEvent.add(handler);
    }

    void ServerManager::ServerManagerDestructed(winrt::event_token const& token)
    {
        m_serverManagerDestructedEvent.remove(token);
    }

    unsigned int ServerManager::RunProcess(hstring filename, hstring command, IVector<hstring> errorOutput, IVector<hstring> standardOutput) const
    {
        const hstring commandLine = BuildCommandLine(filename, command);

        STARTUPINFO startupInfo = { 0 };
        PROCESS_INFORMATION processInfo = { 0 };
        SECURITY_ATTRIBUTES unused_SecAttrs = { 0 };

        HANDLE parentOutputPipeHandle = nullptr;
        HANDLE childOutputPipeHandle = nullptr;
        HANDLE parentErrorPipeHandle = nullptr;
        HANDLE childErrorPipeHandle = nullptr;

        try
        {
            startupInfo.cb = sizeof(STARTUPINFO);

            CreatePipe(&parentOutputPipeHandle, &childOutputPipeHandle, false);
            CreatePipe(&parentErrorPipeHandle, &childErrorPipeHandle, false);

            startupInfo.hStdOutput = childOutputPipeHandle;
            startupInfo.hStdError = childErrorPipeHandle;

            startupInfo.dwFlags = STARTF_USESTDHANDLES;

            startupInfo.wShowWindow = SW_HIDE;
            startupInfo.dwFlags |= STARTF_USESHOWWINDOW;

            // set up the creation flags parameter
            const int creationFlags = 0 | CREATE_NO_WINDOW;

            int errorCode = 0;

            const bool retVal = CreateProcess(
                nullptr,                                    // we don't need this since all the info is in commandLine
                const_cast<LPWSTR>(commandLine.c_str()),    // pointer to the command line string
                &unused_SecAttrs,                           // address to process security attributes, we don't need to inherit the handle
                &unused_SecAttrs,                           // address to thread security attributes.
                true,                                       // handle inheritance flag
                creationFlags,                              // creation flags
                nullptr,                                    // pointer to new environment block
                nullptr,                                    // pointer to current directory name
                &startupInfo,                               // pointer to STARTUPINFO
                &processInfo                                // pointer to PROCESS_INFORMATION
            );

            if (!retVal)
            {
                errorCode = GetLastError();
            }
        }
        catch (...)
        {
            CloseHandle(parentOutputPipeHandle);
            CloseHandle(parentErrorPipeHandle);
            CloseHandle(childOutputPipeHandle);
            CloseHandle(childErrorPipeHandle);
            throw GetLastError();
        }

        CloseHandle(childOutputPipeHandle);
        CloseHandle(childErrorPipeHandle);

        const UINT encode = GetConsoleOutputCP();

        ReadFromPipe(parentOutputPipeHandle, standardOutput, encode);
        ReadFromPipe(parentErrorPipeHandle, errorOutput, encode);

        if (WaitForSingleObject(processInfo.hProcess, 5000) == WAIT_TIMEOUT)
        {
            TerminateProcess(processInfo.hProcess, (UINT)-1);
        }

        DWORD _exitCode = 0;
        GetExitCodeProcess(processInfo.hProcess, &_exitCode);

        CloseHandle(parentOutputPipeHandle);
        CloseHandle(parentErrorPipeHandle);
        CloseHandle(processInfo.hProcess);
        CloseHandle(processInfo.hThread);

        return _exitCode;
    }

    IAsyncOperation<unsigned int> ServerManager::RunProcessAsync(hstring filename, hstring command, IVector<hstring> errorOutput, IVector<hstring> standardOutput) const
    {
        const hstring commandLine = BuildCommandLine(filename, command);

        STARTUPINFO startupInfo = { 0 };
        PROCESS_INFORMATION processInfo = { 0 };
        SECURITY_ATTRIBUTES unused_SecAttrs = { 0 };

        HANDLE parentOutputPipeHandle = nullptr;
        HANDLE childOutputPipeHandle = nullptr;
        HANDLE parentErrorPipeHandle = nullptr;
        HANDLE childErrorPipeHandle = nullptr;

        try
        {
            startupInfo.cb = sizeof(STARTUPINFO);

            CreatePipe(&parentOutputPipeHandle, &childOutputPipeHandle, false);
            CreatePipe(&parentErrorPipeHandle, &childErrorPipeHandle, false);

            startupInfo.hStdOutput = childOutputPipeHandle;
            startupInfo.hStdError = childErrorPipeHandle;

            startupInfo.dwFlags = STARTF_USESTDHANDLES;

            startupInfo.wShowWindow = SW_HIDE;
            startupInfo.dwFlags |= STARTF_USESHOWWINDOW;

            // set up the creation flags parameter
            const int creationFlags = 0 | CREATE_NO_WINDOW;

            int errorCode = 0;

            const bool retVal = CreateProcess(
                nullptr,                                    // we don't need this since all the info is in commandLine
                const_cast<LPWSTR>(commandLine.c_str()),    // pointer to the command line string
                &unused_SecAttrs,                           // address to process security attributes, we don't need to inherit the handle
                &unused_SecAttrs,                           // address to thread security attributes.
                true,                                       // handle inheritance flag
                creationFlags,                              // creation flags
                nullptr,                                    // pointer to new environment block
                nullptr,                                    // pointer to current directory name
                &startupInfo,                               // pointer to STARTUPINFO
                &processInfo                                // pointer to PROCESS_INFORMATION
            );

            if (!retVal)
            {
                errorCode = GetLastError();
            }
        }
        catch (...)
        {
            CloseHandle(parentOutputPipeHandle);
            CloseHandle(parentErrorPipeHandle);
            CloseHandle(childOutputPipeHandle);
            CloseHandle(childErrorPipeHandle);
            throw GetLastError();
        }

        CloseHandle(childOutputPipeHandle);
        CloseHandle(childErrorPipeHandle);

        const UINT encode = GetConsoleOutputCP();

        ReadFromPipe(parentOutputPipeHandle, standardOutput, encode);
        ReadFromPipe(parentErrorPipeHandle, errorOutput, encode);

        if (co_await resume_on_signal(processInfo.hProcess, seconds(5)))
        {
            TerminateProcess(processInfo.hProcess, (UINT)-1);
        }

        DWORD _exitCode = 0;
        GetExitCodeProcess(processInfo.hProcess, &_exitCode);

        CloseHandle(parentOutputPipeHandle);
        CloseHandle(parentErrorPipeHandle);
        CloseHandle(processInfo.hProcess);
        CloseHandle(processInfo.hThread);

        co_return _exitCode;
    }

    IAsyncOperation<unsigned int> ServerManager::DumpAsync(hstring filename, hstring command, DumpDelegate callback, IVector<hstring> output, int encode) const
    {
        const hstring commandLine = BuildCommandLine(filename, command);

        STARTUPINFO startupInfo = { 0 };
        PROCESS_INFORMATION processInfo = { 0 };
        SECURITY_ATTRIBUTES unused_SecAttrs = { 0 };

        HANDLE parentOutputPipeHandle = nullptr;
        HANDLE childOutputPipeHandle = nullptr;
        HANDLE parentErrorPipeHandle = nullptr;
        HANDLE childErrorPipeHandle = nullptr;

        try
        {
            startupInfo.cb = sizeof(STARTUPINFO);

            CreatePipe(&parentOutputPipeHandle, &childOutputPipeHandle, false);
            CreatePipe(&parentErrorPipeHandle, &childErrorPipeHandle, false);

            startupInfo.hStdOutput = childOutputPipeHandle;
            startupInfo.hStdError = childErrorPipeHandle;

            startupInfo.dwFlags = STARTF_USESTDHANDLES;

            startupInfo.wShowWindow = SW_HIDE;
            startupInfo.dwFlags |= STARTF_USESHOWWINDOW;

            // set up the creation flags parameter
            const int creationFlags = 0 | CREATE_NO_WINDOW;

            int errorCode = 0;

            const bool retVal = CreateProcess(
                nullptr,                                    // we don't need this since all the info is in commandLine
                const_cast<LPWSTR>(commandLine.c_str()),    // pointer to the command line string
                &unused_SecAttrs,                           // address to process security attributes, we don't need to inherit the handle
                &unused_SecAttrs,                           // address to thread security attributes.
                true,                                       // handle inheritance flag
                creationFlags,                              // creation flags
                nullptr,                                    // pointer to new environment block
                nullptr,                                    // pointer to current directory name
                &startupInfo,                               // pointer to STARTUPINFO
                &processInfo                                // pointer to PROCESS_INFORMATION
            );

            if (!retVal)
            {
                errorCode = GetLastError();
            }
        }
        catch (...)
        {
            CloseHandle(parentOutputPipeHandle);
            CloseHandle(parentErrorPipeHandle);
            CloseHandle(childOutputPipeHandle);
            CloseHandle(childErrorPipeHandle);
            throw GetLastError();
        }

        CloseHandle(childOutputPipeHandle);
        CloseHandle(childErrorPipeHandle);

        if (callback && ReadFromPipe(parentOutputPipeHandle, callback, output, encode))
        {
            TerminateProcess(processInfo.hProcess, (UINT)-1);
            goto end;
        }
        else
        {
            ReadFromPipe(parentOutputPipeHandle, output, encode);
        }
        ReadFromPipe(parentErrorPipeHandle, output, encode);

    end:
        if (co_await resume_on_signal(processInfo.hProcess, seconds(5)))
        {
            TerminateProcess(processInfo.hProcess, (UINT)-1);
        }

        DWORD _exitCode = 0;
        GetExitCodeProcess(processInfo.hProcess, &_exitCode);

        CloseHandle(parentOutputPipeHandle);
        CloseHandle(parentErrorPipeHandle);
        CloseHandle(processInfo.hProcess);
        CloseHandle(processInfo.hThread);

        co_return _exitCode;
    }

    bool ServerManager::EnableLoopback() const
    {
        vector<wstring> list = vector<wstring>();
        HINSTANCE firewallAPI = LoadLibrary(L"FirewallAPI.dll");
        if (!firewallAPI) { return false; }

        decltype(&NetworkIsolationGetAppContainerConfig) NetworkIsolationGetAppContainerConfig =
            (decltype(NetworkIsolationGetAppContainerConfig))GetProcAddress(
                firewallAPI,
                "NetworkIsolationGetAppContainerConfig");

        if (NetworkIsolationGetAppContainerConfig)
        {
            DWORD size = 0;
            PSID_AND_ATTRIBUTES arrayValue = nullptr;

            if (NetworkIsolationGetAppContainerConfig(&size, &arrayValue) == S_OK)
            {
                if (arrayValue != nullptr)
                {
                    for (DWORD i = 0; i < size; i++)
                    {
                        LPWSTR sid;
                        const SID_AND_ATTRIBUTES cur = arrayValue[i];
                        if (cur.Sid != nullptr)
                        {
                            ConvertSidToStringSid(cur.Sid, &sid);
                            if (sid != nullptr)
                            {
                                list.push_back(sid);
                            }
                        }
                    }
                }
            }
        }

        LPWSTR currentSid = nullptr;

        decltype(&NetworkIsolationEnumAppContainers) NetworkIsolationEnumAppContainers =
            (decltype(NetworkIsolationEnumAppContainers))GetProcAddress(
                firewallAPI,
                "NetworkIsolationEnumAppContainers");

        if (NetworkIsolationEnumAppContainers)
        {
            DWORD size = 0;
            PINET_FIREWALL_APP_CONTAINER arrayValue = nullptr;
            hstring fullName = Package::Current().Id().FullName();

            if (NetworkIsolationEnumAppContainers(NETISO_FLAG::NETISO_FLAG_MAX, &size, &arrayValue) == S_OK)
            {
                if (arrayValue != nullptr)
                {
                    for (DWORD i = 0; i < size; i++)
                    {
                        const INET_FIREWALL_APP_CONTAINER cur = arrayValue[i];
                        if (cur.packageFullName)
                        {
                            wstring packageFullName = cur.packageFullName;
                            if (packageFullName.compare(fullName) == 0)
                            {
                                ConvertSidToStringSid(cur.appContainerSid, &currentSid);
                                break;
                            }
                        }
                    }

                    decltype(&NetworkIsolationFreeAppContainers) NetworkIsolationFreeAppContainers =
                        (decltype(NetworkIsolationFreeAppContainers))GetProcAddress(
                            firewallAPI,
                            "NetworkIsolationFreeAppContainers");

                    if (NetworkIsolationFreeAppContainers)
                    {
                        NetworkIsolationFreeAppContainers(arrayValue);
                    }
                }
            }
        }

        if (currentSid)
        {
            for (wstring left : list)
            {
                if (left.compare(currentSid) == 0)
                {
                    return true;
                }
            }

            decltype(&NetworkIsolationSetAppContainerConfig) NetworkIsolationSetAppContainerConfig =
                (decltype(NetworkIsolationSetAppContainerConfig))GetProcAddress(
                    firewallAPI,
                    "NetworkIsolationSetAppContainerConfig");

            if (NetworkIsolationSetAppContainerConfig)
            {
                vector<SID_AND_ATTRIBUTES> arr;
                DWORD count = 0;

                list.push_back(currentSid);
                for (wstring app : list)
                {
                    SID_AND_ATTRIBUTES sid{};
                    sid.Attributes = 0;
                    //TO DO:
                    PSID ptr = nullptr;
                    ConvertStringSidToSid(app.c_str(), &ptr);
                    sid.Sid = ptr;
                    arr.push_back(sid);
                    count++;
                }

                return NetworkIsolationSetAppContainerConfig(count, arr.data()) == S_OK;
            }
        }

        return false;
    }
}
