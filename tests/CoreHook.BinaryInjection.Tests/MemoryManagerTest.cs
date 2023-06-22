using CoreHook.BinaryInjection.Memory;
using CoreHook.BinaryInjection.RemoteInjection;

using System.Diagnostics;

namespace CoreHook.BinaryInjection.Tests;

public class MemoryManagerTest
{
    [Fact]
    public void ShouldAllocateMemoryInProcess()
    {
        const int memoryAllocationSize = 0x400;

        using (var manager = new MemoryManager(new ManagedProcess(Process.GetCurrentProcess()).SafeHandle))
        {
            var allocation = manager.Allocate(memoryAllocationSize, MemoryProtectionType.ReadWrite);

            Assert.Equal(false, allocation.IsFree);
            Assert.Equal(memoryAllocationSize, allocation.Size);
        }
    }
}
