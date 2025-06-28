// SimpleFileLogger.cs
using System;
using System.Diagnostics;
using System.IO;

namespace TransparentTwitchChatWPF;

public static class SimpleFileLogger
{
    private static readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "debug_log.txt");
    private static readonly object _lock = new object();

    public static void Log(string message)
    {
#if DEBUG
        try
        {
            lock (_lock)
            {
                // Format: [Timestamp] [Process ID] Message
                string logLine = $"[{DateTime.Now:HH:mm:ss.fff}] [P:{Process.GetCurrentProcess().Id}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, logLine);
            }
        }
        catch
        {
            // Failsafe in case of logging errors
        }
#endif
    }

    public static void ClearLog()
    {
        try
        {
            if (File.Exists(LogFilePath))
            {
                File.Delete(LogFilePath);
            }
        }
        catch { }
    }
}