using CoreHook.HookDefinition;
using CoreHook.IPC.Platform;

using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace CoreHook.Extensions;
public static class ProcessExtensions
{
    public static bool AttachHook(this Process process, string injectionLibrary, ILoggerFactory loggerFactory, IPipePlatform pipeplatform, params object[] parameters)
    {
        return RemoteHook.InjectDllIntoTarget(process.Id, injectionLibrary, loggerFactory, pipeplatform, parameters);
    }

    public static bool AttachHook(this Process process, string injectionLibrary, ILoggerFactory loggerFactory, params object[] parameters)
    {
        return RemoteHook.InjectDllIntoTarget(process.Id, injectionLibrary, loggerFactory, parameters: parameters);
    }
}
