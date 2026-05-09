using System.Runtime.CompilerServices;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Providers.Abstractions;

/// <summary>
/// AI Provider 基类，提供通用功能实现
/// </summary>
public abstract class BaseAIProvider : IAIProvider
{
    protected ProviderConfig? _config;
    protected HttpClient? _httpClient;
    
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract AIProviderType ProviderType { get; }
    public abstract IReadOnlyList<string> SupportedChatModels { get; }
    public abstract IReadOnlyList<string> SupportedTranscriptionModels { get; }
    
    public bool IsConfigured => !string.IsNullOrEmpty(_config?.ApiKey);
    public abstract bool SupportsTranscription { get; }
    public abstract bool SupportsChat { get; }
    
    public virtual void Configure(ProviderConfig config)
    {
        _config = config;
        _httpClient = CreateHttpClient();
    }
    
    /// <summary>
    /// 创建 HttpClient 实例
    /// </summary>
    protected virtual HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120)
        };
    }
    
    public abstract Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default);

    public virtual async IAsyncEnumerable<string> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Fallback to non-streaming
        var response = await ChatAsync(request, ct);
        yield return response.Content;
    }

    public abstract Task<Transcript> TranscribeAsync(AudioData audio, TranscriptionOptions? options = null, CancellationToken ct = default);
    
    /// <summary>
    /// 默认的连接测试实现
    /// 发送一个简单的测试请求来验证 API 连接
    /// </summary>
    public virtual async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        if (_config == null || !IsConfigured)
        {
            LoggerService.Warning($"{Name}: Provider 未配置");
            return false;
        }
        
        try
        {
            // 尝试发送一个最小的测试请求
            var testRequest = new ChatRequest
            {
                Model = _config.Model,
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "Hi" }
                },
                MaxTokens = 5
            };
            
            var response = await ChatAsync(testRequest, ct);
            var success = !string.IsNullOrEmpty(response.Content);
            
            LoggerService.Info($"{Name}: 连接测试 {(success ? "成功" : "失败")}");
            return success;
        }
        catch (Exception ex)
        {
            LoggerService.Error($"{Name}: 连接测试失败", ex);
            return false;
        }
    }
    
    /// <summary>
    /// 发送 HTTP 请求的辅助方法
    /// </summary>
    protected async Task<T> SendRequestAsync<T>(
        string url, 
        HttpMethod method, 
        object? body = null,
        CancellationToken ct = default) where T : class
    {
        if (_httpClient == null)
            throw new InvalidOperationException("HttpClient 未初始化");
            
        var request = new HttpRequestMessage(method, url);
        
        // 添加认证 Header
        AddAuthHeaders(request);
        
        // 添加内容
        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }
        
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(content) ?? throw new InvalidOperationException("响应解析失败");
    }
    
    /// <summary>
    /// 添加认证 Header（由子类重写）
    /// </summary>
    protected abstract void AddAuthHeaders(HttpRequestMessage request);
    
    /// <summary>
    /// 获取 API 基础 URL
    /// </summary>
    protected string GetBaseUrl()
    {
        return _config?.BaseUrl ?? GetDefaultBaseUrl();
    }
    
    /// <summary>
    /// 获取默认的 API 基础 URL（由子类实现）
    /// </summary>
    protected abstract string GetDefaultBaseUrl();
    
    /// <summary>
    /// 生成请求追踪 ID
    /// </summary>
    protected string GenerateTraceId()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }
}
