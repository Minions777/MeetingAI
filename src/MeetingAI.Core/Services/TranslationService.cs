using System.Text.Json;
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
        if (string.IsNullOrWhiteSpace(text))
            return new TranslationResult(text, string.Empty);

        var (provider, providerConfig) = await _providerManager.ResolveChatProviderAsync(providerId);

        var targetLanguage = sourceLanguage == LanguageType.ZhCn ? "英文" : "中文";
        var prompt = TranslationPromptTemplate
            .Replace("{terminology_list}", string.IsNullOrEmpty(terminologyList) ? "（无）" : terminologyList)
            .Replace("{text}", text);

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
            var json = ExtractJsonObject(content);
            if (json != null)
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var original = root.TryGetProperty("original", out var origEl) ? origEl.GetString() ?? originalText : originalText;
                var translation = root.TryGetProperty("translation", out var transEl) ? transEl.GetString() ?? string.Empty : string.Empty;
                return new TranslationResult(original, translation);
            }

            LoggerService.Warning("Failed to parse translation JSON, using fallback");
            return new TranslationResult(originalText, content.Trim());
        }
        catch (Exception ex)
        {
            LoggerService.Error("Translation parsing failed", ex);
            return new TranslationResult(originalText, content.Trim());
        }
    }

    private static string? ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        for (var i = start; i < content.Length; i++)
        {
            if (content[i] == '{') depth++;
            else if (content[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return content[start..(i + 1)];
            }
        }
        return null;
    }
}