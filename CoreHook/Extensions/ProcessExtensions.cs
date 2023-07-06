using CoreHook.HookDefinition;
using CoreHook.IPC.Platform;

using Microsoft.Extensions.Logging;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace CoreHook.Extensions;
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

    public static List<Process> GetChildProcesses(this Process process)
    {
        using ManagementObjectSearcher searcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessId = {process.Id}");
        using ManagementObjectCollection processList = searcher.Get();

        return processList.Cast<ManagementObject>().Select(p => Process.GetProcessById(Convert.ToInt32(p.GetPropertyValue("ProcessId")))).ToList();
    }

    public static bool AttachHook(this Process process, string injectionLibrary, ILoggerFactory loggerFactory, IPipePlatform pipeplatform, params object[] parameters)
    {
        return RemoteHook.InjectDllIntoTarget(process.Id, injectionLibrary, loggerFactory, pipeplatform, parameters);
    }

    public static bool AttachHook(this Process process, string injectionLibrary, ILoggerFactory loggerFactory, params object[] parameters)
    {
        return RemoteHook.InjectDllIntoTarget(process.Id, injectionLibrary, loggerFactory, parameters: parameters);
    }
}
