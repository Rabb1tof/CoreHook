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

    protected string _context;

    public string PipeName => _pipeName;

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
            throw new IOException($"{_context}: pipe connection is closed. Unable to write.");
        }

        try
        {
            message = message with { SenderId = _namedPipeId };

            await _writer.WriteLineAsync(message.Serialize());
            await _writer.FlushAsync();

            return true;
        }
        catch (IOException exc)
        {
            _logger.LogError(exc, "{context}: unable to write a {type} message to pipe.", _context, message.GetType().Name);
            throw;
        }
    }


    /// <inheritdoc />
    public async Task<CustomMessage?> Read()
    {
        if (!_pipeStream.IsConnected)
        {
            throw new IOException($"{_context}: pipe connection is closed. Unable to read.");
        }

        string? message = null;
        try
        {
            message = await _reader.ReadLineAsync();
            if (message is not null)
            {
                return CustomMessage.Deserialize(message);
            }

            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{context}: unable to deserialize.\r\nBody: {message}", _context, message);
            throw;
        }

    }

    public abstract void Connect();


    /// <inheritdoc />
    public virtual void Dispose()
    {
        Interlocked.Exchange(ref _pipeStream, null)?.Dispose();
    }

}
