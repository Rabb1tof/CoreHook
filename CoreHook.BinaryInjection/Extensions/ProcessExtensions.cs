
using Microsoft.Win32.SafeHandles;

using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace CoreHook.BinaryInjection;

public static partial class ProcessExtensions
{
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial int GetPackageFullName(nint hProcess, out uint packageFullNameLength, [Out] char[] packageFullName);

    public static bool IsPackagedApp(this Process process, out string packageName)
    {
        var res = GetPackageFullName(process.Handle, out uint packageNameLength, null);

        if (res != 0 && packageNameLength > 0)
        {
            char[] buffer = ArrayPool<char>.Shared.Rent((int)packageNameLength);
            GetPackageFullName(process.Handle, out packageNameLength, buffer);

            packageName = new string(buffer, 0, (int)packageNameLength - 1);
            return true;
        }

        packageName = String.Empty;

        return false;
    }

    public static bool Is64Bit(this Process process)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Environment.Is64BitOperatingSystem;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("Platform not supported for detecting process architecture.");
        }

        if (!Environment.Is64BitOperatingSystem)
        {
            return false;
        }

        SafeFileHandle processHandle = NativeMethods.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION, false, (uint)process.Id);

        if (processHandle.IsInvalid)
        {
            throw new Win32Exception("Failed to open process query handle.");
        }

        using (processHandle)
        {
            BOOL processIsWow64 = false;
            if (!NativeMethods.IsWow64Process(processHandle, out processIsWow64))
            {
                throw new Win32Exception("Determining process architecture with IsWow64Process failed.");
            }

            return !processIsWow64;
        }
    }
}
