using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Providers.Implementations;

public abstract class BaseAIProvider : IAIProvider
{
    protected ProviderConfig? _config;
    protected HttpClient? _httpClient;
    
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract AIProviderType ProviderType { get; }
    public abstract IReadOnlyList<string> SupportedChatModels { get; }
    public abstract IReadOnlyList<string> SupportedTranscriptionModels { get; }
    public abstract bool SupportsTranscription { get; }
    public abstract bool SupportsChat { get; }
    
    public bool IsConfigured => _config != null && !string.IsNullOrEmpty(_config.ApiKey);
    
    public virtual void Configure(ProviderConfig config)
    {
        _config = config;
        _httpClient = CreateHttpClient();
        LoggerService.Info($"{Name} Provider configured");
    }
    
    protected abstract HttpClient CreateHttpClient();
    
    public virtual Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        throw new NotSupportedException($"{Name} does not support chat");
    }
    
    public virtual Task<Transcript> TranscribeAsync(AudioData audio, TranscriptionOptions? options = null, CancellationToken ct = default)
    {
        throw new NotSupportedException($"{Name} does not support transcription");
    }
    
    public virtual Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        return Task.FromResult(IsConfigured);
    }
    
    protected StringContent CreateJsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
    
    protected async Task<string> SendRequestAsync(HttpClient client, string endpoint, HttpContent content, CancellationToken ct)
    {
        var response = await client.PostAsync(endpoint, content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        
        if (!response.IsSuccessStatusCode)
        {
            LoggerService.Error($"{Name} API Error: {json}");
            throw new HttpRequestException($"API Error: {response.StatusCode}");
        }
        
        return json;
    }
}
