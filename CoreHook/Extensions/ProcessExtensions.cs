using CoreHook.HookDefinition;
using CoreHook.IPC.Platform;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace CoreHook.Extensions;
public static partial class ProcessExtensions
{
    public static List<Process> GetChildProcesses(this Process process)
    {
        using ManagementObjectSearcher searcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessId = {process.Id}");
        using ManagementObjectCollection processList = searcher.Get();

        return processList.Cast<ManagementObject>().Select(p => Process.GetProcessById(Convert.ToInt32(p.GetPropertyValue("ProcessId")))).ToList();
    }

    public static bool AttachHook(this Process process, string injectionLibrary, ILoggerFactory loggerFactory, IPipePlatform pipeplatform, params object[] parameters)
    {
        return RemoteHook.InjectDllIntoTarget(process, injectionLibrary, loggerFactory, pipeplatform, parameters);
    }

    public static bool AttachHook(this Process process, string injectionLibrary, ILoggerFactory loggerFactory, params object[] parameters)
    {
        return RemoteHook.InjectDllIntoTarget(process, injectionLibrary, loggerFactory, parameters: parameters);
    }
}
