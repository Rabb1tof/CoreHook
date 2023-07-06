using CoreHook.IPC.Messages;

using System;
using System.IO;

namespace CoreHook.FileMonitor.Hook;
public record CreateFileMessage(string FileName, DateTime DateTime, FileAccess DesiredAccess, FileShare ShareMode, FileMode Mode) : CustomMessage;