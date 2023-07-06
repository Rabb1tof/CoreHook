using System.IO.Pipes;

namespace CoreHook.IPC.Platform;

public class DefaultPipePlatform : IPipePlatform
{
    public static IPipePlatform Instance { get; } = new DefaultPipePlatform();

    public NamedPipeServerStream CreatePipeByName(string pipeName, string serverName)
    {
        return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 65536, 65536);
    }
}
