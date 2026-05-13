using System.Text.Json;
using System.Text.RegularExpressions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public sealed class TranslationService : ITranslationService
{
    private readonly IConfigurationService _configService;
    private readonly ILanguageDetectionService _languageDetection;
    private readonly ProviderManager _providerManager;

    private const string TranslationPromptTemplate = @"你是一个专业的会议翻译。请将以下中文内容翻译为英文，或将英文翻译为中文。

术语表（这些术语不要翻译）：
{terminology_list}

原文：
{text}

要求：
1. 保持术语表中术语不翻译
2. 准确传达原意
3. 返回JSON格式：{{""original"":""..."",""translation"":""...""}}";

    public TranslationService(IConfigurationService configService, ILanguageDetectionService languageDetection, ProviderManager providerManager)
    {
        _configService = configService;
        _languageDetection = languageDetection;
        _providerManager = providerManager;
    }

    public async Task<TranslationResult> TranslateAsync(
        string text,
        LanguageType sourceLanguage,
        string terminologyList,
        string? providerId = null,
        CancellationToken ct = default)
    {
        var providers = await _providerManager.GetChatProvidersAsync();

        if (string.IsNullOrWhiteSpace(text))
            return new TranslationResult(text, string.Empty);

        var targetLanguage = sourceLanguage == LanguageType.ZhCn ? "英文" : "中文";
        var prompt = TranslationPromptTemplate
            .Replace("{terminology_list}", string.IsNullOrEmpty(terminologyList) ? "（无）" : terminologyList)
            .Replace("{text}", text);

        var settings = _configService.Load();
        var effectiveProviderId = providerId ?? settings.DefaultProviderId;

        if (!providers.TryGetValue(effectiveProviderId, out var provider))
        {
            var fallback = providers.FirstOrDefault(p => p.Value.SupportsChat);
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
}