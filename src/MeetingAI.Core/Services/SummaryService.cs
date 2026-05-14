using System.Runtime.CompilerServices;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public class SummaryService : ISummaryService
{
    private readonly IConfigurationService _configService;
    private readonly ProviderManager _providerManager;

    public const string DefaultSummaryPrompt = @"You are a professional meeting assistant. Based on the meeting transcript below, generate a structured meeting summary:

1. **Overview / 会议概要**: Briefly describe the meeting topic
2. **Key Points / 关键要点**: List main discussion items
3. **Action Items / 行动项**: List follow-up tasks with assignees if mentioned
4. **Decisions / 决议**: List decisions made
5. **Open Issues / 待解决问题**: List unresolved questions

Respond in the same language as the transcript. Format clearly for readability.";

    private const string TerminologySectionPrompt = @"

术语表（以下术语请保持不翻译）：
{terminology_list}";

    public SummaryService(IConfigurationService configService, ProviderManager providerManager)
    {
        _configService = configService;
        _providerManager = providerManager;
    }

    private static string BuildSystemPrompt(string? systemPrompt, string? terminologyList)
    {
        var basePrompt = systemPrompt ?? DefaultSummaryPrompt;
        if (string.IsNullOrEmpty(terminologyList))
            return basePrompt;

        var terminologySection = TerminologySectionPrompt.Replace("{terminology_list}", terminologyList);
        return basePrompt + terminologySection;
    }

    public async Task<Summary> SummarizeAsync(
        Transcript transcript,
        string? providerId = null,
        string? systemPrompt = null,
        string? terminologyList = null,
        CancellationToken ct = default)
    {
        var providers = await _providerManager.GetChatProvidersAsync();

        var settings = _configService.Load();
        var effectiveProviderId = providerId ?? settings.DefaultProviderId;

        if (!providers.TryGetValue(effectiveProviderId, out var provider))
        {
            var fallback = providers.FirstOrDefault(p => p.Value.SupportsChat);
            if (fallback.Value == null)
                throw new InvalidOperationException("No chat provider available");

            effectiveProviderId = fallback.Key;
            provider = fallback.Value;
        }

        var providerConfig = settings.Providers.FirstOrDefault(p => p.Id == effectiveProviderId)
            ?? throw new InvalidOperationException($"Provider configuration not found: {effectiveProviderId}");

        var effectiveSystemPrompt = BuildSystemPrompt(systemPrompt, terminologyList);

        var request = new ChatRequest
        {
            Model = providerConfig.Model,
            SystemPrompt = effectiveSystemPrompt,
            Temperature = providerConfig.Temperature,
            MaxTokens = providerConfig.MaxTokens,
            Messages =
            [
                new ChatMessage
                {
                    Role = "user",
                    Content = $"请总结以下会议记录：\n\n{transcript.Text}"
                }
            ]
        };

        LoggerService.Info($"Generating summary with {provider.Name}");
        var response = await provider.ChatAsync(request, ct);
        var summary = ParseSummaryResponse(response.Content);

        LoggerService.Info(
            $"Summary generated: Overview={summary.Overview?.Length ?? 0}, " +
            $"KeyPoints={summary.KeyPoints.Count}, " +
            $"ActionItems={summary.ActionItems.Count}, " +
            $"Decisions={summary.Decisions.Count}");

        return summary;
    }

    public async IAsyncEnumerable<string> StreamSummarizeAsync(
        Transcript transcript,
        string? providerId = null,
        string? systemPrompt = null,
        string? terminologyList = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var providers = await _providerManager.GetChatProvidersAsync();

        var settings = _configService.Load();
        var effectiveProviderId = providerId ?? settings.DefaultProviderId;

        if (!providers.TryGetValue(effectiveProviderId, out var provider))
        {
            var fallback = providers.FirstOrDefault(p => p.Value.SupportsChat);
            if (fallback.Value == null)
                throw new InvalidOperationException("No chat provider available");

            effectiveProviderId = fallback.Key;
            provider = fallback.Value;
        }

        var providerConfig = settings.Providers.FirstOrDefault(p => p.Id == effectiveProviderId)
            ?? throw new InvalidOperationException($"Provider configuration not found: {effectiveProviderId}");

        var effectiveSystemPrompt = BuildSystemPrompt(systemPrompt, terminologyList);

        var request = new ChatRequest
        {
            Model = providerConfig.Model,
            SystemPrompt = effectiveSystemPrompt,
            Temperature = providerConfig.Temperature,
            MaxTokens = providerConfig.MaxTokens,
            Messages =
            [
                new ChatMessage
                {
                    Role = "user",
                    Content = $"请总结以下会议记录：\n\n{transcript.Text}"
                }
            ]
        };

        LoggerService.Info($"Streaming summary with {provider.Name}");
        await foreach (var chunk in provider.StreamChatAsync(request, ct))
        {
            yield return chunk;
        }
    }

    internal static Summary ParseSummaryResponse(string content)
    {
        var summary = new Summary();

        if (string.IsNullOrWhiteSpace(content))
        {
            LoggerService.Warning("AI returned empty summary content.");
            summary.Overview = "[摘要生成失败：AI 返回内容为空]";
            return summary;
        }

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var currentSection = "";
        var parsed = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            var section = DetectSection(trimmed);
            if (!string.IsNullOrEmpty(section))
            {
                currentSection = section;
                var inlineText = ExtractInlineSectionText(trimmed);
                if (!string.IsNullOrEmpty(inlineText))
                    parsed |= AddToSection(summary, currentSection, inlineText);
                continue;
            }

            var cleanLine = CleanSummaryLine(trimmed);
            if (!string.IsNullOrEmpty(cleanLine))
                parsed |= AddToSection(summary, currentSection, cleanLine);
        }

        if (!parsed)
        {
            LoggerService.Warning("Summary parsing failed; using fallback parsing.");
            summary = TryFallbackParsing(content);
        }

        if (string.IsNullOrWhiteSpace(summary.Overview))
        {
            summary.Overview = content.Length > 200 ? content[..200] + "..." : content;
        }

        return summary;
    }

    private static string DetectSection(string line)
    {
        if (line.Contains("会议概要") || line.Contains("概要") || line.Contains("Overview", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Meeting Overview", StringComparison.OrdinalIgnoreCase) || line.Contains("Summary", StringComparison.OrdinalIgnoreCase))
            return "overview";

        if (line.Contains("关键要点") || line.Contains("要点") || line.Contains("Key Points", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Key Discussion", StringComparison.OrdinalIgnoreCase) || line.Contains("Main Points", StringComparison.OrdinalIgnoreCase))
            return "keypoints";

        if (line.Contains("行动项") || line.Contains("Action Items", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Todo", StringComparison.OrdinalIgnoreCase)
            || line.Contains("To-Do", StringComparison.OrdinalIgnoreCase))
            return "actionitems";

        if (line.Contains("决议") || line.Contains("Decisions", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Decision", StringComparison.OrdinalIgnoreCase) || line.Contains("Resolution", StringComparison.OrdinalIgnoreCase))
            return "decisions";

        if (line.Contains("待解决") || line.Contains("Questions", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Open Issues", StringComparison.OrdinalIgnoreCase) || line.Contains("Pending", StringComparison.OrdinalIgnoreCase))
            return "questions";

        return "";
    }

    private static string ExtractInlineSectionText(string line)
    {
        var separators = new[] { "：", ":", "-" };
        foreach (var separator in separators)
        {
            var index = line.IndexOf(separator, StringComparison.Ordinal);
            if (index >= 0 && index + separator.Length < line.Length)
                return CleanSummaryLine(line[(index + separator.Length)..]);
        }

        return "";
    }

    private static string CleanSummaryLine(string line)
    {
        return line
            .Trim()
            .TrimStart('#', '-', '*', '>', ' ', '\t')
            .TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '、')
            .Trim();
    }

    private static bool AddToSection(Summary summary, string section, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        switch (section)
        {
            case "overview":
                summary.Overview = string.IsNullOrWhiteSpace(summary.Overview)
                    ? value
                    : $"{summary.Overview} {value}";
                return true;
            case "keypoints":
                summary.KeyPoints.Add(value);
                return true;
            case "actionitems":
                summary.ActionItems.Add(Core.Models.ActionItem.Create(value));
                return true;
            case "decisions":
                summary.Decisions.Add(value);
                return true;
            case "questions":
                summary.Questions.Add(value);
                return true;
            default:
                return false;
        }
    }

    private static Summary TryFallbackParsing(string content)
    {
        var lines = content
            .Replace("```", "")
            .Replace("`", "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanSummaryLine)
            .Where(line => line.Length > 0)
            .ToList();

        var summary = new Summary();
        if (lines.Count == 0)
            return summary;

        summary.Overview = lines[0];
        foreach (var line in lines.Skip(1).Take(5))
            summary.KeyPoints.Add(line);

        return summary;
    }
}
