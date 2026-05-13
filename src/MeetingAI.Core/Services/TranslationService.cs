using System.Text.Json;
using System.Text.RegularExpressions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public sealed class TranslationService : ITranslationService, IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly ILanguageDetectionService _languageDetection;
    private readonly object _providersLock = new();
    private IReadOnlyDictionary<string, IAIProvider> _providers = new Dictionary<string, IAIProvider>();
    private readonly Lazy<Task> _initialization;
    private bool _disposed;

    private const string TranslationPromptTemplate = @"你是一个专业的会议翻译。请将以下中文内容翻译为英文，或将英文翻译为中文。

术语表（这些术语不要翻译）：
{terminology_list}

原文：
{text}

要求：
1. 保持术语表中术语不翻译
2. 准确传达原意
3. 返回JSON格式：{{""original"":""..."",""translation"":""...""}}";

    public TranslationService(IConfigurationService configService, ILanguageDetectionService languageDetection)
    {
        _configService = configService;
        _languageDetection = languageDetection;
        _configService.SettingsChanged += OnSettingsChanged;
        _initialization = new Lazy<Task>(() => Task.Run(RefreshProviders));
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        LoggerService.Info("Configuration changed, refreshing translation providers...");
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
                LoggerService.Info($"Loaded translation provider: {providerConfig.Name}");
            }
            catch (Exception ex)
            {
                LoggerService.Error($"Failed to load provider for translation {providerConfig.Name}", ex);
            }
        }

        return providers;
    }

    public async Task<TranslationResult> TranslateAsync(
        string text,
        LanguageType sourceLanguage,
        string terminologyList,
        string? providerId = null,
        CancellationToken ct = default)
    {
        await _initialization.Value;

        if (string.IsNullOrWhiteSpace(text))
            return new TranslationResult(text, string.Empty);

        var targetLanguage = sourceLanguage == LanguageType.ZhCn ? "英文" : "中文";
        var prompt = TranslationPromptTemplate
            .Replace("{terminology_list}", string.IsNullOrEmpty(terminologyList) ? "（无）" : terminologyList)
            .Replace("{text}", text);

        var settings = _configService.Load();
        var effectiveProviderId = providerId ?? settings.DefaultProviderId;

        if (!_providers.TryGetValue(effectiveProviderId, out var provider))
        {
            var fallback = _providers.FirstOrDefault(p => p.Value.SupportsChat);
            if (fallback.Value == null)
                throw new InvalidOperationException("No translation provider available");

            effectiveProviderId = fallback.Key;
            provider = fallback.Value;
        }

        var providerConfig = settings.Providers.FirstOrDefault(p => p.Id == effectiveProviderId)
            ?? throw new InvalidOperationException($"Provider configuration not found: {effectiveProviderId}");

        var request = new ChatRequest
        {
            Model = providerConfig.Model,
            SystemPrompt = "你是一个专业的会议翻译。始终返回有效的JSON格式响应。",
            Temperature = providerConfig.Temperature,
            MaxTokens = providerConfig.MaxTokens,
            Messages =
            [
                new ChatMessage
                {
                    Role = "user",
                    Content = prompt
                }
            ]
        };

        LoggerService.Info($"Translating ({sourceLanguage} -> {targetLanguage}) with {provider.Name}");
        var response = await provider.ChatAsync(request, ct);

        return ParseTranslationResponse(response.Content, text);
    }

    private static TranslationResult ParseTranslationResponse(string content, string originalText)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonMatch = Regex.Match(content, @"\{[^{}]*""original""[^{}]*""translation""[^{}]*\}", RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                using var doc = JsonDocument.Parse(jsonMatch.Value);
                var original = doc.RootElement.GetProperty("original").GetString() ?? originalText;
                var translation = doc.RootElement.GetProperty("translation").GetString() ?? string.Empty;
                return new TranslationResult(original, translation);
            }

            // Fallback: return original as-is with empty translation
            LoggerService.Warning("Failed to parse translation JSON, using fallback");
            return new TranslationResult(originalText, content.Trim());
        }
        catch (Exception ex)
        {
            LoggerService.Error("Translation parsing failed", ex);
            return new TranslationResult(originalText, content.Trim());
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
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
                disposable.Dispose();
        }
    }
}