using System.Runtime.InteropServices;

namespace CoreHook.BinaryInjection.NativeDTO;

/// <summary>
/// Managed structure reflecting the core_host_arguments unmanaged one to pass arguments used when injecting the CLR host
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public readonly struct NetHostStartArguments
{
    /// <summary>
    /// Library that resolves dependencies and passes arguments to
    /// the .NET payload Assembly.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    private readonly string _clrBootstrapLibrary;

    /// <summary>
    /// Directory path which contains the folder with a `dotnet.runtimeconfig.json`
    /// containing properties for initializing CoreCLR.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    private readonly string _clrRootPath;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    private readonly string _injectionPipeName;

    /// <summary>
    /// Constructor to initialize a new .NET host start arguments instance
    /// </summary>
    /// <param name="clrBootstrapLibrary">See <see cref="_clrBootstrapLibrary"/></param>
    /// <param name="clrRootPath">See <see cref="_clrRootPath"/></param>
    /// <param name="verboseLog">See <see cref="VerboseLog"/></param>
    /// <param name="injectionPipeName"></param>
    public NetHostStartArguments(string clrBootstrapLibrary, string clrRootPath, string injectionPipeName)
    {
        _clrBootstrapLibrary = clrBootstrapLibrary;
        _clrRootPath = clrRootPath;
        _injectionPipeName = injectionPipeName;
    }
}