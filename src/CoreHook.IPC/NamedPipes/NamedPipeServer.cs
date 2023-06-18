using CoreHook.IPC.Messages;
using CoreHook.IPC.Platform;

using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Threading.Tasks;

namespace CoreHook.IPC.NamedPipes;

/// <summary>
/// Creates a pipe server and allows custom handling of messages from clients.
/// </summary>
public class NamedPipeServer : NamedPipeBase
{
    private readonly ILogger _logger;

    private readonly Action<INamedPipe, CustomMessage> _handleMessage;

    private readonly IPipePlatform _platform;

    private bool _connectionStopped;

    /// <summary>
    /// Initialize a new pipe server.
    /// </summary>
    /// <param name="pipeName">The name of the pipe server.</param>
    /// <param name="platform">Method for initializing a new pipe-based server.</param>
    /// <param name="handleMessage">Event handler called when receiving a new message from a client.</param>
    /// <param name="logger"></param>
    /// <returns>An instance of the new pipe server.</returns>
    public NamedPipeServer(string pipeName, IPipePlatform platform, Action<INamedPipe, CustomMessage> handleMessage, ILogger logger)
    {
        _handleMessage = handleMessage;
        _pipeName = pipeName;
        _platform = platform;
        _logger = logger;

        Connect();
    }

    private async void HandleMessages()
    {
        while (Stream?.IsConnected ?? false)
        {
            var message = await Read();

            // Exit the loop if the stream was closed after reading
            if (!Stream.IsConnected)
            {
                break;
            }

            if (message is null)
            {
                _logger.LogError("A null messagehas been received. Ignoring.");
                continue;
            }

            // Only process the message if it has not been sent by the current thread, as both the client and server can write/read messages.
            if (message.SenderId != _namedPipeId)
            {
                _logger.LogDebug("Message {MessageId} will be handled by {NamedPipeId}", message.MessageId, this._namedPipeId);
                _handleMessage?.Invoke(this, message);
            }
        }
    }

    /// <inheritdoc />
    public override async void Connect()
    {
        try
        {
            if (Stream is not null)
            {
                throw new InvalidOperationException("Pipe server already started");
            }

            var pipeStream = _platform.CreatePipeByName(_pipeName);

            Stream = pipeStream;

            await pipeStream.WaitForConnectionAsync();

            if (!_connectionStopped)
            {
                _ = Task.Run(() => HandleMessages());
            }
        }
        catch (IOException e)
        {
            _logger.LogError(e, "Pipe {_pipeName} broken with: ", _pipeName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled exception during server start");
        }
    }

    /// <inheritdoc />
    public new void Dispose()
    {
        _connectionStopped = true;

        base.Dispose();
    }
}
