using System.Runtime.CompilerServices;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public class SummaryService : ISummaryService, IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly object _providersLock = new();
    private IReadOnlyDictionary<string, IAIProvider> _providers = new Dictionary<string, IAIProvider>();
    private readonly Lazy<Task> _initialization;
    private bool _disposed;

    public const string DefaultSummaryPrompt = @"你是一个专业的会议助手。请根据以下会议记录，生成结构化的会议摘要：

1. **会议概要**：简要描述会议主题
2. **关键要点**：列出会议主要讨论内容
3. **行动项**：列出需要跟进的任务
4. **决议**：列出会议做出的决定
5. **待解决问题**：列出悬而未决的问题

请用中文回复，格式清晰，便于阅读。";

    public SummaryService(IConfigurationService configService)
    {
        _configService = configService;
        _configService.SettingsChanged += OnSettingsChanged;
        _initialization = new Lazy<Task>(() => Task.Run(RefreshProviders));
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        LoggerService.Info("Configuration changed, refreshing chat providers...");
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
                LoggerService.Info($"Loaded chat provider: {providerConfig.Name}");
            }
            catch (Exception ex)
            {
                LoggerService.Error($"Failed to load provider {providerConfig.Name}", ex);
            }
        }

        return providers;
    }

    public async Task<Summary> SummarizeAsync(
        Transcript transcript,
        string? providerId = null,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        await _initialization.Value;

        var settings = _configService.Load();
        var effectiveProviderId = providerId ?? settings.DefaultProviderId;
        var providers = _providers;

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

        var request = new ChatRequest
        {
            Model = providerConfig.Model,
            SystemPrompt = systemPrompt ?? providerConfig.SystemPrompt ?? DefaultSummaryPrompt,
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
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await _initialization.Value;

        var settings = _configService.Load();
        var effectiveProviderId = providerId ?? settings.DefaultProviderId;
        var providers = _providers;

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

        var request = new ChatRequest
        {
            Model = providerConfig.Model,
            SystemPrompt = systemPrompt ?? providerConfig.SystemPrompt ?? DefaultSummaryPrompt,
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

    private static Summary ParseSummaryResponse(string content)
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
        if (line.Contains("会议概要") || line.Contains("概要") || line.Contains("Overview", StringComparison.OrdinalIgnoreCase))
            return "overview";

        if (line.Contains("关键要点") || line.Contains("要点") || line.Contains("Key Points", StringComparison.OrdinalIgnoreCase))
            return "keypoints";

        if (line.Contains("行动项") || line.Contains("Action Items", StringComparison.OrdinalIgnoreCase))
            return "actionitems";

        if (line.Contains("决议") || line.Contains("Decisions", StringComparison.OrdinalIgnoreCase))
            return "decisions";

        if (line.Contains("待解决") || line.Contains("Questions", StringComparison.OrdinalIgnoreCase))
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
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
