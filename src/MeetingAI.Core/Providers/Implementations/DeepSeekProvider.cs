using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers.Implementations;

public class DeepSeekProvider : BaseAIProvider
{
    public override string Id => "deepseek";
    public override string Name => "DeepSeek";
    public override AIProviderType ProviderType => AIProviderType.DeepSeek;
    
    public override IReadOnlyList<string> SupportedChatModels { get; } = new[]
    {
        "deepseek-chat", "deepseek-coder"
    };
    
    public override IReadOnlyList<string> SupportedTranscriptionModels { get; } = Array.Empty<string>();
    
    public override bool SupportsTranscription => false;
    public override bool SupportsChat => true;
    
    protected override HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(_config?.TimeoutSeconds ?? 120) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config?.ApiKey);
        return client;
    }
    
    public override async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        if (_httpClient == null || _config == null)
            throw new InvalidOperationException("Provider not configured");
            
        var endpoint = $"{_config.BaseUrl.TrimEnd('/')}/chat/completions";
        
        var body = new
        {
            model = request.Model ?? _config.Model,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt }
            }.Concat(request.Messages.Select(m => new { role = m.Role, content = m.Content })).ToArray(),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens
        };
        
        var json = await SendRequestAsync(_httpClient, endpoint, CreateJsonContent(body), ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        
        return new ChatResponse
        {
            Content = content,
            Model = request.Model ?? _config.Model,
            TokensUsed = root.GetProperty("usage").GetProperty("total_tokens").GetInt32(),
            FinishReason = root.GetProperty("choices")[0].GetProperty("finish_reason").GetString() ?? ""
        };
    }
}
