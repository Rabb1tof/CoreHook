using CoreHook.IPC.Messages;

using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace CoreHook.IPC.NamedPipes;
public abstract class NamedPipeBase : INamedPipe
{
    protected ILogger _logger;

    protected string _pipeName;

    protected string _namedPipeId = Guid.NewGuid().ToString();

    public virtual PipeStream? Stream
    {
        get => _pipeStream;
        protected set
        {
            _pipeStream = value;
            _reader = new StreamReader(_pipeStream);
            _writer = new StreamWriter(_pipeStream);
        }
    }

    private PipeStream? _pipeStream;

    private StreamReader? _reader;
    private StreamWriter? _writer;

    public async Task<bool> TryWrite(CustomMessage message)
    {
        if (!Stream.IsConnected)
        {
            throw new IOException("Pipe connection is closed. Unable to write.");
        }

        try
        {
            message = message with { SenderId = _namedPipeId };

            await _writer.WriteLineAsync(message.Serialize());
            await _writer.FlushAsync();

            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }


    /// <inheritdoc />
    public async Task<CustomMessage?> Read()
    {
        if (!_pipeStream.IsConnected)
        {
            throw new IOException("Pipe connection is closed. Unable to read.");
        }

        string? message = null;
        try
        {
            message = await _reader.ReadLineAsync();
            if (message is not null)
            {
                return CustomMessage.Deserialize(message);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to deserialize.\r\nBody: {message}", message);
            Console.WriteLine();
        }

        return null;
    }

    public abstract void Connect();


    /// <inheritdoc />
    public virtual void Dispose()
    {
        Interlocked.Exchange(ref _pipeStream, null)?.Dispose();
    }

}
