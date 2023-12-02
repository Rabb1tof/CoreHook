using CoreHook.FileMonitor.Hook;
using CoreHook.Generator;
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

    // this is where we are intercepting all file accesses!
    [GenerateHook(TargetDllName = "kernel32.dll", TargetMethod = "CreateFile2", Description = "Logs any call to CreateFile2 (UWP).")]
    private IntPtr CreateFile2(string fileName, uint desiredAccess, uint shareMode, uint creationDisposition, IntPtr pCreateExParams)
    {
        var msg = new CreateFileMessage(fileName, DateTime.Now, (FileAccess)desiredAccess, (FileShare)shareMode, (FileMode)creationDisposition);

        _ = _pipe.TryWrite(msg);

        // call original API...
        return CreateFile2Native(fileName, desiredAccess, shareMode, creationDisposition, pCreateExParams);
    }

    // Intercepts all file accesses and stores the requested filenames to a Queue.
    [GenerateHook(TargetDllName = "kernel32.dll", TargetMethod = "CreateFileW", Description = "Logs any call to CreateFile.")]
    private IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile)
    {
        var msg = new CreateFileMessage(fileName, DateTime.Now, (FileAccess)desiredAccess, (FileShare)shareMode, (FileMode)creationDisposition);

        _ = _pipe.TryWrite(msg);

        // Call original API function.
        return CreateFileNative(fileName, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
    }
}
