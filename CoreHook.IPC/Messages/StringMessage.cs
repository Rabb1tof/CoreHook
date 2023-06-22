
namespace CoreHook.IPC.Messages;

/// <summary>
/// A representation of data containing information to be communicated between a client and server.
/// </summary>
public record StringMessage(string Message) : CustomMessage;
