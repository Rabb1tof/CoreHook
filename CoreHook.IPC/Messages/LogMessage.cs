using Microsoft.Extensions.Logging;

namespace CoreHook.IPC.Messages;

/// <summary>
/// A message containing application status information.
/// </summary>
public record LogMessage(string Message, LogLevel Level = LogLevel.Information) : CustomMessage;
