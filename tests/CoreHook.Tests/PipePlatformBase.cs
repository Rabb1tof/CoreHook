using System.IO.Pipes;

using CoreHook.IPC.Platform;

namespace CoreHook.Tests;

internal class PipePlatformBase : IPipePlatform
{
    public static PipePlatformBase Instance { get; } = new PipePlatformBase();

    public NamedPipeServerStream CreatePipeByName(string pipeName, string serverName)
    {
        return new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            65536,
            65536
        );
    }
}
