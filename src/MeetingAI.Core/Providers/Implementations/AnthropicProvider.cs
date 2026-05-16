using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

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
        client.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", "2023-06-01");

        // Anthropic uses x-api-key header, not Authorization: Bearer
        if (_config != null && !string.IsNullOrEmpty(_config.ApiKey))
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", _config.ApiKey);
        }
    }

    public override async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        if (_httpClient == null || _config == null)
            throw new InvalidOperationException("Provider not configured");

        var baseUrl = _config.BaseUrl.TrimEnd('/');
        var endpoint = baseUrl.EndsWith("/messages") ? baseUrl : $"{baseUrl}/messages";

        var systemContent = string.IsNullOrEmpty(request.SystemPrompt)
            ? "You are a professional meeting assistant."
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
            () => CreateJsonContent(body),
            ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var contentArray = root.GetProperty("content");
        var responseContent = contentArray.GetArrayLength() > 0
            ? contentArray[0].GetProperty("text").GetString() ?? ""
            : "";

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

    public override async IAsyncEnumerable<string> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_httpClient == null || _config == null)
            throw new InvalidOperationException("Provider not configured");

        var baseUrl = _config.BaseUrl.TrimEnd('/');
        var endpoint = baseUrl.EndsWith("/messages") ? baseUrl : $"{baseUrl}/messages";

        var systemContent = string.IsNullOrEmpty(request.SystemPrompt)
            ? "You are a professional meeting assistant."
            : request.SystemPrompt;

        var body = new
        {
            model = request.Model ?? _config.Model,
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            system = systemContent,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            stream = true
        };

        using var requestMsg = new HttpRequestMessage(HttpMethod.Post, endpoint);
        requestMsg.Content = CreateJsonContent(body);
        requestMsg.Headers.Add("x-api-key", _config.ApiKey);

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
            if (!line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            if (string.IsNullOrWhiteSpace(data)) continue;

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(data);
            }
            catch (JsonException)
            {
                continue;
            }

            using (doc)
            {
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeEl) &&
                    typeEl.GetString() == "content_block_delta" &&
                    root.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("text", out var textEl))
                {
                    var text = textEl.GetString();
                    if (!string.IsNullOrEmpty(text))
                        yield return text;
                }
            }
        }
    }
}
