using System.Runtime.CompilerServices;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Resilience;

public sealed class ProviderSwitcher
{
    private readonly IConfigurationService _configService;
    private readonly List<IAIProviderWrapper> _providers = new();
    private int _currentIndex;
    private readonly object _lock = new();

    public ProviderSwitcher(IConfigurationService configService, IEnumerable<IAIProvider> providers)
    {
        _configService = configService;
        foreach (var provider in providers)
        {
            _providers.Add(new ResilientAiProvider(provider));
        }
        LoggerService.Info($"ProviderSwitcher initialized with {_providers.Count} providers");
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        var startIndex = _currentIndex;

        for (int i = 0; i < _providers.Count; i++)
        {
            var index = (startIndex + i) % _providers.Count;
            try
            {
                var result = await _providers[index].ChatAsync(request, ct);
                if (result.IsSuccess)
                {
                    lock (_lock)
                    {
                        _currentIndex = index;
                    }
                    return result;
                }
                LoggerService.Warning($"Provider {index} returned unsuccessful result, trying next");
            }
            catch (Exception ex)
            {
                LoggerService.Warning($"Provider {index} ({_providers[index].ProviderName}) failed: {ex.Message}");
            }
        }

        return new ChatResponse
        {
            Content = "所有 AI Provider 均不可用",
            FinishReason = "error",
            IsSuccess = false
        };
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var startIndex = _currentIndex;

        for (int i = 0; i < _providers.Count; i++)
        {
            var index = (startIndex + i) % _providers.Count;
            var chunks = new List<string>();

            try
            {
                await foreach (var chunk in _providers[index].StreamChatAsync(request, ct))
                {
                    chunks.Add(chunk);
                }
            }
            catch (Exception ex)
            {
                LoggerService.Warning($"Provider {index} ({_providers[index].ProviderName}) streaming failed: {ex.Message}");
                continue;
            }

            if (chunks.Count > 0)
            {
                lock (_lock)
                {
                    _currentIndex = index;
                }
                foreach (var chunk in chunks)
                {
                    yield return chunk;
                }
                yield break;
            }
        }

        LoggerService.Error("All providers failed to stream");
    }

    public int ActiveProviderIndex => _currentIndex;
    public string ActiveProviderName => _providers.Count > 0 ? _providers[_currentIndex].ProviderName : "None";
}