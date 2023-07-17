using CoreHook.BinaryInjection;
using CoreHook.BinaryInjection.NativeDTO;
using CoreHook.Extensions;
using CoreHook.IPC.Platform;
using CoreHook.Loader;

using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace CoreHook.HookDefinition;

/// <summary>
/// 
/// </summary>
public static class RemoteHook
{
    /// <summary>
    /// The name of the native detour module for 64-bit processes.
    /// </summary>
    private const string CoreHookingModule64 = "x64\\corehook64.dll";

    /// <summary>
    /// The name of the native detour module for 32-bit processes.
    /// </summary>
    private const string CoreHookingModule32 = "x86\\corehook32.dll";

    /// <summary>
    /// The name of the pipe used for notifying the host process
    /// if the hooking plugin has been loaded successfully in
    /// the target process or if loading failed.
    /// </summary>
    private const string PIPE_NAME_BASE = "CoreHookInjection_";

    /// <summary>
    /// The .NET Assembly class that loads the .NET plugin, resolves any references, and executes
    /// the IEntryPoint.Run method for that plugin.
    /// </summary>
    private static readonly AssemblyDelegate CoreHookLoaderDelegate = new("CoreHook", "CoreHook.Loader.PluginLoader", "Load", "CoreHook.Loader.PluginLoader+LoadDelegate, CoreHook");

    /// <summary>
    /// Inject and load the CoreHook hooking module <paramref name="injectionLibrary"/>
    /// in the existing created process referenced by <paramref name="targetProcessId"/>.
    /// </summary>
    /// <param name="targetProcessId">The target process ID to inject and load plugin into.</param>
    /// <param name="logger"></param>
    /// <param name="hookLibrary"></param>
    /// <param name="pipePlatform"></param>
    /// <param name="parameters"></param>
    public static bool InjectDllIntoTarget(Process targetProcess, string hookLibrary, ILoggerFactory loggerFactory, IPipePlatform? pipePlatform = null, params object[] parameters)
    {
        var logger = loggerFactory.CreateLogger("RemoteHook");

        if (!File.Exists(hookLibrary))
        {
            throw new FileNotFoundException("File path {hookLibrary} is invalid or file does not exist.", hookLibrary);
        }

        logger.LogInformation("Hook library '{hookLibrary}' found. Injecting.", hookLibrary);

        var injectionPipeName = PIPE_NAME_BASE + targetProcess.Id;

        var managedFuncArgs = new ManagedFunctionArguments(CoreHookLoaderDelegate, injectionPipeName, new ManagedRemoteInfo(Environment.ProcessId, injectionPipeName, Path.GetFullPath(hookLibrary), parameters));

        var corehookPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, targetProcess.Is64Bit() ? CoreHookingModule64 : CoreHookingModule32);
        if (!File.Exists(corehookPath))
        {
            throw new FileNotFoundException("File path {corehookPath} is invalid or file does not exist.", corehookPath);
        }

        if (targetProcess.IsPackagedApp(out string _))
        {
            UwpSecurityHelper.GrantAllAppPackagesAccessToFile(corehookPath);
        }

        var coreLoadPath = Assembly.GetExecutingAssembly().Location;

        using var injector = new RemoteInjector(targetProcess, pipePlatform ?? IPipePlatform.Default, injectionPipeName, loggerFactory);
        injector.InjectLibraries(corehookPath);
        return injector.InjectManaged(coreLoadPath, managedFuncArgs);
    }
}
