using System.Net.Http;
using System.Text;
using System.Text.Json;
using MeetingAI.Models;

namespace MeetingAI.Services;

public class AISummaryService : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    private const string SystemPrompt = @"你是一个专业的会议记录助手。请根据以下会议内容，生成一份结构化的会议纪要，包括：
1. 会议主题
2. 关键讨论点
3. 重要决策
4. 行动项（包含负责人和截止时间）
5. 下一步计划
请用简洁专业的语言输出。";

    public AISummaryService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public async Task<string> SummarizeAsync(string transcript, AIModelConfig config)
    {
        ArgumentNullException.ThrowIfNull(transcript);
        ArgumentNullException.ThrowIfNull(config);
        if (string.IsNullOrWhiteSpace(transcript))
            throw new ArgumentException("转录内容不能为空", nameof(transcript));

        var endpoint = GetEndpoint(config);
        return config.Provider switch
        {
            AIProvider.OpenAI or AIProvider.DeepSeek or AIProvider.Ollama
                => await CallOpenAIFormatAsync(endpoint, transcript, config),
            AIProvider.Claude => await CallClaudeFormatAsync(endpoint, transcript, config),
            AIProvider.Gemini => await CallGeminiFormatAsync(endpoint, transcript, config),
            _ => throw new NotSupportedException($"不支持的 AI 提供商: {config.Provider}")
        };
    }

    private static string GetEndpoint(AIModelConfig config) => config.Provider switch
    {
        AIProvider.OpenAI => $"{config.BaseUrl}/chat/completions",
        AIProvider.Claude => $"{config.BaseUrl}/messages",
        AIProvider.DeepSeek => $"{config.BaseUrl}/chat/completions",
        AIProvider.Gemini => $"{config.BaseUrl}/models/{config.Model}:generateContent",
        AIProvider.Ollama => $"{config.BaseUrl}/chat",
        _ => throw new NotSupportedException()
    };

    private async Task<string> CallOpenAIFormatAsync(string endpoint, string transcript, AIModelConfig config)
    {
        var requestBody = new
        {
            model = config.Model,
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"以下是会议转录内容：\n\n{transcript}" }
            },
            temperature = config.Temperature,
            max_tokens = config.MaxTokens
        };
        return await SendOpenAIRequestAsync(endpoint, requestBody, config.ApiKey);
    }

    private async Task<string> SendOpenAIRequestAsync(string endpoint, object body, string apiKey)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = content;
        if (!string.IsNullOrEmpty(apiKey))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.SendAsync(request);
        return await ParseResponseAsync(response);
    }

    private async Task<string> CallClaudeFormatAsync(string endpoint, string transcript, AIModelConfig config)
    {
        var requestBody = new
        {
            model = config.Model,
            max_tokens = config.MaxTokens,
            system = SystemPrompt,
            messages = new[] { new { role = "user", content = transcript } }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("x-api-key", config.ApiKey);
        content.Headers.Add("anthropic-version", "2023-06-01");

        var response = await _httpClient.PostAsync(endpoint, content);
        var responseBody = await ParseResponseAsync(response);

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
    }

    private async Task<string> CallGeminiFormatAsync(string endpoint, string transcript, AIModelConfig config)
    {
        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = $"{SystemPrompt}\n\n以下是会议转录内容：\n\n{transcript}" } } } },
            generationConfig = new { temperature = config.Temperature, maxOutputTokens = config.MaxTokens }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        var responseBody = await ParseResponseAsync(response);

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
    }

    private static async Task<string> ParseResponseAsync(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"AI API 请求失败 ({(int)response.StatusCode}): {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;
        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            return choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
            return content.GetString() ?? "";
        return responseBody;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
