using CoreHook.EntryPoint;
using CoreHook.HookDefinition;
using CoreHook.IPC.Messages;
using CoreHook.IPC.NamedPipes;

using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CoreHook.FileMonitor.Hook;

public partial class EntryPoint : IEntryPoint
{
    private LocalHook _createFileHook;
    private NamedPipeClient _pipe;

    // The number of arguments in the constructor and Run method must be equal to the number passed during injection in the FileMonitor application.
    public EntryPoint(string _) { }

    public void Run(string pipeName)
    {
        Task.Run(() =>
        {
            try
            {
                _pipe = new NamedPipeClient(pipeName, true);

                _ = _pipe.TryWrite(new LogMessage("Hook pipe is connected, creating hooks."));

                CreateHooks();

                _ = _pipe.TryWrite(new LogMessage("Success!"));
            }
            catch (Exception e)
            {
                _ = _pipe?.TryWrite(new LogMessage(e.Message, LogLevel.Error));
            }
        });
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
    delegate IntPtr CreateFileDelegate(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    //[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    //[UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
    //private static partial IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    // Intercepts all file accesses and stores the requested filenames to a Queue.
    private IntPtr CreateFile_Hooked(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile)
    {
        var msg = new CreateFileMessage(fileName, DateTime.Now, (FileAccess)desiredAccess, (FileShare)shareMode, (FileMode)creationDisposition);

        _ = _pipe.TryWrite(msg);

        // Call original API function.
        return CreateFile(fileName, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
    }

    protected void CreateHooks()
    {
        _createFileHook = LocalHook.Create("kernel32.dll", "CreateFileW", new CreateFileDelegate(CreateFile_Hooked), this);
        _ = _pipe.TryWrite(new LogMessage($"Success, mapped CreateFileW from 0x{_createFileHook.OriginalAddress:x} to 0x{_createFileHook.TargetAddress:x}."));
    }
}
