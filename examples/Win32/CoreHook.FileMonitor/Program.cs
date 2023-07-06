using CoreHook.BinaryInjection;
using CoreHook.Extensions;
using CoreHook.FileMonitor.Hook;
using CoreHook.FileMonitor.Uwp;
using CoreHook.IPC.Messages;
using CoreHook.IPC.NamedPipes;
using CoreHook.IPC.Platform;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using Spectre.Console;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;

using Console = Spectre.Console.AnsiConsole;

namespace CoreHook.FileMonitor;

class Program
{
    private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger logger = loggerFactory.CreateLogger<Program>();

    /// <summary>
    /// The library to be injected into the target process and executed using the EntryPoint's 'Run' Method.
    /// </summary>
    private const string HookLibraryName = "CoreHook.FileMonitor.Hook.dll";

    private const string HookLibraryNameUwp = "CoreHook.Uwp.FileMonitor.Hook.dll";

    /// <summary>
    /// The name of the communication pipe that will be used for this program
    /// </summary>
    private const string PipeName = "FileMonitorHookPipe";


    /// <summary>
    /// Security Identifier representing ALL_APPLICATION_PACKAGES permission.
    /// </summary>
    private static readonly SecurityIdentifier AllAppPackagesSid = new SecurityIdentifier("S-1-15-2-1");

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
            process = Process.Start(targetProgram);
        }

        if (process is null)
        {
            throw new InvalidOperationException($"Failed to start or retrieve the executable.");
        }

        var isUwp = process.IsPackagedApp(out string pname);
        if (isUwp)
        {
            logger.LogInformation("Granting access to all hook libraries and files.");

            // Grant read+execute permissions on the binary files we are injecting into the UWP application.
            GrantAllAppPackagesAccessToDir(currentDir);
        }

        logger.LogInformation($"Using process {process.Id} ({process.ProcessName}) [{(process.Is64Bit() ? "x64" : "x86")} / {(isUwp ? $"UWP ({pname})" : "Win32")}]");

        process.EnableRaisingEvents = true;
        process.Exited += (o, e) => { logger.LogInformation("Process has been closed. Exiting."); Environment.Exit(0); };

        // Start the RPC server for handling requests from the hooked program.
        using var server = new NamedPipeServer(PipeName, IPipePlatform.Default, HandleRequest, logger);
        logger.LogInformation($"Now listening on {PipeName}.");
        
        string injectionLibrary = Path.Combine(currentDir, isUwp ? HookLibraryNameUwp : HookLibraryName);
        //string injectionLibrary = Path.Combine(currentDir, HookLibraryName);

        // Inject FileMonitor dll into process using default pipe platform
        if (!process.AttachHook(injectionLibrary, loggerFactory, PipeName)) //isUwp ? new UwpPipePlatform() : DefaultPipePlatform.Instance, 
        {
            return;
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

    //TODO: move to a helper class
    /// <summary>
    /// Grant ALL_APPLICATION_PACKAGES permissions to binary
    /// and configuration files in <paramref name="directoryPath"/>.
    /// </summary>
    /// <param name="directoryPath">Directory containing application files.</param>
    private static void GrantAllAppPackagesAccessToDir(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        GrantAllAppPackagesAccessToFolder(directoryPath);

        foreach (var folder in Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories))
        {
            GrantAllAppPackagesAccessToFolder(folder);
        }

        foreach (var filePath in Directory.GetFiles(directoryPath, "*.json|*.dll|*.pdb", SearchOption.AllDirectories))
        {
            GrantAllAppPackagesAccessToFile(filePath);
        }
    }


    /// <summary>
    /// Grant ALL_APPLICATION_PACKAGES permissions to a directory at <paramref name="folderPath"/>.
    /// </summary>
    /// <param name="folderPath">The directory to be granted ALL_APPLICATION_PACKAGES permissions.</param>
    private static void GrantAllAppPackagesAccessToFolder(string folderPath)
    {
        try
        {
            var dirInfo = new DirectoryInfo(folderPath);

            DirectorySecurity acl = dirInfo.GetAccessControl(AccessControlSections.Access);
            acl.SetAccessRule(new FileSystemAccessRule(AllAppPackagesSid, FileSystemRights.ReadAndExecute, AccessControlType.Allow));

            dirInfo.SetAccessControl(acl);
        }
        catch
        {
        }
    }


    /// <summary>
    /// Grant ALL_APPLICATION_PACKAGES permissions to a file at <paramref name="fileName"/>.
    /// </summary>
    /// <param name="fileName">The file to be granted ALL_APPLICATION_PACKAGES permissions.</param>
    private static void GrantAllAppPackagesAccessToFile(string fileName)
    {
        try
        {
            var fileInfo = new FileInfo(fileName);

            FileSecurity acl = fileInfo.GetAccessControl();
            acl.SetAccessRule(new FileSystemAccessRule(AllAppPackagesSid, FileSystemRights.ReadAndExecute, AccessControlType.Allow));

            fileInfo.SetAccessControl(acl);
        }
        catch
        {
        }
    }
}