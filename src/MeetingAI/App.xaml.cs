using System.Windows;
using System.Windows.Threading;
using MeetingAI.Services;

namespace MeetingAI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SetupGlobalExceptionHandling();
        
        var configPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MeetingAI");
        System.IO.Directory.CreateDirectory(configPath);
        
        LoggerService.Info("应用程序启动");
    }

    private void SetupGlobalExceptionHandling()
    {
        DispatcherUnhandledException += (sender, e) =>
        {
            LoggerService.Error("UI 线程未处理异常", e.Exception);
            MessageBox.Show(
                "程序遇到未预期的错误：\n\n" + e.Exception.Message + "\n\n详细信息已记录到日志文件。",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex) LoggerService.Error("非 UI 线程未处理异常", ex);
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            LoggerService.Error("任务未观察异常", e.Exception);
            e.SetObserved();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LoggerService.Info("应用程序退出");
        base.OnExit(e);
    }
}