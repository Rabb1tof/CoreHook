using CoreHook.IPC.Messages;

namespace CoreHook.BinaryInjection;

/// <summary>
/// A message containing information about an attempt to load
/// a plugin in a remote process. The message is sent from the
/// target application back to the host application that awaits it.
/// If the host application does not receive the message, then we assume
/// that the plugin loading has failed.
/// </summary>
public record InjectionCompleteMessage(int ProcessId, bool Completed) : CustomMessage;
