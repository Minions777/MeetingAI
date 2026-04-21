using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeetingAI.Models;
using MeetingAI.Services;

namespace MeetingAI.Services;

public class AISummaryService : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    private const string DefaultSystemPrompt = "你是一个专业的会议记录助手。请根据以下会议内容，生成一份结构化的会议纪要，包括：1. 会议主题 2. 关键讨论点 3. 重要决策 4. 行动项（包含负责人和截止时间）5. 下一步计划。请用简洁专业的语言输出。";

    public AISummaryService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MeetingAI", "1.0"));
    }

    public async Task<string> SummarizeAsync(string transcript, AIModelConfig config)
    {
        ArgumentNullException.ThrowIfNull(transcript);
        ArgumentNullException.ThrowIfNull(config);
        if (string.IsNullOrWhiteSpace(transcript)) throw new ArgumentException("转录内容不能为空", nameof(transcript));

        var validationResult = config.Validate();
        if (!validationResult.IsValid) throw new ArgumentException("配置无效: " + validationResult.ErrorMessage);

        var endpoint = GetEndpoint(config);
        LoggerService.Info("调用 AI API: " + config.Provider + " - " + config.Model);

        return config.Provider switch
        {
            AIProvider.OpenAI or AIProvider.DeepSeek or AIProvider.Ollama or AIProvider.Zhipu or AIProvider.MiniMax => await CallOpenAIFormatAsync(endpoint, transcript, config),
            AIProvider.Anthropic => await CallClaudeFormatAsync(endpoint, transcript, config),
            _ => throw new NotSupportedException("不支持的 AI 提供商: " + config.Provider)
        };
    }

    private static string GetEndpoint(AIModelConfig config) => config.Provider switch
    {
        AIProvider.OpenAI or AIProvider.DeepSeek or AIProvider.Zhipu or AIProvider.MiniMax or AIProvider.Ollama => config.BaseUrl + "/chat/completions",
        AIProvider.Anthropic => config.BaseUrl + "/messages",
        _ => throw new NotSupportedException()
    };

    private async Task<string> CallOpenAIFormatAsync(string endpoint, string transcript, AIModelConfig config)
    {
        var systemPrompt = string.IsNullOrWhiteSpace(config.SystemPrompt) ? DefaultSystemPrompt : config.SystemPrompt;

        var requestBody = new
        {
            model = config.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = "以下是会议转录内容：\n\n" + transcript }
            },
            temperature = config.Temperature,
            max_tokens = config.MaxTokens,
            top_p = config.TopP
        };

        return await SendOpenAIRequestAsync(endpoint, requestBody, config.ApiKey, config.TimeoutSeconds);
    }

    private async Task<string> SendOpenAIRequestAsync(string endpoint, object body, string apiKey, int timeoutSeconds)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        LoggerService.Debug("请求体: " + json);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = content;
        if (!string.IsNullOrEmpty(apiKey)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            var response = await _httpClient.SendAsync(request, cts.Token);
            return await ParseOpenAIResponseAsync(response);
        }
        catch (OperationCanceledException) { throw new TimeoutException("请求超时（" + timeoutSeconds + "秒）"); }
    }

    private static async Task<string> ParseOpenAIResponseAsync(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            LoggerService.Error("API 请求失败: " + (int)response.StatusCode + " - " + responseBody);
            throw new HttpRequestException("AI API 请求失败 (" + (int)response.StatusCode + ")");
        }

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
            {
                var message = choices[0].GetProperty("message");
                if (message.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String)
                {
                    return contentProp.GetString() ?? "";
                }
            }

            if (root.TryGetProperty("content", out var contentDirect) && contentDirect.ValueKind == JsonValueKind.String)
            {
                return contentDirect.GetString() ?? "";
            }

            LoggerService.Warning("无法解析 API 响应，返回原始内容");
            return responseBody;
        }
        catch (JsonException ex)
        {
            LoggerService.Error("JSON 解析失败", ex);
            throw new InvalidOperationException("AI API 返回了无效的 JSON 响应", ex);
        }
    }

    private async Task<string> CallClaudeFormatAsync(string endpoint, string transcript, AIModelConfig config)
    {
        var systemPrompt = string.IsNullOrWhiteSpace(config.SystemPrompt) ? DefaultSystemPrompt : config.SystemPrompt;

        var requestBody = new
        {
            model = config.Model,
            max_tokens = config.MaxTokens,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = transcript } },
            temperature = config.Temperature,
            top_p = config.TopP
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("x-api-key", config.ApiKey);
        content.Headers.Add("anthropic-version", "2023-06-01");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(config.TimeoutSeconds));
        var response = await _httpClient.PostAsync(endpoint, content, cts.Token);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            LoggerService.Error("Claude API 请求失败: " + (int)response.StatusCode + " - " + responseBody);
            throw new HttpRequestException("Claude API 请求失败 (" + (int)response.StatusCode + ")");
        }

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.Array && contentProp.GetArrayLength() > 0)
            {
                var textBlock = contentProp[0];
                if (textBlock.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                {
                    return textProp.GetString() ?? "";
                }
            }

            LoggerService.Warning("无法解析 Claude 响应");
            return responseBody;
        }
        catch (JsonException ex)
        {
            LoggerService.Error("Claude 响应 JSON 解析失败", ex);
            throw new InvalidOperationException("Claude API 返回了无效的 JSON 响应", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        LoggerService.Info("AISummaryService 释放资源");
        _httpClient.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}