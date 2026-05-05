using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using MeetingAI.Shared.Logging;
using MeetingAI.Shared.Configuration;
using MeetingAI.Core.Services;
using MeetingAI.Client.ViewModels;

namespace MeetingAI.Client;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        // Initialize logging
        LoggerService.Initialize();
        
        // Setup DI
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
        
        // Global exception handling
        SetupExceptionHandling();
        
        LoggerService.Info("MeetingAI v2 启动");
        
        base.OnStartup(e);
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddMeetingAICore();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
    
    private void SetupExceptionHandling()
    {
        DispatcherUnhandledException += (sender, e) =>
        {
            LoggerService.Fatal("UI 线程未处理异常", e.Exception);
            MessageBox.Show(
                $"程序遇到未预期的错误：\n\n{e.Exception.Message}\n\n详细信息已记录到日志文件。",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        };
        
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                LoggerService.Fatal("非 UI 线程未处理异常", ex);
        };
        
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            LoggerService.Error("任务未观察异常", e.Exception);
            e.SetObserved();
        };
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        LoggerService.Shutdown();
        base.OnExit(e);
    }
}
