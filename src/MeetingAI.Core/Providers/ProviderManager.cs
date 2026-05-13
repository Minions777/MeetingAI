using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers;

public sealed class ProviderManager : IDisposable
{
    private readonly ProviderCollection _chatProviders;
    private readonly ProviderCollection _transcriptionProviders;

    public ProviderManager(IConfigurationService configService)
    {
        _chatProviders = new ProviderCollection(configService, p => p.SupportsChat);
        _transcriptionProviders = new ProviderCollection(configService, p => p.SupportsTranscription);
    }

    public Task<IReadOnlyDictionary<string, IAIProvider>> GetChatProvidersAsync()
        => _chatProviders.GetProvidersAsync();

    public Task<IReadOnlyDictionary<string, IAIProvider>> GetTranscriptionProvidersAsync()
        => _transcriptionProviders.GetProvidersAsync();

    public IReadOnlyDictionary<string, IAIProvider> GetChatProviders()
        => _chatProviders.GetProviders();

    public IReadOnlyDictionary<string, IAIProvider> GetTranscriptionProviders()
        => _transcriptionProviders.GetProviders();

    public void Dispose()
    {
        _chatProviders.Dispose();
        _transcriptionProviders.Dispose();
    }
}
