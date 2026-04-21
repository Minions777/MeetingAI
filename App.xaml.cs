using System.Windows;

namespace MeetingAI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"发生未处理的异常: {ex?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        };
    }
}