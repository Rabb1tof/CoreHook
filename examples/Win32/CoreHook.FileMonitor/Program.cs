using CoreHook.BinaryInjection;
using CoreHook.Extensions;
using CoreHook.FileMonitor.Hook;
using CoreHook.IPC.Messages;
using CoreHook.IPC.NamedPipes;
using CoreHook.IPC.Platform;

using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CoreHook.FileMonitor;

class Program
{
    private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger logger = loggerFactory.CreateLogger<Program>();

    /// <summary>
    /// The library to be injected into the target process and executed using the EntryPoint's 'Run' Method.
    /// </summary>
    private const string HookLibraryName = "CoreHook.FileMonitor.Hook.dll";

    /// <summary>
    /// The name of the communication pipe that will be used for this program
    /// </summary>
    private const string PipeName = "FileMonitorHookPipe";

    private static void Main(string[] args)
    {
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
                Console.Write("Please enter a process Id or path to executable: ");

                args = new string[]
                {
                    Console.ReadLine()
                };

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

        string injectionLibrary = Path.Combine(currentDir, HookLibraryName);

        // Start process
        if (!string.IsNullOrWhiteSpace(targetProgram))
        {
            process = Process.Start(targetProgram);
            if (process is null)
            {
                throw new InvalidOperationException($"Failed to start the executable at {targetProgram}");
            }
        }

        logger.LogInformation($"Using process {process.Id} ({process.ProcessName}) [{(process.Is64Bit() ? "x64" : "x86")}]");

        // Start the RPC server for handling requests from the hooked program.
        using var server = new NamedPipeServer(PipeName, IPipePlatform.Default, HandleRequest, logger);
        logger.LogInformation($"Now listening on {PipeName}.");

        // Inject FileMonitor dll into process using default pipe platform
        if (!process.AttachHook(injectionLibrary, loggerFactory, PipeName))
        {
            return;
        }

        logger.LogInformation("Injection successful.");
        logger.LogInformation("Waiting for messages... (press Enter to quit)");
        Console.ReadLine();
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
            foreach (var f in message.Queue)
            {
                logger.LogInformation(f);
            }
        }
    }

}