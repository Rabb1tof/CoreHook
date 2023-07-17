using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CoreHook.BinaryInjection;

internal class ModulesPathHelper
{
    /// <summary>
    /// The name of the .NET Core hosting module for 64-bit processes.
    /// </summary>
    private const string CoreHostModule64 = "x64\\CoreHook.NativeHost64.dll";

    /// <summary>
    /// The name of the .NET Core hosting module for 32-bit processes.
    /// </summary>
    private const string CoreHostModule32 = "x86\\CoreHook.NativeHost32.dll";

    private static string _applicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    /// <summary>
    /// Determine if the current application is a self-contained application.
    /// </summary>
    /// <param name="applicationBase">The application base directory.</param>
    /// <returns>True if the coreclr module exists in the application base.</returns>
    private static bool IsPublishedApplication()
    { 
        return File.Exists(Path.Combine(_applicationBase, "coreclr.dll"));
    }

    /// <summary>
    /// Determine if the application has a local CoreCLR runtime configuration file.
    /// </summary>
    /// <param name="applicationBase">The application base directory.</param>
    /// <returns>True if there the directory contains a runtime configuration file.</returns>
    private static bool HasLocalRuntimeConfiguration(string libraryToLoad)
    {
        // The configuration file should be named 'CoreHook.CoreLoad.runtimeconfig.json'
        return File.Exists(Path.Combine(_applicationBase, libraryToLoad.Replace("dll", "runtimeconfig.json")));
    }

    /// <summary>
    /// Determine if the path CoreCLR runtime configuration file.
    /// </summary>
    /// <param name="configurationBase">Directory that should contain a CoreCLR runtime configuration file.</param>
    /// <returns>True if there the directory contains a runtime configuration file.</returns>
    private static bool DoesDirectoryContainRuntimeConfiguration(string configurationBase)
    {
        // The configuration file should be named 'dotnet.runtimeconfig.json'
        return File.Exists(Path.Combine(configurationBase, "dotnet.runtimeconfig.json"));
    }

    /// <summary>
    /// Get the directory path of the .NET Core runtime configuration file.
    /// </summary>
    /// <param name="is64BitProcess">Value to determine which native modules path to look for.</param>
    /// <param name="libraryToLoad"></param>
    /// <param name="coreRootPath">Path to the directory containing the CoreCLR runtime configuration.</param>
    /// <returns>Whether the CoreCLR path was found or not.</returns>
    private static bool GetCoreClrRootPath(string libraryToLoad, bool is64BitProcess, out string coreRootPath)
    {
        // Check if we are using a published application or a local
        // runtime configuration file, in which case we don't need
        // the paths from the environment variables.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && (IsPublishedApplication() || HasLocalRuntimeConfiguration(libraryToLoad)))
        {
            // Set the directory for finding dependencies to the application base directory.
            coreRootPath = _applicationBase;
            return true;
        }

        // Path to the directory containing the CoreCLR runtime configuration file.
        if (RuntimeInformation.ProcessArchitecture is Architecture.Arm or Architecture.Arm64)
        {
            coreRootPath = _applicationBase;
        }
        else
        {
            coreRootPath = is64BitProcess ? Environment.GetEnvironmentVariable("CORE_ROOT_64") : Environment.GetEnvironmentVariable("CORE_ROOT_32");
        }

        if (string.IsNullOrWhiteSpace(coreRootPath) || !DoesDirectoryContainRuntimeConfiguration(coreRootPath))
        {
            Console.WriteLine($"CoreCLR configuration was not found for ${(is64BitProcess ? 64 : 32)}-bit processes. Either use a self contained app or check the CORE_ROOT_${(is64BitProcess ? 64 : 32)} environment variable.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Retrieve the required paths for initializing the CoreCLR and executing .NET assemblies in an unmanaged process.
    /// </summary>
    /// <param name="is64BitProcess">Flag for determining which native modules to load into the target process</param>
    /// <param name="nativeModulesConfig">Configuration class containing paths to the native modules used by CoreHook.</param>
    /// <returns>Returns whether all required paths and modules have been found.</returns>
    public static (string coreRootPath, string coreHostPath, string nethostLibPath) GetCoreClrPaths(bool is64BitProcess, string libraryToLoad)
    {
        if (!GetCoreClrRootPath(libraryToLoad, is64BitProcess, out string coreRootPath))
        {
            throw new InvalidOperationException("Core CLR Root path could not be determined.");
        }

        // Module that initializes the .NET Core runtime and executes .NET assemblies
        var nativeHostPath = Path.Combine(_applicationBase, is64BitProcess ? CoreHostModule64 : CoreHostModule32);
        if (!File.Exists(nativeHostPath))
        {
            Console.WriteLine($"Cannot find file {nativeHostPath}");
            throw new FileNotFoundException(nativeHostPath);
        }

        var nethostLibPath = Path.Combine(_applicationBase, $"{(is64BitProcess ? "x64" : "x86")}\\nethost.dll");

        return (coreRootPath, nativeHostPath, nethostLibPath);
    }
}
