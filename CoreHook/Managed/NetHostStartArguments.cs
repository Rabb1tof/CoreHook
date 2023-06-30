using System.Runtime.InteropServices;

namespace CoreHook.Managed;

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
    public readonly string ClrBootstrapLibrary;

    /// <summary>
    /// Directory path which contains the folder with a `dotnet.runtimeconfig.json`
    /// containing properties for initializing CoreCLR.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public readonly string ClrRootPath;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    private readonly string InjectionPipeName;

    /// <summary>
    /// Constructor to initialize a new .NET host start arguments instance
    /// </summary>
    /// <param name="clrBootstrapLibrary">See <see cref="ClrBootstrapLibrary"/></param>
    /// <param name="clrRootPath">See <see cref="ClrRootPath"/></param>
    /// <param name="verboseLog">See <see cref="VerboseLog"/></param>
    /// <param name="injectionPipeName"></param>
    public NetHostStartArguments(string clrBootstrapLibrary, string clrRootPath, string injectionPipeName)
    {
        ClrBootstrapLibrary = clrBootstrapLibrary;
        ClrRootPath = clrRootPath;
        InjectionPipeName = injectionPipeName;
    }
}