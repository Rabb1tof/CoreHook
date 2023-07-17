using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace CoreHook.Native;

internal static partial class NativeApi64
{
    private const string DLL_NAME = "x64\\corehook64.dll";

    [LibraryImport(DLL_NAME, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial string RtlGetLastErrorStringCopy();

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int RtlGetLastError();

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial void DetourUninstallAllHooks();

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourInstallHook(nint entryPoint, nint hookProcedure, nint callback, nint handle);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourUninstallHook(nint refHandle);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourWaitForPendingRemovals();

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourSetInclusiveACL([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] threadIdList, int threadCount, nint handle);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourSetExclusiveACL([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] threadIdList, int threadCount, nint handle);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourSetGlobalInclusiveACL([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] threadIdList, int threadCount);


    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourSetGlobalExclusiveACL([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] threadIdList, int threadCount);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourIsThreadIntercepted(nint handle, int threadId, [MarshalAs(UnmanagedType.Bool)] out bool result);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourBarrierGetCallback(out nint returnValue);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourBarrierGetReturnAddress(out nint returnValue);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourBarrierGetAddressOfReturnAddress(out nint returnValue);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourBarrierCallStackTrace(nint returnValue, long maxCount, out long outMaxCount);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourBarrierBeginStackTrace(out nint backup);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourBarrierEndStackTrace(nint backup);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourBarrierGetCallingModule(out nint returnValue);

    [LibraryImport(DLL_NAME)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial int DetourGetHookBypassAddress(nint handle, out nint address);

    [LibraryImport(DLL_NAME, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DetourCreateProcessWithDllExW(string lpApplicationName,
        string lpCommandLine,
        nint lpProcessAttributes,
        nint lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint dwCreationFlags,
        nint lpEnvironment,
        string lpCurrentDirectory,
        nint lpStartupInfo,
        nint lpProcessInformation,
        string lpDllName,
        nint pfCreateProcessW);

    [LibraryImport(DLL_NAME, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DetourCreateProcessWithDllExA(string lpApplicationName,
        string lpCommandLine,
        nint lpProcessAttributes,
        nint lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint dwCreationFlags,
        nint lpEnvironment,
        string lpCurrentDirectory,
        nint lpStartupInfo,
        nint lpProcessInformation,
        string lpDllName,
        nint pfCreateProcessW);

    [LibraryImport(DLL_NAME, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DetourCreateProcessWithDllsExW(string lpApplicationName,
          string lpCommandLine,
          nint lpProcessAttributes,
          nint lpThreadAttributes,
          [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
          uint dwCreationFlags,
          nint lpEnvironment,
          string lpCurrentDirectory,
          nint lpStartupInfo,
          nint lpProcessInformation,
          uint nDlls,
          nint rlpDlls,
          nint pfCreateProcessW);

    [LibraryImport(DLL_NAME, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DetourCreateProcessWithDllsExA(string lpApplicationName,
        string lpCommandLine,
        nint lpProcessAttributes,
        nint lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint dwCreationFlags,
        nint lpEnvironment,
        string lpCurrentDirectory,
        nint lpStartupInfo,
        nint lpProcessInformation,
        uint nDlls,
        nint rlpDlls,
        nint pfCreateProcessW);

    [LibraryImport(DLL_NAME, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
    public static partial nint DetourFindFunction(string lpModule, string lpFunction);
}
