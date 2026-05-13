using Microsoft.Extensions.DependencyInjection;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Core.Resilience;
using MeetingAI.Core.State;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Helpers;

namespace MeetingAI.Core.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddMeetingAICore(this IServiceCollection services)
    {
        // Platform-specific services
        services.AddPlatformServices();

        // Core services
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // State management
        services.AddSingleton<MeetingStateManager>();

        // AI Provider Factory
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();

        // Services
        services.AddSingleton<IRecordingService, RecordingService>();
        services.AddSingleton<ITranscriptionService, TranscriptionService>();
        services.AddSingleton<ISummaryService, SummaryService>();
        services.AddSingleton<MeetingHistoryService>();

        // Resilience — factory-based registration that resolves default provider at construction
        services.AddSingleton<IAIProviderWrapper>(sp =>
        {
            var factory = sp.GetRequiredService<IAIProviderFactory>();
            var configService = sp.GetRequiredService<IConfigurationService>();
            var settings = configService.Load();

            ProviderConfig? providerConfig = null;
            if (!string.IsNullOrEmpty(settings.DefaultProviderId))
            {
                providerConfig = settings.Providers.FirstOrDefault(p => p.Id == settings.DefaultProviderId && p.IsEnabled);
            }
            providerConfig ??= settings.Providers.FirstOrDefault(p => p.IsEnabled);

            if (providerConfig == null)
                throw new InvalidOperationException("No enabled AI provider found in configuration");

            var provider = factory.Create(providerConfig);
            return new ResilientAiProvider(provider);
        });

        return services;
    }

    private static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        // Secure storage — AES-256-GCM works on all platforms
        services.AddSingleton<ISecureStorage, AesSecureStorage>();

        // Audio capture — platform-specific
#if WINDOWS
        services.AddSingleton<IAudioCapture, WindowsAudioCapture>();
#elif MACOS
        services.AddSingleton<IAudioCapture, MacAudioCapture>();
#else
        throw new PlatformNotSupportedException("This platform is not supported. Windows and macOS are supported.");
#endif

        // Hotkey service — platform-specific
#if WINDOWS
        services.AddSingleton<IPlatformHotkeyService, WindowsHotkeyService>();
#elif MACOS
        services.AddSingleton<IPlatformHotkeyService, MacHotkeyService>();
#else
        services.AddSingleton<IPlatformHotkeyService, UnsupportedPlatformHotkeyService>();
#endif

        return services;
    }
}
