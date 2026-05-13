using System.Runtime.CompilerServices;
using MeetingAI.Core.Constants;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public sealed class AIAssistantService : IAIAssistantService, IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly object _providersLock = new();
    private IReadOnlyDictionary<string, IAIProvider> _providers = new Dictionary<string, IAIProvider>();
    private readonly Lazy<Task> _initialization;
    private bool _disposed;

    public AIAssistantService(IConfigurationService configService)
    {
        _configService = configService;
        _configService.SettingsChanged += OnSettingsChanged;
        _initialization = new Lazy<Task>(() => Task.Run(RefreshProviders));
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        LoggerService.Info("Configuration changed, refreshing AI assistant providers...");
        _ = Task.Run(RefreshProviders);
    }

    private void RefreshProviders()
    {
        ReplaceProviders(CreateProviders());
    }

    private IReadOnlyDictionary<string, IAIProvider> CreateProviders()
    {
        var settings = _configService.Load();
        var providers = new Dictionary<string, IAIProvider>();

        foreach (var providerConfig in settings.Providers.Where(p => p.IsEnabled && p.SupportsChat))
        {
            try
            {
                var provider = ProviderFactory.Create(providerConfig);
                providers[providerConfig.Id] = provider;
                LoggerService.Info($"Loaded chat provider for AI assistant: {providerConfig.Name}");
            }
            catch (Exception ex)
            {
                LoggerService.Error($"Failed to load provider {providerConfig.Name}", ex);
            }
        }

        return providers;
    }

    public async IAsyncEnumerable<string> AskAsync(
        string selectedText,
        string context,
        TimeSpan? timestamp = null,
        string? providerId = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await _initialization.Value;

        var (provider, providerConfig) = GetProviderWithConfig(providerId);
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

    private (IAIProvider Provider, ProviderConfig Config) GetProviderWithConfig(string? providerId)
    {
        var settings = _configService.Load();
        var effectiveProviderId = providerId ?? settings.DefaultProviderId;
        var providers = _providers;

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _configService.SettingsChanged -= OnSettingsChanged;
                DisposeProviders(ReplaceProviders(new Dictionary<string, IAIProvider>()));
            }
            _disposed = true;
        }
    }

    private IReadOnlyDictionary<string, IAIProvider> ReplaceProviders(IReadOnlyDictionary<string, IAIProvider> providers)
    {
        lock (_providersLock)
        {
            var oldProviders = _providers;
            _providers = providers;
            return oldProviders;
        }
    }

    private static void DisposeProviders(IReadOnlyDictionary<string, IAIProvider> providers)
    {
        foreach (var provider in providers.Values)
        {
            if (provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}