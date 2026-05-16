using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers;

public sealed class ProviderManager : IDisposable
{
    private readonly ProviderCollection _chatProviders;
    private readonly ProviderCollection _transcriptionProviders;
    private readonly IConfigurationService _configService;

    public ProviderManager(IConfigurationService configService)
    {
        _configService = configService;
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

    /// <summary>
    /// Resolves a chat provider by ID with fallback to the first available provider.
    /// Returns the provider and its configuration.
    /// </summary>
    public async Task<(IAIProvider Provider, ProviderConfig Config)> ResolveChatProviderAsync(string? providerId)
    {
        var providers = await GetChatProvidersAsync();
        var settings = _configService.Load();
        var effectiveId = providerId ?? settings.DefaultProviderId;

        if (!string.IsNullOrEmpty(effectiveId) && providers.TryGetValue(effectiveId, out var provider))
        {
            var config = settings.Providers.FirstOrDefault(p => p.Id == effectiveId)
                ?? throw new InvalidOperationException($"Provider configuration not found: {effectiveId}");
            return (provider, config);
        }

        var fallback = providers.FirstOrDefault(p => p.Value.SupportsChat);
        if (fallback.Value == null)
            throw new InvalidOperationException("No chat provider available");

        var fallbackConfig = settings.Providers.FirstOrDefault(p => p.Id == fallback.Key)
            ?? throw new InvalidOperationException($"Provider configuration not found: {fallback.Key}");
        return (fallback.Value, fallbackConfig);
    }

    public void Dispose()
    {
        _chatProviders.Dispose();
        _transcriptionProviders.Dispose();
    }
}
