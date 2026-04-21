using System.Net.Http;
using System.Text;
using System.Text.Json;
using MeetingAI.Models;

namespace MeetingAI.Services;

public class AISummaryService
{
    private readonly HttpClient _httpClient;

    public AISummaryService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<string> SummarizeAsync(string transcript, AIModelConfig config)
    {
        var systemPrompt = @"你是一个专业的会议记录助手。请根据以下会议内容，生成一份结构化的会议纪要，包括：
1. 会议主题
2. 关键讨论点
3. 重要决策
4. 行动项（包含负责人和截止时间）
5. 下一步计划

请用简洁专业的语言输出。";

        string endpoint = config.Provider switch
        {
            AIProvider.OpenAI => $"{config.BaseUrl}/chat/completions",
            AIProvider.Claude => $"{config.BaseUrl}/messages",
            AIProvider.DeepSeek => $"{config.BaseUrl}/chat/completions",
            AIProvider.Gemini => $"{config.BaseUrl}/models/{config.Model}:generateContent?key={config.ApiKey}",
            AIProvider.Ollama => $"{config.BaseUrl}/chat",
            _ => throw new NotSupportedException()
        };

        return config.Provider switch
        {
            AIProvider.OpenAI or AIProvider.DeepSeek or AIProvider.Ollama 
                => await CallOpenAIFormatAsync(endpoint, transcript, systemPrompt, config),
            AIProvider.Claude => await CallClaudeFormatAsync(endpoint, transcript, systemPrompt, config),
            AIProvider.Gemini => await CallGeminiFormatAsync(endpoint, transcript, systemPrompt, config),
            _ => throw new NotSupportedException()
        };
    }

    private async Task<string> CallOpenAIFormatAsync(string endpoint, string transcript, string systemPrompt, AIModelConfig config)
    {
        var requestBody = new
        {
            model = config.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"以下是会议转录内容：\n\n{transcript}" }
            },
            temperature = config.Temperature,
            max_tokens = config.MaxTokens
        };

        return await SendRequestAsync(endpoint, requestBody, config.ApiKey);
    }

    private async Task<string> CallClaudeFormatAsync(string endpoint, string transcript, string systemPrompt, AIModelConfig config)
    {
        var requestBody = new
        {
            model = config.Model,
            max_tokens = config.MaxTokens,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = $"以下是会议转录内容：\n\n{transcript}" }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("x-api-key", config.ApiKey);
        content.Headers.Add("anthropic-version", "2023-06-01");

        var response = await _httpClient.PostAsync(endpoint, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
    }

    private async Task<string> CallGeminiFormatAsync(string endpoint, string transcript, string systemPrompt, AIModelConfig config)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = $"{systemPrompt}\n\n以下是会议转录内容：\n\n{transcript}" }
                    }
                }
            },
            generationConfig = new
            {
                temperature = config.Temperature,
                maxOutputTokens = config.MaxTokens
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("candidates")[0]
                                 .GetProperty("content").GetProperty("parts")[0]
                                 .GetProperty("text").GetString() ?? "";
    }

    private async Task<string> SendRequestAsync(string endpoint, object body, string apiKey)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        var response = await _httpClient.PostAsync(endpoint, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"AI API Error: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("choices", out _))
            return root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
      
        if (root.TryGetProperty("content", out _))
            return root.GetProperty("content").GetString() ?? "";

        return responseBody;
    }
}