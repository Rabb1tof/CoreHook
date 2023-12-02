using CoreHook.Generator;
using CoreHook.HookDefinition;
using CoreHook.IPC.NamedPipes;

using System;
using System.IO;

namespace CoreHook.FileMonitor.Hook;

public partial class EntryPoint : HookBase
{
    private LocalHook _createFileHook;
    private NamedPipeClient _pipe;

    // The number of arguments in the constructor and Run method must be equal to the number passed during injection in the FileMonitor application.
    public EntryPoint(string pipeName) : base(pipeName) { }

    ////[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    ////[UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
    ////private static partial IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    // Intercepts all file accesses and stores the requested filenames to a Queue.
    [GenerateHook(TargetDllName = "kernel32.dll", TargetMethod = "CreateFileW", Description = "Logs any call to CreateFile.")]
    public IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile)
    {
        var msg = new CreateFileMessage(fileName, DateTime.Now, (FileAccess)desiredAccess, (FileShare)shareMode, (FileMode)creationDisposition);
        
        _ = _pipe.TryWrite(msg);

        // Call original API function.
        return CreateFileNative(fileName, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
    }
}
