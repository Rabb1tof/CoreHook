using System.IO.Pipes;

namespace CoreHook.IPC.Platform;

public class DefaultPipePlatform : IPipePlatform
{
    public static IPipePlatform Instance { get; } = new DefaultPipePlatform();

    public NamedPipeServerStream CreatePipeByName(string pipeName, string serverName, int instances)
    {
        return new NamedPipeServerStream(pipeName, PipeDirection.InOut, instances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 65536, 65536);
    }
}
