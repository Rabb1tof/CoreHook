using CoreHook.BinaryInjection;
using CoreHook.IPC.Messages;
using CoreHook.IPC.NamedPipes;

using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreHook.Loader;

internal class NotificationHelper : IDisposable
{
    private readonly INamedPipe _pipe;
    private readonly ILogger _logger;

    internal NotificationHelper(string pipeName, ILogger logger)
    {
        _pipe = new NamedPipeClient(pipeName, logger, true);
        _logger = logger;
    }

    /// <summary>
    /// Notify the injecting process when injection has completed successfully
    /// and the plugin is about to be executed.
    /// </summary>
    /// <param name="processId">The process ID to send in the notification message.</param>
    /// <returns>True if the injection completion notification was sent successfully.</returns>
    internal async Task<bool> SendInjectionComplete(int processId)
    {
        return await _pipe.TryWrite(new InjectionCompleteMessage(processId, true));
    }

    internal async Task<bool> Log(string message, LogLevel level = LogLevel.Information)
    {
        return await _pipe.TryWrite(new LogMessage(message, level));
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing communication pipe '{pipeName}'.", _pipe.PipeName);
        _pipe?.Dispose();
    }
}
