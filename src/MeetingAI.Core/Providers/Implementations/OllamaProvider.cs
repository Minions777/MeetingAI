using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

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

        var baseUrl = _config.BaseUrl.TrimEnd('/');
        // Ollama native API: /api/chat, OpenAI-compatible: /v1/chat/completions
        var endpoint = baseUrl.EndsWith("/v1")
            ? $"{baseUrl}/chat/completions"
            : $"{baseUrl}/api/chat";

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

    public override async IAsyncEnumerable<string> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_httpClient == null || _config == null)
            throw new InvalidOperationException("Provider not configured");

        var baseUrl = _config.BaseUrl.TrimEnd('/');
        // Ollama native API: /api/chat, OpenAI-compatible: /v1/chat/completions
        var endpoint = baseUrl.EndsWith("/v1")
            ? $"{baseUrl}/chat/completions"
            : $"{baseUrl}/api/chat";

        var body = new
        {
            model = request.Model ?? _config.Model,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt }
            }.Concat(request.Messages.Select(m => new { role = m.Role, content = m.Content })).ToArray(),
            stream = true
        };

        using var requestMsg = new HttpRequestMessage(HttpMethod.Post, endpoint);
        requestMsg.Content = CreateJsonContent(body);

        using var response = await _httpClient.SendAsync(requestMsg, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            LoggerService.Error($"{Name} Stream API Error: {errorBody}");
            throw new HttpRequestException($"API Error: {response.StatusCode}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(line);
            }
            catch (JsonException)
            {
                continue;
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("content", out var contentEl))
                {
                    var content = contentEl.GetString();
                    if (!string.IsNullOrEmpty(content))
                        yield return content;
                }

                if (root.TryGetProperty("done", out var doneEl) && doneEl.GetBoolean())
                    yield break;
            }
        }
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
