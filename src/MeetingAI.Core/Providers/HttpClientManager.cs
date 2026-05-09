using System.Collections.Concurrent;
using System.Net.Http.Headers;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers;

/// <summary>
/// HTTP 客户端管理器，避免套接字耗尽
/// 使用共享的 HttpClient 实例，通过配置不同的请求头来区分不同的 Provider
/// </summary>
public static class HttpClientManager
{
    private static readonly ConcurrentDictionary<string, HttpClient> _clients = new();
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(120);

    /// <summary>
    /// 获取或创建 HttpClient 实例
    /// </summary>
    /// <param name="providerId">Provider 标识</param>
    /// <param name="config">Provider 配置</param>
    /// <param name="configureClient">自定义配置委托</param>
    /// <returns>HttpClient 实例</returns>
    public static HttpClient GetOrCreateClient(string providerId, ProviderConfig config, Action<HttpClient>? configureClient = null)
    {
        return _clients.GetOrAdd(providerId, _ =>
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = 10
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 120)
            };

            // 设置默认请求头
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 允许自定义配置
            configureClient?.Invoke(client);

            return client;
        });
    }

    /// <summary>
    /// 更新 HttpClient 的 API Key
    /// </summary>
    public static void UpdateApiKey(string providerId, string apiKey)
    {
        if (_clients.TryGetValue(providerId, out var client))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    /// <summary>
    /// 移除指定的 HttpClient
    /// </summary>
    public static void RemoveClient(string providerId)
    {
        if (_clients.TryRemove(providerId, out var client))
        {
            client.Dispose();
        }
    }

    /// <summary>
    /// 清理所有 HttpClient
    /// </summary>
    public static void ClearAll()
    {
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }
        _clients.Clear();
    }
}