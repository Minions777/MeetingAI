using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Constants;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public class SummaryService : ISummaryService
{
    private readonly ConfigurationService _configService;
    private readonly Dictionary<string, IAIProvider> _providers = new();
    
    private const string DefaultSummaryPrompt = @"你是一个专业的会议助手。请根据以下会议记录，生成结构化的会议摘要：

1. **会议概要** (50字以内)：简要描述会议主题
2. **关键要点** (3-5条)：列出会议的主要讨论内容
3. **行动项** (如有)：列出需要跟进的任务和负责人
4. **决议** (如有)：列出会议做出的决定
5. **待解决问题** (如有)：列出悬而未决的问题

请用中文回复，格式清晰，便于阅读。";
    
    public SummaryService(ConfigurationService configService)
    {
        _configService = configService;
        InitializeProviders();
    }
    
    private void InitializeProviders()
    {
        var settings = _configService.Load();
        foreach (var providerConfig in settings.Providers.Where(p => p.IsEnabled && p.SupportsChat))
        {
            try
            {
                var provider = ProviderFactory.Create(providerConfig);
                _providers[providerConfig.Id] = provider;
                LoggerService.Info($"Loaded chat provider: {providerConfig.Name}");
            }
            catch (Exception ex)
            {
                LoggerService.Error($"Failed to load provider {providerConfig.Name}", ex);
            }
        }
    }
    
    public async Task<Summary> SummarizeAsync(
        Transcript transcript,
        string? providerId = null,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        var settings = _configService.Load();
        providerId ??= settings.DefaultProviderId;
        
        if (!_providers.TryGetValue(providerId, out var provider))
        {
            provider = _providers.Values.FirstOrDefault(p => p.SupportsChat);
            if (provider == null)
                throw new InvalidOperationException("No chat provider available");
        }
        
        var providerConfig = settings.Providers.First(p => p.Id == providerId);
        
        var request = new ChatRequest
        {
            Model = providerConfig.Model,
            SystemPrompt = systemPrompt ?? providerConfig.SystemPrompt ?? DefaultSummaryPrompt,
            Temperature = providerConfig.Temperature,
            MaxTokens = providerConfig.MaxTokens,
            Messages = new List<ChatMessage>
            {
                new ChatMessage 
                { 
                    Role = "user", 
                    Content = $"请总结以下会议记录：\n\n{transcript.Text}" 
                }
            }
        };
        
        LoggerService.Info($"Generating summary with {provider.Name}");
        var response = await provider.ChatAsync(request, ct);
        
        var summary = ParseSummaryResponse(response.Content);
        
        // 记录摘要生成统计
        LoggerService.Info($"摘要生成完成: Overview={summary.Overview?.Length ?? 0}字符, " +
            $"KeyPoints={summary.KeyPoints.Count}条, " +
            $"ActionItems={summary.ActionItems.Count}条, " +
            $"Decisions={summary.Decisions.Count}条");
        
        return summary;
    }
    
    private Summary ParseSummaryResponse(string content)
    {
        var summary = new Summary();
        
        if (string.IsNullOrWhiteSpace(content))
        {
            LoggerService.Warning("AI 返回内容为空");
            summary.Overview = "[摘要生成失败：AI 返回内容为空]";
            return summary;
        }
        
        var lines = content.Split('\n');
        var currentSection = "";
        var parseSuccess = false;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("**会议概要**") || trimmed.Contains("会议概要"))
            {
                currentSection = "overview";
            }
            else if (trimmed.StartsWith("**关键要点**") || trimmed.Contains("关键要点"))
            {
                currentSection = "keypoints";
            }
            else if (trimmed.StartsWith("**行动项**") || trimmed.Contains("行动项"))
            {
                currentSection = "actionitems";
            }
            else if (trimmed.StartsWith("**决议**") || trimmed.Contains("决议"))
            {
                currentSection = "decisions";
            }
            else if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("#"))
            {
                var cleanLine = trimmed.TrimStart('-', '*', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '、');
                
                switch (currentSection)
                {
                    case "overview":
                        if (string.IsNullOrEmpty(summary.Overview))
                        {
                            summary.Overview = cleanLine;
                            parseSuccess = true;
                        }
                        break;
                    case "keypoints":
                        if (!string.IsNullOrEmpty(cleanLine))
                        {
                            summary.KeyPoints.Add(cleanLine);
                            parseSuccess = true;
                        }
                        break;
                    case "actionitems":
                        if (!string.IsNullOrEmpty(cleanLine))
                        {
                            summary.ActionItems.Add(cleanLine);
                            parseSuccess = true;
                        }
                        break;
                    case "decisions":
                        if (!string.IsNullOrEmpty(cleanLine))
                        {
                            summary.Decisions.Add(cleanLine);
                            parseSuccess = true;
                        }
                        break;
                }
            }
        }
        
        // 如果没有成功解析到任何内容，添加警告并使用原始内容
        if (!parseSuccess)
        {
            LoggerService.Warning("摘要解析失败，AI 返回格式可能不规范");
            
            // 尝试更宽松的解析策略
            summary = TryFallbackParsing(content);
            
            if (string.IsNullOrEmpty(summary.Overview))
            {
                // 最终 fallback：截取前200字符作为概览
                summary.Overview = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
                LoggerService.Warning($"使用原始内容前200字符作为摘要: {summary.Overview.Length}字符");
            }
        }
        
        return summary;
    }
    
    /// <summary>
    /// 备用解析策略：尝试更宽松的格式匹配
    /// </summary>
    private Summary TryFallbackParsing(string content)
    {
        var summary = new Summary();
        
        // 移除 Markdown 代码块
        var cleanContent = content
            .Replace("```", "")
            .Replace("`", "");
        
        // 按行分割，尝试提取内容
        var lines = cleanContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        var collectedContent = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // 过滤掉标题符号但保留内容
            if (trimmed.Length > 2)
            {
                var cleanLine = trimmed
                    .TrimStart('#', '*', '-', '>', ' ')
                    .Trim();
                
                if (!string.IsNullOrEmpty(cleanLine) && !cleanLine.StartsWith("请") && !cleanLine.StartsWith("根据"))
                {
                    collectedContent.Add(cleanLine);
                }
            }
        }
        
        // 如果收集到内容，第一条作为概览，其余作为要点
        if (collectedContent.Count > 0)
        {
            summary.Overview = collectedContent[0];
            
            for (int i = 1; i < collectedContent.Count && i <= 5; i++)
            {
                summary.KeyPoints.Add(collectedContent[i]);
            }
            
            LoggerService.Info($"备用解析成功: {collectedContent.Count}条内容");
        }
        
        return summary;
    }
}
