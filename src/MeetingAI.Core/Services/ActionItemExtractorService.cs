using System.Text.Json;
using System.Text.RegularExpressions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public sealed class ActionItemExtractorService : IActionItemExtractor
{
    private readonly IConfigurationService _configService;
    private readonly ProviderManager _providerManager;

    public ActionItemExtractorService(IConfigurationService configService, ProviderManager providerManager)
    {
        _configService = configService;
        _providerManager = providerManager;
    }

    public async Task<IReadOnlyList<ActionItem>> ExtractAsync(string summaryText, CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, IAIProvider> providers;
        try
        {
            providers = await _providerManager.GetChatProvidersAsync();
        }
        catch (Exception ex)
        {
            LoggerService.Warning($"No chat provider available for action item extraction, using regex fallback: {ex.Message}");
            return RegexExtract(summaryText);
        }

        if (providers.Count == 0)
        {
            LoggerService.Warning("No chat provider available for action item extraction, using regex fallback");
            return RegexExtract(summaryText);
        }

        var (provider, providerConfig) = await _providerManager.ResolveChatProviderAsync(null);

        try
        {
            var prompt = BuildExtractionPrompt(summaryText, DateTime.UtcNow);
            var request = new ChatRequest
            {
                Model = providerConfig.Model,
                SystemPrompt = "你是一个专业的会议助手，擅长从会议摘要中提取待办事项。",
                Temperature = 0.3,
                MaxTokens = 2048,
                Messages = [new ChatMessage { Role = "user", Content = prompt }]
            };

            var response = await provider.ChatAsync(request, ct);
            return ParseExtractionResponse(response.Content);
        }
        catch (Exception ex)
        {
            LoggerService.Warning($"LLM extraction failed, falling back to regex: {ex.Message}");
            return RegexExtract(summaryText);
        }
    }

    private static string BuildExtractionPrompt(string summaryText, DateTime referenceDate)
    {
        return $@"从以下会议摘要中提取所有待办事项（行动项），并以 JSON 格式输出。

要求：
1. 每个待办事项包含：description（描述）、assignee（负责人，如有）、dueDate（截止日期，如有时区设为 UTC）、priority（优先级：Low/Medium/High/Critical）
2. 只提取明确的行动项，忽略讨论性质的描述
3. 截止日期如果提到""本周""、""下周""、""月底""等模糊时间，转换为具体日期（以今天 {referenceDate:yyyy-MM-dd} 为基准）
4. 如果没有待办事项，返回空数组 []

会议摘要：
{summaryText}

请直接返回 JSON，无需解释：";
    }

    private static IReadOnlyList<ActionItem> ParseExtractionResponse(string content)
    {
        try
        {
            var json = ExtractJson(content);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() == 0)
                return [];

            var items = new List<ActionItem>();
            foreach (var element in root.EnumerateArray())
            {
                var description = element.GetProperty("description").GetString() ?? "";
                var assignee = element.TryGetProperty("assignee", out var a) && a.ValueKind == JsonValueKind.String
                    ? a.GetString() : null;
                var priority = Priority.Medium;
                if (element.TryGetProperty("priority", out var p) && p.ValueKind == JsonValueKind.String)
                {
                    if (Enum.TryParse<Priority>(p.GetString(), true, out var pp))
                        priority = pp;
                }

                DateTime? dueDate = null;
                if (element.TryGetProperty("dueDate", out var d) && d.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(d.GetString(), out var parsed))
                        dueDate = parsed.ToUniversalTime();
                }

                if (!string.IsNullOrWhiteSpace(description))
                    items.Add(ActionItem.Create(description, assignee, dueDate, priority: priority));
            }

            return items;
        }
        catch (Exception ex)
        {
            LoggerService.Warning($"Failed to parse action items JSON: {ex.Message}");
            return [];
        }
    }

    private static string ExtractJson(string content)
    {
        var start = content.IndexOf('[');
        var end = content.LastIndexOf(']');
        if (start < 0 || end < 0 || start > end)
        {
            start = content.IndexOf('{');
            end = content.LastIndexOf('}');
        }
        if (start < 0 || end < 0)
            return "[]";
        return content[start..(end + 1)];
    }

    private static IReadOnlyList<ActionItem> RegexExtract(string text)
    {
        var items = new List<ActionItem>();

        var triggerPatterns = new[]
        {
            @"([一-龥a-zA-Z]{2,10})(?:需|应该|必须|负责|将)(?:要|在|于|把|完成|提交|发送|准备|安排)[:：]?\s*([^\n，。！？]{5,80})",
            @"(?:需|应该|必须)[:：]?\s*([^\n，。！？]{5,80})",
            @"(?:截止|截至|限期)[:：]?\s*([^\n]{5,60})",
            @"在\s*(\d{1,2}[月/\-]\d{1,2}[日]?(?:前|内)?)\s*完成[:：]?\s*([^\n，。]{5,60})",
        };

        foreach (var pattern in triggerPatterns)
        {
            var matches = Regex.Matches(text, pattern);
            foreach (Match match in matches)
            {
                var groups = match.Groups;
                string description;
                string? assignee = null;

                if (groups.Count >= 3 && !string.IsNullOrWhiteSpace(groups[2].Value))
                {
                    assignee = groups[1].Value.Trim();
                    description = groups[2].Value.Trim();
                }
                else if (groups.Count >= 2)
                {
                    description = groups[1].Value.Trim();
                }
                else continue;

                if (description.Length < 5) continue;

                description = Regex.Replace(description, @"^(?:需|应该|必须)\s*", "");

                DateTime? dueDate = null;
                var dueDatePattern = @"(?:本周|下周|本月|月底|月中|周末)";
                var dueDateMatch = Regex.Match(text, dueDatePattern);
                if (dueDateMatch.Success)
                    dueDate = ResolveRelativeDate(dueDateMatch.Value);

                items.Add(ActionItem.Create(description, assignee, dueDate));
            }
        }

        return items.DistinctBy(i => i.Description).ToList();
    }

    private static DateTime? ResolveRelativeDate(string relative)
    {
        var today = DateTime.UtcNow.Date;
        return relative switch
        {
            "本周" => today.AddDays(7 - (int)today.DayOfWeek),
            "下周" => today.AddDays(14 - (int)today.DayOfWeek),
            "本月" => new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)),
            "月底" or "月中" => new DateTime(today.Year, today.Month, 15),
            "周末" => today.AddDays(DayOfWeek.Saturday >= today.DayOfWeek
                ? (int)DayOfWeek.Saturday - (int)today.DayOfWeek
                : 7 - (int)today.DayOfWeek + (int)DayOfWeek.Saturday),
            _ => null
        };
    }
}