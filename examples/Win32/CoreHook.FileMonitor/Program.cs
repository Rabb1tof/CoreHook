using CoreHook.BinaryInjection;
using CoreHook.Extensions;
using CoreHook.FileMonitor.Hook;
using CoreHook.FileMonitor.Uwp;
using CoreHook.HookDefinition;
using CoreHook.IPC.Messages;
using CoreHook.IPC.NamedPipes;
using CoreHook.IPC.Platform;

using Microsoft.Extensions.Logging;

using Spectre.Console;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

using Console = Spectre.Console.AnsiConsole;

namespace CoreHook.FileMonitor;

class Program
{
    private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(conf => conf.SingleLine = true));
    private static readonly ILogger logger = loggerFactory.CreateLogger<Program>();

    /// <summary>
    /// The library to be injected into the target process and executed using the EntryPoint's 'Run' Method.
    /// </summary>
    private const string HOOK_LIB_NAME = "CoreHook.FileMonitor.Hook.dll";

    private const string HOOK_LIB_NAME_UWP = "CoreHook.Uwp.FileMonitor.Hook.dll";

    /// <summary>
    /// The name of the communication pipe that will be used for this program
    /// </summary>
    private const string PIPE_NAME_BASE = "FileMonitorHookPipe_";

    //public IntPtr CreateFile_Hooked(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile)
    //{
    //    return IntPtr.Zero;
    //}



    //private Delegate CreateDelegate(MethodInfo hookMethod)
    //{
    //    var parameters = hookMethod.GetParameters().Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name)).ToArray();//.Append(hookMethod.ReturnType).ToArray();
    //    var call = Expression.Call(Expression.Constant(this), hookMethod, parameters);
    //    var deleg = Expression.Lambda(call, parameters).Compile();

    //    return deleg;
    //}

    private static void Main(string[] args)
    {
        //LocalHook.Create("kernel32.dll", "CreateFileW", new Program().CreateDelegate(typeof(Program).GetMethod("CreateFile_Hooked", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)), new EntryPoint(""));

        Process? process = null;
        string targetProgram = string.Empty;

        // Get the process to hook by file path for launching or process id for loading into.
        while ((args.Length != 1) || !ParseProcessId(args[0], out process) || !FindOnPath(args[0]))
        {
            if (process is not null)
            {
                break;
            }
            if (args.Length != 1 || !FindOnPath(args[0]))
            {
                Console.WriteLine();
                Console.WriteLine("Usage: FileMonitor %PID%");
                Console.WriteLine("   or: FileMonitor PathToExecutable");
                Console.WriteLine();

                args = new[] { Console.Prompt(new TextPrompt<string>("Please enter a process Id or path to executable").DefaultValue("c:\\windows\\notepad.exe")) };

                if (string.IsNullOrEmpty(args[0]))
                {
                    return;
                }
            }
            else
            {
                targetProgram = args[0];
                break;
            }
        }

        var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        // Start process
        if (!string.IsNullOrWhiteSpace(targetProgram))
        {
            logger.LogInformation("Starting program and letting it start...");
            process = Process.Start(targetProgram);

            Thread.Sleep(1000);
        }

        if (process is null)
        {
            throw new InvalidOperationException($"Failed to start or retrieve the executable.");
        }

        List<Process>? hookTargets = new List<Process> { process };
        var children = process.GetChildProcesses();
        if (children.Any())
        {
            Console.WriteLine("Process has child process(es):");

            var processesTable = new Table();
            processesTable.AddColumn("ID");
            processesTable.AddColumn("Name");
            children.ForEach(child => processesTable.AddRow(child.Id.ToString(), child.ProcessName));
            Console.Write(processesTable);

            if (Console.Confirm("Would you like to hook to those as well?"))
            {
                hookTargets.AddRange(children);
            }
        }

        process.EnableRaisingEvents = true;
        process.Exited += (o, e) => { logger.LogInformation("Process has been closed. Exiting."); Environment.Exit(0); };

        foreach (var target in hookTargets)
        {
            string pipeName = PIPE_NAME_BASE + target.Id;

            var isUwp = target.IsPackagedApp(out string pname);
            if (isUwp)
            {
                logger.LogInformation("Granting access to all hook libraries and files.");

                // Grant read+execute permissions on the binary files we are injecting into the UWP application.
                UwpSecurityHelper.GrantAllAppPackagesAccessToDir(currentDir);
            }

            logger.LogInformation("Using process {processId} ({processName}) [{64or86} / {uwp}]", target.Id, target.ProcessName, target.Is64Bit() ? "x64" : "x86", isUwp ? $"UWP ({pname})" : "Win32");

            // Start the RPC server for handling requests from the hooked program.
            // TODO: should we dispose that one?
            var server = new NamedPipeServer(pipeName, isUwp ? new UwpPipePlatform() : DefaultPipePlatform.Instance, HandleRequest, logger);

            logger.LogInformation("Now listening on {pipeName}.", pipeName);

            string injectionLibrary = Path.Combine(currentDir, isUwp ? HOOK_LIB_NAME_UWP : HOOK_LIB_NAME);
            string injectedType = isUwp ? typeof(CoreHook.Uwp.FileMonitor.Hook.EntryPoint).FullName! : typeof(CoreHook.FileMonitor.Hook.EntryPoint).FullName!;

            // Inject FileMonitor dll into process using default pipe platform
            if (!target.AttachHook(injectionLibrary, injectedType, loggerFactory, isUwp ? new UwpPipePlatform() : DefaultPipePlatform.Instance, pipeName))
            {
                logger.LogInformation("Unable to inject into {processName} ({processId}).", target.ProcessName, target.Id);
                return;
            }
        }

        logger.LogInformation("Injection successful.");
        logger.LogInformation("Waiting for messages... (press Enter to quit)");
        System.Console.ReadLine();
    }

    /// <summary>
    /// Get an existing process ID by value or by name.
    /// </summary>
    /// <param name="targetProgram">The ID or name of a process to lookup.</param>
    /// <param name="processId">The ID of the process if found.</param>
    /// <returns>True if there is an existing process with the specified ID or name.</returns>
    private static bool ParseProcessId(string targetProgram, out Process? process)
    {
        if (int.TryParse(targetProgram, out var processId))
        {
            process = Process.GetProcessById(processId);
        }
        else
        {
            process = Process.GetProcessesByName(targetProgram).FirstOrDefault();
        }

        return process is not null;

    }

    /// <summary>
    /// Check if an application exists on the path.
    /// </summary>
    /// <param name="targetProgram">The program name, such as "notepad.exe".</param>
    /// <returns>True of the program is found on the path.</returns>
    private static bool FindOnPath(string targetProgram)
    {
        // File is in current dir or path was fully specified
        if (File.Exists(targetProgram))
        {
            return true;
        }

        // File wasn't found and path wasn't absolute: stop here
        if (Path.IsPathRooted(targetProgram))
        {
            return false;
        }

        // Or check in the configured paths
        var path = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path.Split(";").Any(pathDir => File.Exists(Path.Combine(pathDir, targetProgram)));
        }

        return false;
    }

    private static void HandleRequest(INamedPipe _, CustomMessage obj)
    {
        if (obj is CreateFileMessage message)
        {
            logger.LogInformation("{datetime:dd/MM/yyyy HH:mm:ss.fff} - Called CreateFile on {filename} (Access: {accessmode} / Share: {sharemode} / Mode: {mode})", message.DateTime, message.FileName, message.DesiredAccess, message.ShareMode, message.Mode);
        }
    }

}