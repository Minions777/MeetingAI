using Microsoft.Extensions.DependencyInjection;
using MeetingAI.Core.Providers;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddMeetingAICore(this IServiceCollection services)
    {
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<IRecordingService, RecordingService>();
        services.AddSingleton<ITranscriptionService, TranscriptionService>();
        services.AddSingleton<ISummaryService, SummaryService>();
        return services;
    }
}
