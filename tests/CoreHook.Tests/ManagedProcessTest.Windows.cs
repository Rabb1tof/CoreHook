using CoreHook.BinaryInjection.RemoteInjection;

using System;
using System.Diagnostics;
using System.IO;

namespace CoreHook.Tests;

public partial class ManagedProcessTest
{
    [Fact]
    public void ShouldGetFunctionAddressForCurrentProcess()
    {
        string moduleFileName = Path.Combine(Environment.ExpandEnvironmentVariables("%Windir%"), "System32", "kernel32.dll");
        const string functionName = "LoadLibraryW";

        using var process = new ManagedProcess(Process.GetCurrentProcess());

        var functionAddress = process.GetProcAddress(moduleFileName, functionName);

        Assert.NotEqual(IntPtr.Zero, functionAddress);
    }
}
