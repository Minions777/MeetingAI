using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MeetingAI.Client.ViewModels;
using MeetingAI.Client.Views;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Services;
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

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddMeetingAICore();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<ProviderManagementViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MermaidRendererViewModel>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<MainWindow>();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        var mainViewModel = Services.GetService<MainViewModel>();
        mainViewModel?.Dispose();
        var providerManager = Services.GetService<ProviderManager>();
        providerManager?.Dispose();
        if (Services is IDisposable disposable)
            disposable.Dispose();
        LoggerService.Shutdown();
    }
}
