using MeetingAI.Core.Models;
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
        LoggerService.Info($"Summary generated successfully");
        
        return summary;
    }
    
    private Summary ParseSummaryResponse(string content)
    {
        var summary = new Summary();
        
        // Simple parsing - in production could use more sophisticated approach
        var lines = content.Split('\n');
        var currentSection = "";
        
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
                            summary.Overview = cleanLine;
                        break;
                    case "keypoints":
                        if (!string.IsNullOrEmpty(cleanLine))
                            summary.KeyPoints.Add(cleanLine);
                        break;
                    case "actionitems":
                        if (!string.IsNullOrEmpty(cleanLine))
                            summary.ActionItems.Add(cleanLine);
                        break;
                    case "decisions":
                        if (!string.IsNullOrEmpty(cleanLine))
                            summary.Decisions.Add(cleanLine);
                        break;
                }
            }
        }
        
        // If no sections were parsed, use the entire content as overview
        if (string.IsNullOrEmpty(summary.Overview))
        {
            summary.Overview = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
        }
        
        return summary;
    }
}
