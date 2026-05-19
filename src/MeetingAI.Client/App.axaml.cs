using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MeetingAI.Client.ViewModels;
using MeetingAI.Client.Views;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Services;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Helpers;
using MeetingAI.Shared.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingAI.Client;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        LoggerService.Initialize();

        // Register global unhandled exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
            desktop.Exit += OnExit;
        }

        LoggerService.Info("MeetingAI started");
        base.OnFrameworkInitializationCompleted();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LoggerService.Fatal($"Unhandled domain exception: {ex.Message}", ex);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LoggerService.Error($"Unobserved task exception: {e.Exception.Message}", e.Exception);
        e.SetObserved(); // Prevent app crash from unobserved task exceptions
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddMeetingAICore();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<ProviderManagementViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MermaidRendererViewModel>();
        services.AddTransient<SettingsWindow>();
        services.AddSingleton<MainWindow>();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        // Dispose all IDisposable singletons in reverse dependency order
        // MainViewModel first (depends on services), then services, then infrastructure
        foreach (var serviceType in new[]
        {
            typeof(MainViewModel),
            typeof(IPlatformHotkeyService),
            typeof(IRecordingService),
            typeof(IAudioCapture),
            typeof(ProviderManager),
            typeof(IConfigurationService),
            typeof(ISecureStorage),
        })
        {
            if (Services.GetService(serviceType) is IDisposable disposable)
                disposable.Dispose();
        }

        if (Services is IDisposable spDisposable)
            spDisposable.Dispose();

        LoggerService.Shutdown();
    }
}
