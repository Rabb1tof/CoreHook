using CoreHook.BinaryInjection.NativeDTO;
using CoreHook.BinaryInjection.RemoteInjection;
using CoreHook.IPC.Platform;
using CoreHook.Loader;

using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;

namespace CoreHook.BinaryInjection;

public class RemoteInjector : IDisposable
{
    private readonly ILogger _logger;
    
    private readonly Process _targetProcess;
    private readonly ManagedProcess _managedProcess;
    
    private readonly string _injectionPipeName;
    private readonly InjectionHelper _injectionHelper;

    public RemoteInjector(Process targetProcess, IPipePlatform pipePlatform, string injectionPipeName, ILoggerFactory logFactory)
    {
        if (string.IsNullOrWhiteSpace(injectionPipeName))
        {
            throw new ArgumentException("Invalid injection pipe name");
        }

        _logger = logFactory.CreateLogger(nameof(RemoteInjector));

        _targetProcess = targetProcess;
        _managedProcess = new ManagedProcess(targetProcess);

        _injectionPipeName = injectionPipeName;
        _injectionHelper = new InjectionHelper(injectionPipeName, pipePlatform, _logger);
    }

    public bool InjectLibraries(params string[] libraries)
    {
        try
        {
            foreach (var lib in libraries)
            {
                _logger.LogInformation("Injecting library: {lib}", lib);
                _managedProcess.InjectModule(lib);
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to inject.");
            return false;
        }
    }

    /// <summary>
    /// Start CoreCLR and execute a .NET assembly in a target process.
    /// </summary>
    /// <param name="localProcessId">Process ID of the process communicating with the target process.</param>
    /// <param name="targetProcessId">The process ID of the process to inject the .NET assembly into.</param>
    /// <param name="remoteInjectorConfig">Configuration settings for starting CoreCLR and executing .NET assemblies.</param>
    /// <param name="pipePlatform">Class for creating pipes for communication with the target process.</param>
    /// <param name="passThruArguments">Arguments passed to the .NET hooking plugin once it is loaded in the target process.</param>
    public bool InjectNative<T>(string hostLibrary, string method, T arguments, bool waitForExit = true)
    {
        _logger.LogInformation("Starting injection for {lib} / {method}({type})", hostLibrary, method, typeof(T));

        //TODO: useless when waitForExit == true?
        _injectionHelper.BeginInjection(_targetProcess.Id);

        try
        {
            _logger.LogInformation("Injecting module: {module}", hostLibrary);
            _managedProcess.InjectModule(hostLibrary);

            _logger.LogInformation($"Creating thread and {(waitForExit ? "synchronously" : "asynchronously")} calling {method}..");
            _managedProcess.CreateThread(hostLibrary, method, ref arguments, waitForExit);

            // If we didn't wait for the thread to exit, it means the injected module will notify through the pipe when injection is done
            if (!waitForExit)
            {
                _logger.LogInformation($"Waiting for the injection notification...");
                _injectionHelper.WaitForInjection(_targetProcess.Id, 100000);
            }

            _logger.LogInformation($"Injection done!");

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to inject.");
            return false;
        }
        finally
        {
            _injectionHelper.EndInjection(_targetProcess.Id);
        }
    }

    public bool InjectManaged(string coreLoadPath, AssemblyDelegate? assemblyDelegate, ManagedRemoteInfo args)//ManagedFunctionArguments managedFuncArgs)
    {
        var is64Bits = _targetProcess.Is64Bit();
        _logger.LogInformation("Process #{targetProcessId} is {64or32}.", _targetProcess.Id, is64Bits ? "64bits" : "32bits");

        var paths = ModulesPathHelper.GetCoreClrPaths(is64Bits, coreLoadPath);
        _logger.LogInformation(".NET Path: {coreRootPath}\r\nNativeHost path: {coreHostPath}\r\nNethost path: {nethostLibPath}", paths.coreRootPath, paths.coreHostPath, paths.nethostLibPath);

        if (_targetProcess.IsPackagedApp(out string _))
        {
            // Make sure the native dll modules can be accessed by the UWP application
            UwpSecurityHelper.GrantAllAppPackagesAccessToFile(paths.coreHostPath);
        }

        var startCoreCLRArgs = new CoreHostArguments(coreLoadPath, paths.coreRootPath, _injectionPipeName);

        if (InjectLibraries(paths.nethostLibPath) && InjectNative(paths.coreHostPath, "StartCoreCLR", startCoreCLRArgs, true))
        {
            _logger.LogInformation("Successfully started the .NET CLR in the target process.");

            var managedFuncArgs = new AssemblyFunctionCall(assemblyDelegate ?? PluginLoader.DefaultDelegate, _injectionPipeName, args);
            if (InjectNative(paths.coreHostPath, "ExecuteAssemblyFunction", managedFuncArgs, false))
            {
                _logger.LogInformation("Successfully executed target .NET method.");
                return true;
            }
        }

        return false;
    }

    ~RemoteInjector()
    {
        Dispose();
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing...");

        _managedProcess.Dispose();
        _injectionHelper.Dispose();
    }

}
