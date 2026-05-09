using Microsoft.Extensions.DependencyInjection;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Core.Resilience;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddMeetingAICore(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // AI Provider Factory
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();

        // Services
        services.AddSingleton<IRecordingService, RecordingService>();
        services.AddSingleton<ITranscriptionService, TranscriptionService>();
        services.AddSingleton<ISummaryService, SummaryService>();
        services.AddSingleton<MeetingHistoryService>();

        // Resilience
        services.AddSingleton<IAIProviderWrapper, ResilientAiProvider>();
        services.AddSingleton<ProviderSwitcher>();

        return services;
    }
}