using CoreHook.BinaryInjection.NativeDTO;

using System.Runtime.InteropServices;
using System.Text.Json;

namespace CoreHook.BinaryInjection.NativeDTO;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public readonly struct ManagedFunctionArguments
{
    private readonly AssemblyDelegate _assemblyDelegate;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    private readonly string _pipeName = string.Empty;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
    private readonly string _payLoad = string.Empty;

    public ManagedFunctionArguments(AssemblyDelegate assemblyDelegate, string pipeName, object payLoad)
    {
        _pipeName = pipeName;
        _assemblyDelegate = assemblyDelegate;// ?? throw new ArgumentNullException(nameof(assemblyDelegate));
        if (payLoad is not null)
        {
            _payLoad = JsonSerializer.Serialize(payLoad, new JsonSerializerOptions() { IncludeFields = true });
        }
    }
}
