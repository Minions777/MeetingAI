using System.Runtime.CompilerServices;
using MeetingAI.Core.Constants;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public sealed class AIAssistantService : IAIAssistantService
{
    private readonly IConfigurationService _configService;
    private readonly ProviderManager _providerManager;

    public AIAssistantService(IConfigurationService configService, ProviderManager providerManager)
    {
        _configService = configService;
        _providerManager = providerManager;
    }

    public async IAsyncEnumerable<string> AskAsync(
        string selectedText,
        string context,
        TimeSpan? timestamp = null,
        string? providerId = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var (provider, providerConfig) = await GetProviderWithConfigAsync(providerId);
        var prompt = AIPromptTemplates.BuildAskAIPrompt(selectedText, context, timestamp);

        var request = new ChatRequest
        {
            Model = providerConfig.Model,
            SystemPrompt = "你是一个专业的会议助手，专注于回答关于会议内容的问题。回答必须简洁，不超过100字，并引用相关时间戳。",
            Temperature = 0.7,
            MaxTokens = 200,
            Messages =
            [
                new ChatMessage
                {
                    Role = "user",
                    Content = prompt
                }
            ]
        };

        LoggerService.Debug($"AI Assistant query with {provider.Name}");
        await foreach (var chunk in provider.StreamChatAsync(request, ct))
        {
            yield return chunk;
        }
    }

    public async Task<string> AskSingleAsync(
        string selectedText,
        string context,
        TimeSpan? timestamp = null,
        string? providerId = null,
        CancellationToken ct = default)
    {
        var sb = new System.Text.StringBuilder();
        await foreach (var chunk in AskAsync(selectedText, context, timestamp, providerId, ct))
        {
            sb.Append(chunk);
        }
        return sb.ToString();
    }

    private async Task<(IAIProvider Provider, ProviderConfig Config)> GetProviderWithConfigAsync(string? providerId)
    {
        var providers = await _providerManager.GetChatProvidersAsync();
        var settings = _configService.Load();
        var effectiveProviderId = providerId ?? settings.DefaultProviderId;

        if (!string.IsNullOrEmpty(effectiveProviderId) && providers.TryGetValue(effectiveProviderId, out var provider))
        {
            var config = settings.Providers.FirstOrDefault(p => p.Id == effectiveProviderId)!;
            return (provider, config);
        }

        var fallback = providers.FirstOrDefault(p => p.Value.SupportsChat);
        if (fallback.Value == null)
            throw new InvalidOperationException("No chat provider available for AI assistant");

        var fallbackConfig = settings.Providers.FirstOrDefault(p => p.Id == fallback.Key)!;
        return (fallback.Value, fallbackConfig);
    }
}