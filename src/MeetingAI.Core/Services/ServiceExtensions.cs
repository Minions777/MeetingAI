using Microsoft.Extensions.DependencyInjection;
using MeetingAI.Core.Providers;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddMeetingAICore(this IServiceCollection services)
    {
        // Configuration
        services.AddSingleton<ConfigurationService>();
        
        // AI Providers
        services.AddSingleton<ProviderFactory>();
        
        // Services
        services.AddSingleton<IRecordingService, RecordingService>();
        services.AddSingleton<ITranscriptionService, TranscriptionService>();
        services.AddSingleton<ISummaryService, SummaryService>();
        
        return services;
    }
}
