using CoreHook.IPC.Messages;

namespace CoreHook.FileMonitor.Hook;
public record CreateFileMessage(string[] Queue) : CustomMessage;