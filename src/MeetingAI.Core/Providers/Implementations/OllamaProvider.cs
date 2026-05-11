using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers.Implementations;

public class OllamaProvider : BaseAIProvider
{
    public override string Id => "ollama";
    public override string Name => "Ollama (本地)";
    public override AIProviderType ProviderType => AIProviderType.Ollama;
    
    public override IReadOnlyList<string> SupportedChatModels { get; } = new[]
    {
        "llama3.2", "qwen2.5", "deepseek-r1", "mistral"
    };
    
    public override IReadOnlyList<string> SupportedTranscriptionModels { get; } = new[]
    {
        "whisper-onnx"
    };
    
    public override bool SupportsTranscription => true;
    public override bool SupportsChat => true;
    
    protected override void ConfigureHttpClient(HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(_config?.TimeoutSeconds ?? 300);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
    
    public override async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        if (_httpClient == null || _config == null)
            throw new InvalidOperationException("Provider not configured");
            
        var endpoint = $"{_config.BaseUrl.TrimEnd('/')}/chat";
        
        var body = new
        {
            model = request.Model ?? _config.Model,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt }
            }.Concat(request.Messages.Select(m => new { role = m.Role, content = m.Content })).ToArray(),
            stream = false
        };
        
        var json = await SendRequestAsync(_httpClient, endpoint, () => CreateJsonContent(body), ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        var content = root.GetProperty("message").GetProperty("content").GetString() ?? "";
        
        return new ChatResponse
        {
            Content = content,
            Model = request.Model ?? _config.Model,
            TokensUsed = 0,
            IsSuccess = true
        };
    }

    public override async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        if (_httpClient == null || _config == null)
            return false;
            
        try
        {
            var response = await _httpClient.GetAsync($"{_config.BaseUrl.TrimEnd('/')}/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
