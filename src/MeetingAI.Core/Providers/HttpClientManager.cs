using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers;

/// <summary>
/// Manages shared HttpClient instances per provider configuration.
/// </summary>
public static class HttpClientManager
{
    private sealed record ClientEntry(HttpClient Client, string Signature);

    private static readonly ConcurrentDictionary<string, ClientEntry> _clients = new();
    private static readonly object _sync = new();
    private static readonly TimeSpan _disposeDelay = TimeSpan.FromMinutes(2);

    public static HttpClient GetOrCreateClient(
        string providerId,
        ProviderConfig config,
        Action<HttpClient>? configureClient = null)
    {
        var key = GetClientKey(providerId, config);
        var signature = CreateSignature(providerId, config);

        lock (_sync)
        {
            if (_clients.TryGetValue(key, out var existing) && existing.Signature == signature)
            {
                return existing.Client;
            }

            if (existing != null)
            {
                ScheduleDispose(existing.Client);
            }

            var client = CreateClient(config);
            configureClient?.Invoke(client);
            _clients[key] = new ClientEntry(client, signature);

            return client;
        }
    }

    public static void UpdateApiKey(string providerId, string apiKey)
    {
        foreach (var entry in FindEntries(providerId))
        {
            entry.Value.Client.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(apiKey)
                ? null
                : new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public static void RemoveClient(string providerId)
    {
        foreach (var entry in FindEntries(providerId).ToList())
        {
            if (_clients.TryRemove(entry.Key, out var removed))
            {
                ScheduleDispose(removed.Client);
            }
        }
    }

    public static void ClearAll()
    {
        foreach (var entry in _clients.Values)
        {
            entry.Client.Dispose();
        }

        _clients.Clear();
    }

    private static HttpClient CreateClient(ProviderConfig config)
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

        client.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(config.ApiKey)
            ? null
            : new AuthenticationHeaderValue("Bearer", config.ApiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    private static string GetClientKey(string providerId, ProviderConfig config)
    {
        return string.IsNullOrWhiteSpace(config.Id)
            ? providerId
            : $"{providerId}:{config.Id}";
    }

    private static string CreateSignature(string providerId, ProviderConfig config)
    {
        return string.Join(
            '|',
            providerId,
            config.ProviderType,
            config.BaseUrl,
            HashValue(config.ApiKey),
            config.TimeoutSeconds);
    }

    private static string HashValue(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static void ScheduleDispose(HttpClient client)
    {
        _ = DisposeLaterAsync(client);
    }

    private static async Task DisposeLaterAsync(HttpClient client)
    {
        try
        {
            await Task.Delay(_disposeDelay);
            client.Dispose();
        }
        catch
        {
            client.Dispose();
        }
    }

    private static IEnumerable<KeyValuePair<string, ClientEntry>> FindEntries(string providerId)
    {
        var prefix = $"{providerId}:";
        return _clients.Where(entry =>
            entry.Key == providerId ||
            entry.Key.StartsWith(prefix, StringComparison.Ordinal));
    }
}
