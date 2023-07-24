using System;

namespace CoreHook.Tests.SimpleParameterTest;

public class EntryPoint
{
    public EntryPoint(string _) { }

    public void Run(string message) => Console.WriteLine(message);
}
