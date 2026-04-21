namespace MeetingAI.Services;

public static class LoggerService
{
    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MeetingAI", "logs");
        
    private static readonly object _lock = new();

    static LoggerService()
    {
        try { Directory.CreateDirectory(_logPath); }
        catch { }
    }

    public static void Info(string message, Exception? ex = null) => WriteLog("INFO", message, ex);
    public static void Warning(string message, Exception? ex = null) => WriteLog("WARN", message, ex);
    public static void Error(string message, Exception? ex = null) => WriteLog("ERROR", message, ex);
    public static void Debug(string message, Exception? ex = null) => WriteLog("DEBUG", message, ex);

    private static void WriteLog(string level, string message, Exception? ex)
    {
        try
        {
            lock (_lock)
            {
                var logFile = Path.Combine(_logPath, "MeetingAI_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = "[" + timestamp + "] [" + level + "] " + message;
                
                if (ex != null)
                {
                    logEntry += "\nException: " + ex.GetType().Name + ": " + ex.Message + "\nStackTrace: " + ex.StackTrace;
                }

                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
        }
        catch { }
    }
}