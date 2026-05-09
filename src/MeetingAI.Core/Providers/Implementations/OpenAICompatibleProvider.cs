using System.Runtime.CompilerServices;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers.Implementations;

/// <summary>
/// OpenAI 兼容 Provider 基类
/// 适用于使用 OpenAI API 格式的 Provider（如 DeepSeek、智谱、MiniMax 等）
/// </summary>
public abstract class OpenAICompatibleProvider : BaseAIProvider
{
    /// <summary>
    /// 获取聊天完成端点路径（子类可重写）
    /// </summary>
    protected virtual string ChatEndpoint => "/chat/completions";

    /// <summary>
    /// 解析聊天响应（子类可重写以处理不同的响应格式）
    /// </summary>
    protected virtual ChatResponse ParseChatResponse(string json, ChatRequest request)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        var tokens = root.TryGetProperty("usage", out var usage) 
            ? usage.GetProperty("total_tokens").GetInt32() 
            : 0;
            
        return new ChatResponse
        {
            Content = content,
            Model = request.Model ?? _config!.Model,
            TokensUsed = tokens,
            FinishReason = root.GetProperty("choices")[0].GetProperty("finish_reason").GetString() ?? "",
            IsSuccess = true
        };
    }

    public override async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        if (_httpClient == null || _config == null)
            throw new InvalidOperationException("Provider not configured");
            
        var endpoint = $"{_config.BaseUrl.TrimEnd('/')}{ChatEndpoint}";
        
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
        return ParseChatResponse(json, request);
    }

    public override async IAsyncEnumerable<string> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_httpClient == null || _config == null)
            throw new InvalidOperationException("Provider not configured");

        var endpoint = $"{_config.BaseUrl.TrimEnd('/')}{ChatEndpoint}";

        var body = new
        {
            model = request.Model ?? _config.Model,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt }
            }.Concat(request.Messages.Select(m => new { role = m.Role, content = m.Content })).ToArray(),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            stream = true
        };

        using var requestMsg = new HttpRequestMessage(HttpMethod.Post, endpoint);
        requestMsg.Content = CreateJsonContent(body);

        using var response = await _httpClient.SendAsync(requestMsg, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data: ") == true)
            {
                var data = line["data: ".Length..];
                if (data == "[DONE]") yield break;

                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices) &&
                    choices.ValueKind == JsonValueKind.Array &&
                    choices.GetArrayLength() > 0)
                {
                    var delta = choices[0];
                    if (delta.TryGetProperty("delta", out var deltaObj) &&
                        deltaObj.TryGetProperty("content", out var contentEl))
                    {
                        var content = contentEl.GetString();
                        if (!string.IsNullOrEmpty(content))
                            yield return content;
                    }
                }
            }
        }
    }
}