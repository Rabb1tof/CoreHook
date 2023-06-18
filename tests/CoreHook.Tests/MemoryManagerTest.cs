using System.Diagnostics;
using Xunit;
using CoreHook.BinaryInjection.RemoteInjection;

namespace CoreHook.Tests;

public class MemoryManagerTest
{
    [Fact]
    public void ShouldAllocateMemoryInProcess()
    {
        const int memoryAllocationSize = 0x400;

        using (BinaryInjection.Memory.MemoryManager manager = new BinaryInjection.Memory.MemoryManager(new ManagedProcess(Process.GetCurrentProcess()).SafeHandle))
        {
            var allocation = manager.Allocate(memoryAllocationSize, BinaryInjection.Memory.MemoryProtectionType.ReadWrite);

            Assert.Equal(false, allocation.IsFree);
            Assert.Equal(memoryAllocationSize, allocation.Size);
        }
    }
}
