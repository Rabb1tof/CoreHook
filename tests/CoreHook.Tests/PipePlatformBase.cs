using System.IO;
using System.IO.Pipes;

using CoreHook.IPC.Platform;

namespace CoreHook.Tests;

internal class PipePlatformBase : IPipePlatform
{
    public static PipePlatformBase Instance { get; } = new PipePlatformBase();

    public static string GetUniquePipeName() => Path.GetRandomFileName();

    public NamedPipeServerStream CreatePipeByName(string pipeName, string serverName, int instances)
    {
        return new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            instances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            65536,
            65536
        );
    }
}
