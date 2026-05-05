using MeetingAI.Core.Models;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers.Implementations;

public class ZhipuProvider : BaseAIProvider
{
    public override string Id => "zhipu";
    public override string Name => "智谱 AI";
    public override AIProviderType ProviderType => AIProviderType.Zhipu;
    
    public override IReadOnlyList<string> SupportedChatModels { get; } = new[]
    {
        "glm-4", "glm-4-flash", "glm-4-plus", "glm-3-turbo"
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
            TokensUsed = 0
        };
    }
}
