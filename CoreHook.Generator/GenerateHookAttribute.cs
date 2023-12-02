using System;
using System.Runtime.InteropServices;

namespace CoreHook.Generator;

[System.AttributeUsage(System.AttributeTargets.Method)]
public class GenerateHookAttribute : Attribute
{
    public string TargetDllName { get; set; }

    public string? TargetMethod { get; set; }

    public ulong? TargetRelativeAddress { get; set; }

    public string Description { get; set; }

    public enum Actions { Log, Count };

    public Actions Action { get; set; }

    public CallingConvention CallingConvention { get; set; } = CallingConvention.StdCall;

    public bool SetLastError { get; set; } = true;

    public CharSet CharSet { get; set; } = CharSet.Unicode;
}