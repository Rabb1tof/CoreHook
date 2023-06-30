using System.Diagnostics;

namespace CoreHook.Tests;

public partial class ManagedProcessTest
{
    [Fact]
    public void ShouldOpenProcessHandleForCurrentProcess()
    {
        using var processHandle = new BinaryInjection.RemoteInjection.ManagedProcess(Process.GetCurrentProcess()).SafeHandle;
        Assert.NotEqual(true, processHandle.IsInvalid);
    }
}
