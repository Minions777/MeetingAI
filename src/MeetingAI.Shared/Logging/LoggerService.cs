using Serilog;
using Serilog.Events;

namespace MeetingAI.Shared.Logging;

public static class LoggerService
{
    private static ILogger? _logger;

    public static void Initialize()
    {
        if (_logger != null) return;

        Directory.CreateDirectory(Constants.AppConstants.Paths.Logs);

        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("App", Constants.AppConstants.AppName)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine(Constants.AppConstants.Paths.Logs, "meetingai-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Info("LoggerService 初始化完成");
    }

    public static void Info(string message) => _logger?.Information(message);
    public static void Debug(string message) => _logger?.Debug(message);
    public static void Warning(string message) => _logger?.Warning(message);
    public static void Error(string message, Exception? ex = null) => _logger?.Error(ex, message);
    public static void Fatal(string message, Exception? ex = null) => _logger?.Fatal(ex, message);

    public static void Shutdown()
    {
        Info("应用程序关闭");
        Log.CloseAndFlush();
    }
}
