using System.Reflection;
using System.Runtime.InteropServices;

namespace CoreHook.BinaryInjection.NativeDTO;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public readonly struct AssemblyDelegate
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    private readonly string _assemblyPath;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    private readonly string _typeNameQualified;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    private readonly string _methodName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    private readonly string? _delegateTypeName;

    public AssemblyDelegate(string assemblyName, string typeName, string methodName) : this()
    {
        var assembly = Assembly.Load(assemblyName);
        _assemblyPath = assembly.Location;
        _typeNameQualified = Assembly.CreateQualifiedName(assemblyName, typeName);
        _methodName = methodName;
    }

    public AssemblyDelegate(string assemblyName, string typeName, string methodName, string? delegateTypeName) : this(assemblyName, typeName, methodName)
    {
        _delegateTypeName = delegateTypeName;
    }
}
