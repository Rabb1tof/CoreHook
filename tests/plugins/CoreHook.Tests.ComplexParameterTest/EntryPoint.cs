using System;

namespace CoreHook.Tests.ComplexParameterTest;

public class EntryPoint
{
    public EntryPoint(ComplexParameter _) { }

    public void Run(ComplexParameter complexParameter)
    {
        Console.WriteLine(complexParameter.Message);
        Console.WriteLine(complexParameter.HostProcessId);
    }
}
