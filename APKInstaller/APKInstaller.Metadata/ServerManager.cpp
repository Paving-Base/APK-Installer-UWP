#include "pch.h"
#include "ServerManager.h"
#include "ServerManager.g.cpp"

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

    unsigned int ServerManager::RunProcess(hstring filename, hstring command, IVector<hstring> errorOutput, IVector<hstring> standardOutput)
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

        if (standardOutput)
        {
            ReadFromPipe(parentOutputPipeHandle, standardOutput, encode);
        }

        if (errorOutput)
        {
            ReadFromPipe(parentErrorPipeHandle, errorOutput, encode);
        }

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

    IAsyncOperation<unsigned int> ServerManager::RunProcessAsync(hstring filename, hstring command, IVector<hstring> errorOutput, IVector<hstring> standardOutput)
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

        if (standardOutput)
        {
            ReadFromPipe(parentOutputPipeHandle, standardOutput, encode);
        }

        if (errorOutput)
        {
            ReadFromPipe(parentErrorPipeHandle, errorOutput, encode);
        }

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

        co_return _exitCode;
    }

    IAsyncOperation<unsigned int> Metadata::implementation::ServerManager::DumpAsync(hstring filename, hstring command, DumpDelegate callback, IVector<hstring> output)
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

        const size_t bufferLen = defaultBufferSize;
        char* buffer = new char[bufferLen];
        memset(buffer, '\0', bufferLen);
        DWORD recLen = 0;
        int index = 0;
        bool terminated = false;
        std::wstringstream line = {};
        do
        {
            if (!ReadFile(parentOutputPipeHandle, buffer, bufferLen, &recLen, NULL))
            {
                break;
            }
            if (recLen <= 0) { break; }

            const int dBufSize = MultiByteToWideChar(encode, 0, buffer, recLen, NULL, 0);
            wchar_t* dBuf = new wchar_t[dBufSize];
            wmemset(dBuf, '\0', dBufSize);
            const int nRet = MultiByteToWideChar(encode, 0, buffer, recLen, dBuf, dBufSize);

            if (nRet > 0)
            {
                for (int i = 0; i < nRet; i++)
                {
                    if (dBuf[i] == L'\r')
                    {
                        continue;
                    }
                    else if (dBuf[i] == L'\n')
                    {
                        if (line.rdbuf()->in_avail())
                        {
                            hstring msg = (hstring)line.str();

                            if ((terminated = callback(msg, index)) == true)
                            {
                                try
                                {
                                    TerminateProcess(processInfo.hProcess, S_OK);
                                }
                                catch (...) {}
                            }
                            else
                            {
                                index++;
                            }

                            output.Append(msg);
                            line.str(L"");
                        }
                    }
                    else
                    {
                        line << dBuf[i];
                    }
                }
            }
            else
            {
                for (DWORD i = 0; i < recLen; i++)
                {
                    if (buffer[i] == '\r')
                    {
                        continue;
                    }
                    else if (buffer[i] == '\n')
                    {
                        if (line.rdbuf()->in_avail())
                        {
                            std::wstring msg = line.str();

                            if ((terminated = callback(msg, index)) == true)
                            {
                                try
                                {
                                    TerminateProcess(processInfo.hProcess, S_OK);
                                }
                                catch (...) {}
                            }
                            else
                            {
                                index++;
                            }

                            output.Append(msg);
                            line.str(L"");
                        }
                    }
                    else
                    {
                        line << buffer[i];
                    }
                }
            }

            delete dBuf;
            if (recLen < bufferLen) { break; }
        } while (recLen > 0 && !terminated);
        if (line.rdbuf()->in_avail())
        {
            std::wstring msg = line.str();
            if ((terminated = callback(msg, index)) == false) { index++; }
            output.Append(msg);
        }
        delete buffer;

        ReadFromPipe(parentErrorPipeHandle, output, encode);
        TerminateProcess(processInfo.hProcess, S_OK);

        DWORD _exitCode = 0;
        GetExitCodeProcess(processInfo.hProcess, &_exitCode);

        CloseHandle(parentOutputPipeHandle);
        CloseHandle(parentErrorPipeHandle);
        CloseHandle(processInfo.hProcess);
        CloseHandle(processInfo.hThread);

        co_return _exitCode;
    }
}
