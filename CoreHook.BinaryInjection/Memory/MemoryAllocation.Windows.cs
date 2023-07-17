using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Windows.Win32.System.Memory;

namespace CoreHook.BinaryInjection.Memory;

public partial class MemoryAllocation
{
    private unsafe IntPtr Allocate(int size, PAGE_PROTECTION_FLAGS protection)
    //,MemoryAllocationType allocation //VIRTUAL_ALLOCATION_TYPE allocation = VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE)
    {
        IntPtr allocationAddress = (IntPtr)NativeMethods.VirtualAllocEx(_processHandle, IntPtr.Zero.ToPointer(), new UIntPtr((uint)size), VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE, protection);

        if (allocationAddress == IntPtr.Zero)
        {
            throw new Win32Exception($"Failed to allocated a memory region of {size} bytes.");
        }

        return allocationAddress;
    }

    public unsafe int WriteBytes(byte[] byteArray)
    {
        var bytesWritten = new nuint();

        if (!NativeMethods.WriteProcessMemory(_processHandle, (void*)Address, (void*)Marshal.UnsafeAddrOfPinnedArrayElement(byteArray, 0), (nuint)byteArray.Length, &bytesWritten))
        {
            throw new Win32Exception("Failed to write to process memory");
        }

        if (byteArray.Length != (int)bytesWritten)
        {
            throw new Win32Exception($"Failed to write all data to process ({bytesWritten} != {byteArray.Length}).");
        }

        return (int)bytesWritten;
    }

    private unsafe void Free()
    {
        if (!NativeMethods.VirtualFreeEx(_processHandle, Address.ToPointer(), new UIntPtr(0), VIRTUAL_FREE_TYPE.MEM_RELEASE))
        {
            throw new Win32Exception($"Failed to free the memory region at {Address.ToInt64():X16}.");
        }
    }
}
