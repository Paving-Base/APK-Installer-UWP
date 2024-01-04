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
        IAsyncOperation<unsigned int> DumpAsync(hstring filename, hstring command, DumpDelegate callback, IVector<hstring> output, int encode);

    private:
        const size_t defaultBufferSize = 1024;
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

        inline const hstring BuildCommandLine(const hstring fileName, const hstring command)
        {
            // Construct a StringBuilder with the appropriate command line
            // to pass to CreateProcess.  If the filename isn't already
            // in quotes, we quote it here.  This prevents some security
            // problems (it specifies exactly which part of the string
            // is the file to execute).
            const bool fileNameIsQuoted = !fileName.empty() && fileName.starts_with('"') && fileName.ends_with('"');
            return fileNameIsQuoted ? fileName + ' ' + command : '"' + fileName + L"\" " + command;
        }

        inline void ReadFromPipe(const HANDLE hPipeRead, const IVector<hstring> output, const UINT encode) const
        {
            const size_t bufferLen = defaultBufferSize;
            char* buffer = new char[bufferLen];
            memset(buffer, '\0', bufferLen);
            DWORD recLen = 0;
            if (output)
            {
                std::wstringstream line = {};
                do
                {
                    if (!ReadFile(hPipeRead, buffer, bufferLen, &recLen, NULL))
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
                                    output.Append(line.str());
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
                                    output.Append(line.str());
                                    line.str(L"");
                                }
                            }
                            else
                            {
                                line << buffer[i];
                            }
                        }
                    }

                    delete[] dBuf;
                    if (recLen < bufferLen) { break; }
                } while (recLen > 0);
                if (line.rdbuf()->in_avail())
                {
                    output.Append(line.str());
                }
            }
            else
            {
                do
                {
                    if (!ReadFile(hPipeRead, buffer, bufferLen, &recLen, NULL))
                    {
                        break;
                    }
                } while (recLen > 0);
            }
            delete[] buffer;
        }

        inline IAsyncAction ProcessCallback(DumpDelegate callback, hstring line, int index, bool& terminated) const
        {
            co_await resume_background();
            try
            {
                if (!terminated && callback(line, index))
                {
                    terminated = true;
                }
            }
            catch (...) {}
        }

        inline bool ReadFromPipe(const HANDLE hPipeRead, DumpDelegate callback, const IVector<hstring> output, const UINT encode) const
        {
            const size_t bufferLen = defaultBufferSize;
            char* buffer = new char[bufferLen];
            memset(buffer, '\0', bufferLen);
            bool terminated = false;
            DWORD recLen = 0;
            int index = 0;
            std::wstringstream line = {};
            do
            {
                if (!ReadFile(hPipeRead, buffer, bufferLen, &recLen, NULL))
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
                                line.str(L"");
                                output.Append(msg);
                                ProcessCallback(callback, msg, index, terminated);
                                if (terminated) { goto end; }
                                index++;
                            }
                        }
                        else
                        {
                            line << dBuf[i];
                        }
                        if (terminated) { break; }
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
                                hstring msg = (hstring)line.str();
                                line.str(L"");
                                output.Append(msg);
                                ProcessCallback(callback, msg, index, terminated);
                                if (terminated) { goto end; }
                                index++;
                            }
                        }
                        else
                        {
                            line << buffer[i];
                        }
                    }
                }

                delete[] dBuf;
                if (recLen < bufferLen) { break; }
            } while (recLen > 0 && !terminated);
            if (line.rdbuf()->in_avail())
            {
                hstring msg = (hstring)line.str();
                line.str(L"");
                output.Append(msg);
                ProcessCallback(callback, msg, index, terminated);
                index++;
            }
        end:
            delete[] buffer;
            return terminated;
        }
    };
}

namespace winrt::APKInstaller::Metadata::factory_implementation
{
    struct ServerManager : ServerManagerT<ServerManager, implementation::ServerManager>
    {
    };
}
