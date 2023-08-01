using CoreHook.IPC.Messages;
using CoreHook.IPC.NamedPipes;
using CoreHook.IPC.Platform;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;

namespace CoreHook.BinaryInjection;

/// <summary>
/// Handles notifications from a target process related to the CoreHook bootstrapping stage,
/// which is handled by the CoreLoad module. The host process should either receive 
/// a message about that the CoreHook plugin was successfully loaded or throw an
/// exception after a certain amount of time when no message has been received.
/// </summary>
public class InjectionHelper : IDisposable
{
    private readonly SortedList<int, InjectionState> ProcessList = new SortedList<int, InjectionState>();
    private readonly ILogger _logger;
    private readonly NamedPipeServer _server;

    public InjectionHelper(string namedPipeName, IPipePlatform pipePlatform, ILogger logger)
    {
        _logger = logger;
        _server = new NamedPipeServer(namedPipeName, pipePlatform, HandleMessage, logger);
    }

    /// <summary>
    /// Process a message received by the server.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="channel">The server communication channel.</param>
    private void HandleMessage(INamedPipe _, CustomMessage message)
    {
        if (message is LogMessage logMessageData)
        {
            _logger.Log(logMessageData.Level, "[{source}] {message}", logMessageData.Source ?? "-", logMessageData.Message);
        }
        else if (message is InjectionCompleteMessage messageData)
        {
            if (messageData.Completed)
            {
                InjectionCompleted(messageData.ProcessId);
            }
            else
            {
                throw new InjectionLoadException($"Injection into process {messageData.ProcessId} failed.");
            }
        }
        else
        {
            throw new InvalidOperationException($"Message type {message.GetType().Name} is not supported");
        }
    }

    /// <summary>
    /// Start the process for awaiting a notification from a remote process.
    /// </summary>
    /// <param name="targetProcessId">The remote process ID we expect the notification from.</param>
    public void BeginInjection(int targetProcessId)
    {
        InjectionState? state;

        lock (ProcessList)
        {
            if (!ProcessList.TryGetValue(targetProcessId, out state))
            {
                state = new InjectionState();

                ProcessList.Add(targetProcessId, state);
            }
        }

        state.ThreadLock.WaitOne();
        state.Error = null;
        state.Completion.Reset();

        lock (ProcessList)
        {
            if (!ProcessList.ContainsKey(targetProcessId))
            {
                ProcessList.Add(targetProcessId, state);
            }
        }
    }

    /// <summary>
    /// Remove a process ID from a list we use to wait for remote notifications.
    /// </summary>
    /// <param name="targetProcessId">The remote process ID we expect the notification from.</param>
    public void EndInjection(int targetProcessId)
    {
        lock (ProcessList)
        {
            ProcessList[targetProcessId].ThreadLock.ReleaseMutex();

            ProcessList.Remove(targetProcessId);
        }
    }

    /// <summary>
    /// Complete the wait for a notification.
    /// </summary>
    /// <param name="remoteProcessId">The remote process ID we expect the notification from.</param>
    public void InjectionCompleted(int remoteProcessId)
    {
        InjectionState state;

        lock (ProcessList)
        {
            state = ProcessList[remoteProcessId];
        }

        state.Error = null;
        state.Completion.Set();
    }

    /// <summary>
    /// Block the current thread and wait to until we receive a signal from a remote process to continue.
    /// </summary>
    /// <param name="targetProcessId">The remote process ID we expect the notification from.</param>
    /// <param name="timeOutMilliseconds">The time in milliseconds to wait for the notification message.</param>
    public void WaitForInjection(int targetProcessId, int timeOutMilliseconds = 20000)
    {
        InjectionState state;

        lock (ProcessList)
        {
            state = ProcessList[targetProcessId];
        }

        if (!state.Completion.WaitOne(timeOutMilliseconds, false))
        {
            HandleException(targetProcessId, new TimeoutException("Unable to wait for plugin injection to complete."));
        }

        if (state.Error is not null)
        {
            throw state.Error;
        }
    }

    /// <summary>
    /// Handle any errors that occur during the wait for the injection complete notification.
    /// If an error occurs, then save it and complete the notification wait so the host program
    /// can continue the execution of the thread being blocked.
    /// </summary>
    /// <param name="remoteProcessId">The process ID we expect a notification from.</param>
    /// <param name="e">The error that occured during the wait.</param>
    private void HandleException(int remoteProcessId, Exception e)
    {
        InjectionState state;

        lock (ProcessList)
        {
            state = ProcessList[remoteProcessId];
        }

        state.Error = e;
        state.Completion.Set();
    }

    public void Dispose()
    {
        _server.Dispose();
    }
}
