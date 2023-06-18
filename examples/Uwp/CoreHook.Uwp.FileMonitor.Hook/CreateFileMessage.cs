using CoreHook.IPC.Messages;

namespace CoreHook.Uwp.FileMonitor.Hook;
public record CreateFileMessage(string[] Queue) : CustomMessage;