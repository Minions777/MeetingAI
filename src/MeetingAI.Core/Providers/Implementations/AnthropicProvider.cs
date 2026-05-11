using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers.Implementations;

public class AnthropicProvider : BaseAIProvider
{
    public override string Id => "anthropic";
    public override string Name => "Anthropic";
    public override AIProviderType ProviderType => AIProviderType.Anthropic;
    
    public override IReadOnlyList<string> SupportedChatModels { get; } = new[]
    {
        "claude-3-5-sonnet-20241022", "claude-3-opus-20240229", "claude-3-sonnet-20240229"
    };
    
    public override IReadOnlyList<string> SupportedTranscriptionModels { get; } = Array.Empty<string>();
    
    public override bool SupportsTranscription => false;
    public override bool SupportsChat => true;
    
    protected override void ConfigureHttpClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }
    
    public override async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        if (_httpClient == null || _config == null)
            throw new InvalidOperationException("Provider not configured");
            
        var endpoint = "https://api.anthropic.com/v1/messages";
        
        var systemContent = string.IsNullOrEmpty(request.SystemPrompt) 
            ? "You is a professional meeting assistant." 
            : request.SystemPrompt;
            
        var body = new
        {
            model = request.Model ?? _config.Model,
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            system = systemContent,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
        };
        
        var json = await SendRequestAsync(
            _httpClient,
            endpoint,
            () =>
            {
                var content = CreateJsonContent(body);
                content.Headers.Add("anthropic-dangerous-direct-browser-access", "true");
                return content;
            },
            ct);
            
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        var responseContent = root.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
        
        return new ChatResponse
        {
            Content = responseContent,
            Model = request.Model ?? _config.Model,
            TokensUsed = root.GetProperty("usage").GetProperty("input_tokens").GetInt32() +
                        root.GetProperty("usage").GetProperty("output_tokens").GetInt32(),
            FinishReason = root.GetProperty("stop_reason").GetString() ?? "",
            IsSuccess = true
        };
    }
}
