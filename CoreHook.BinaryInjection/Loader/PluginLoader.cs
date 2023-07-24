using Microsoft.Extensions.Logging;

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using System.Text.Json;
using System.Threading.Tasks;

namespace CoreHook.Loader;

public class PluginLoader
{
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
    private static readonly ILogger _logger = _loggerFactory.CreateLogger<PluginLoader>();

    /// <summary>
    /// Initialize the plugin dependencies and execute its entry point.
    /// </summary>
    /// <param name="remoteInfoAddr">Parameters containing the plugin to load and the parameters to pass to it's entry point.</param>
    /// <returns>A status code representing the plugin initialization state.</returns>
    [UnmanagedCallersOnly]//(CallConvs = new[] { typeof(CallConvCdecl) })]
    public unsafe static int Load(IntPtr payLoadPtr)
    {
        NotificationHelper? hostNotifier = null;
        try
        {
            if (payLoadPtr == IntPtr.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(payLoadPtr), "Remote arguments address was zero");
            }

            var payLoadStr = Marshal.PtrToStringUni(payLoadPtr)!;

            var payLoad = JsonSerializer.Deserialize<ManagedRemoteInfo>(payLoadStr, new JsonSerializerOptions() { IncludeFields = true });

            payLoad.UserParams = payLoad.UserParams?.Zip(payLoad.UserParamsTypeNames!, (param, typeName) => param is null ? null : ((JsonElement)param).Deserialize(Type.GetType(typeName, true))).ToArray() ?? Array.Empty<object>();

            // Start the IPC message notifier with a connection to the host application.
            hostNotifier = new NotificationHelper(payLoad.ChannelName, _logger);

            _ = hostNotifier.Log($"Initializing plugin: {payLoad.UserLibrary}.");

            var resolver = new DependencyResolver(payLoad.UserLibrary, hostNotifier);

            // Execute the plugin library's entry point and pass in the user arguments.
            var loadPluginTask = LoadPlugin(resolver.Assembly, payLoad.UserParams, payLoad.ClassName, payLoad.MethodName, hostNotifier);
            loadPluginTask.Wait();

            return (int)loadPluginTask.Result;
        }
        //catch (ArgumentOutOfRangeException outOfRangeEx)
        //{
        //    Log(hostNotifier, outOfRangeEx.ToString());
        //    throw;
        //}
        catch (Exception e)
        {
            Log(hostNotifier, e.ToString());
        }
        finally
        {
            hostNotifier?.Dispose();
        }

        return (int)PluginInitializationState.Failed;
    }

    /// <summary>
    /// Find the entry point of the plugin module, initialize it, and execute its Run method.
    /// </summary>
    /// <param name="assembly">The plugin assembly containing the entry point.</param>
    /// <param name="paramArray">The parameters passed to the plugin Run method.</param>
    /// <param name="hostNotifier">Used to notify the host about the state of the plugin initialization.</param>
    private static async Task<PluginInitializationState> LoadPlugin(Assembly assembly, object[] paramArray, string className, string methodName, NotificationHelper hostNotifier)
    {
        //var entryPoint = FindEntryPoint(assembly);
        var entryPoint = assembly.GetType(className, false, false);
        if (entryPoint is null)
        {
            LogAndThrow(hostNotifier, new ArgumentException($"Assembly {assembly.FullName} doesn't contain the {className} type."));
        }

        var runMethod = entryPoint.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (runMethod is null)
        {
            LogAndThrow(hostNotifier, new MissingMethodException($"Failed to find the 'Run' function with {paramArray.Length} parameter(s) in {assembly.FullName}."));
        }

        _ = hostNotifier.Log("Found entry point, initializing plugin class.");

        var instance = Activator.CreateInstance(entryPoint, paramArray);
        if (instance is null)
        {
            LogAndThrow(hostNotifier, new MissingMethodException($"Failed to find the constructor {entryPoint.Name} in {assembly.FullName}"));
        }

        _ = hostNotifier.Log("Plugin successfully initialized. Executing the plugin entry point.");

        if (await hostNotifier.SendInjectionComplete(Environment.ProcessId))
        {
            try
            {
                // Execute the plugin 'Run' entry point.
                runMethod?.Invoke(instance, BindingFlags.Public | BindingFlags.Instance | BindingFlags.ExactBinding | BindingFlags.InvokeMethod, null, paramArray, null);
            }
            catch
            {
            }
            return PluginInitializationState.Initialized;
        }
        return PluginInitializationState.Failed;
    }

    /// <summary>
    /// Log a message.
    /// </summary>
    /// <param name="message">The information to log.</param>
    private static void Log(NotificationHelper? notifier, string message)
    {
        _ = notifier?.Log(message, LogLevel.Error);
    }

    /// <summary>
    /// Send a exception message to the host and then throw the exception in the current application.
    /// </summary>
    /// <param name="notifier">Communication helper to send messages to the host application.</param>
    /// <param name="e">The exception that occurred.</param>
    private static void LogAndThrow(NotificationHelper? notifier, Exception e)
    {
        Log(notifier, e.Message);
        throw e;
    }
}
