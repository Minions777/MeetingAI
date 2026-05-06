using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers.Implementations;

public class OpenAIProvider : BaseAIProvider
{
    public override string Id => "openai";
    public override string Name => "OpenAI";
    public override AIProviderType ProviderType => AIProviderType.OpenAI;
    
    public override IReadOnlyList<string> SupportedChatModels { get; } = new[]
    {
        "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo"
    };
    
    public override IReadOnlyList<string> SupportedTranscriptionModels { get; } = new[]
    {
        "whisper-1"
    };
    
    public override bool SupportsTranscription => true;
    public override bool SupportsChat => true;
    
    protected override HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(_config?.TimeoutSeconds ?? 120) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config?.ApiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
            top_p = request.TopP,
            max_tokens = request.MaxTokens
        };
        
        var json = await SendRequestAsync(_httpClient, endpoint, CreateJsonContent(body), ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        var tokens = root.TryGetProperty("usage", out var usage) 
            ? usage.GetProperty("total_tokens").GetInt32() 
            : 0;
            
        return new ChatResponse
        {
            Content = content,
            Model = request.Model ?? _config.Model,
            TokensUsed = tokens,
            FinishReason = root.GetProperty("choices")[0].GetProperty("finish_reason").GetString() ?? ""
        };
    }
    
    public override async Task<Transcript> TranscribeAsync(AudioData audio, TranscriptionOptions? options = null, CancellationToken ct = default)
    {
        if (_httpClient == null || _config == null)
            throw new InvalidOperationException("Provider not configured");
            
        var endpoint = $"{_config.BaseUrl.TrimEnd('/')}/audio/transcriptions";
        
        using var content = new MultipartFormDataContent();
        var audioContent = new ByteArrayContent(audio.Bytes);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(audioContent, "file", "recording.wav");
        content.Add(new StringContent(_config.WhisperModel), "model");
        
        if (options?.Language != null)
            content.Add(new StringContent(options.Language), "language");
            
        if (options?.Prompt != null)
            content.Add(new StringContent(options.Prompt), "prompt");
            
        var response = await _httpClient.PostAsync(endpoint, content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Whisper API Error: {response.StatusCode} - {json}");
            
        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement.GetProperty("text").GetString() ?? "";
        
        var transcript = new Transcript
        {
            Text = text,
            Language = options?.Language ?? "zh",
            Duration = audio.Duration
        };
        
        // Parse segments if available
        if (doc.RootElement.TryGetProperty("segments", out var segments))
        {
            foreach (var seg in segments.EnumerateArray())
            {
                transcript.Segments.Add(new TranscriptSegment
                {
                    Id = seg.GetProperty("id").GetInt32(),
                    Start = TimeSpan.FromSeconds(seg.GetProperty("start").GetDouble()),
                    End = TimeSpan.FromSeconds(seg.GetProperty("end").GetDouble()),
                    Text = seg.GetProperty("text").GetString() ?? "",
                    Confidence = seg.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 1.0
                });
            }
        }
        
        return transcript;
    }
}
