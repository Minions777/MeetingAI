using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers.Implementations;

public class MiniMaxProvider : BaseAIProvider
{
    public override string Id => "minimax";
    public override string Name => "MiniMax";
    public override AIProviderType ProviderType => AIProviderType.MiniMax;
    
    public override IReadOnlyList<string> SupportedChatModels { get; } = new[]
    {
        "MiniMax-Text-01", "abab6.5s-chat", "abab6.5-chat"
    };
    
    public override IReadOnlyList<string> SupportedTranscriptionModels { get; } = new[]
    {
        "speech-02-hd"
    };
    
    public override bool SupportsTranscription => true;
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
            
        var endpoint = $"{_config.BaseUrl.TrimEnd('/')}/text/chatcompletion_v2";
        
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
        
        var content = root.GetProperty("choices")[0].GetProperty("messages")[0].GetProperty("text").GetString() ?? "";
        
        return new ChatResponse
        {
            Content = content,
            Model = request.Model ?? _config.Model,
            TokensUsed = 0
        };
    }
}
