using CoreHook.FileMonitor.Hook;
using CoreHook.HookDefinition;

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CoreHook.Uwp.FileMonitor.Hook;

public partial class EntryPoint : HookBase
{
    private LocalHook _createFileHook;
    
    // The number of arguments in the constructor and Run method
    // must be equal to the number passed during injection
    // in the FileMonitor application.
    public EntryPoint(string pipeName) : base(pipeName) { }

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
    delegate IntPtr CreateFile2Delegate(string fileName, uint desiredAccess, uint shareMode, uint creationDisposition, IntPtr pCreateExParams);

    //[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    //[UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
    //private static partial IntPtr CreateFile2(string fileName, uint desiredAccess, uint shareMode, uint creationDisposition, IntPtr pCreateExParams);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr CreateFile2(string fileName, uint desiredAccess, uint shareMode, uint creationDisposition, IntPtr pCreateExParams);


    // this is where we are intercepting all file accesses!
    [Hook(TargetDllName = "kernel32.dll", TargetMethod = nameof(CreateFile2), Description = "Logs any call to CreateFile2 (UWP).", DelegateType = typeof(CreateFile2Delegate))]
    private IntPtr CreateFile2_Hooked(string fileName, uint desiredAccess, uint shareMode, uint creationDisposition, IntPtr pCreateExParams)
    {
        var msg = new CreateFileMessage(fileName, DateTime.Now, (FileAccess)desiredAccess, (FileShare)shareMode, (FileMode)creationDisposition);

        _ = _pipe.TryWrite(msg);

        // call original API...
        return CreateFile2(fileName, desiredAccess, shareMode, creationDisposition, pCreateExParams);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
    delegate IntPtr CreateFileDelegate(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    //[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    //[UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
    //private static partial IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    // Intercepts all file accesses and stores the requested filenames to a Queue.
    [Hook(TargetDllName = "kernel32.dll", TargetMethod = "CreateFileW", Description = "Logs any call to CreateFile.", DelegateType = typeof(CreateFileDelegate))]
    private IntPtr CreateFile_Hooked(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile)
    {
        var msg = new CreateFileMessage(fileName, DateTime.Now, (FileAccess)desiredAccess, (FileShare)shareMode, (FileMode)creationDisposition);

        _ = _pipe.TryWrite(msg);

        // Call original API function.
        return CreateFile(fileName, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
    }
}
