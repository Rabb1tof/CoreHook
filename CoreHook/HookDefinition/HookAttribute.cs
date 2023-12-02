using System;

namespace CoreHook.HookDefinition;

public class HookAttribute : Attribute
{
    public string TargetDllName { get; set; }

    public string TargetMethod { get; set; }

    public string Description { get; set; }

    public Type DelegateType { get; set; }

    public ulong TargetRelativeAddress { get; set; } = 0;
}