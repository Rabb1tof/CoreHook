using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace CoreHook.IPC.Messages;

/// <summary>
/// A message containing application status information.
/// </summary>
public record LogMessage(string Message, LogLevel Level = LogLevel.Information, string? Source = null) : CustomMessage;
