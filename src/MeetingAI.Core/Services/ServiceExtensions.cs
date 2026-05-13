using Microsoft.Extensions.DependencyInjection;
using MeetingAI.Core.Providers;
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

        // AI Provider Factory
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
        services.AddSingleton<ProviderManager>();

        // Services
        services.AddSingleton<IRecordingService, RecordingService>();
        services.AddSingleton<ITranscriptionService, TranscriptionService>();
        services.AddSingleton<ISummaryService, SummaryService>();
        services.AddSingleton<IAIAssistantService, AIAssistantService>();
        services.AddSingleton<MeetingHistoryService>();

        // Additional services
        services.AddSingleton<ILanguageDetectionService, LanguageDetectionService>();
        services.AddSingleton<ITranslationService, TranslationService>();
        services.AddSingleton<ITerminologyService, TerminologyService>();
        services.AddSingleton<IActionItemExtractor, ActionItemExtractorService>();
        services.AddSingleton<IMermaidRendererService, MermaidRendererService>();

        // Speaker diarization: inner implementation + facade
        services.AddSingleton<OnnxSpeakerDiarizationService>();
        services.AddSingleton<ISpeakerDiarizationService>(sp =>
            new SpeakerDiarizationService(sp.GetRequiredService<OnnxSpeakerDiarizationService>()));

        // Combined transcription (Whisper + diarization)
        services.AddSingleton<CombinedTranscriptionService>();

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
