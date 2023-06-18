using CoreHook.EntryPoint;

using System;

namespace CoreHook.Tests.SimpleParameterTest;

public class EntryPoint : IEntryPoint
{
    public EntryPoint(string _) { }

    public void Run(string message) => Console.WriteLine(message);
}
