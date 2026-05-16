using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;
using Polly;
using Polly.Retry;

namespace MeetingAI.Core.Providers.Abstractions;

public abstract class BaseAIProvider : IAIProvider, IDisposable
{
    protected ProviderConfig? _config;
    protected HttpClient? _httpClient;
    private bool _disposed;

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract AIProviderType ProviderType { get; }
    public abstract IReadOnlyList<string> SupportedChatModels { get; }
    public abstract IReadOnlyList<string> SupportedTranscriptionModels { get; }
    public abstract bool SupportsTranscription { get; }
    public abstract bool SupportsChat { get; }

    public bool IsConfigured => _config != null && !string.IsNullOrEmpty(_config.ApiKey);

    public virtual void Configure(ProviderConfig config)
    {
        _config = config;
        _httpClient = HttpClientManager.GetOrCreateClient(Id, config, ConfigureHttpClient);
        LoggerService.Info($"{Name} Provider configured");
    }

    protected virtual void ConfigureHttpClient(HttpClient client)
    {
    }

    public virtual Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        throw new NotSupportedException($"{Name} does not support chat");
    }

    public virtual async IAsyncEnumerable<string> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var response = await ChatAsync(request, ct);
        yield return response.Content;
    }

    public virtual Task<Transcript> TranscribeAsync(AudioData audio, TranscriptionOptions? options = null, CancellationToken ct = default)
    {
        throw new NotSupportedException($"{Name} does not support transcription");
    }

    public virtual Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        return Task.FromResult(IsConfigured);
    }

    protected StringContent CreateJsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    protected async Task<string> SendRequestAsync(HttpClient client, string endpoint, Func<HttpContent> createContent, CancellationToken ct)
    {
        var retryPolicy = CreateRetryPolicy();

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using var content = createContent();
            using var response = await client.PostAsync(endpoint, content, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                LoggerService.Error($"{Name} API Error: {json}");
                throw new HttpRequestException($"API Error: {response.StatusCode}");
            }

            return json;
        });
    }

    protected virtual AsyncRetryPolicy<string> CreateRetryPolicy()
    {
        return Policy<string>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    LoggerService.Warning($"{Name} API 调用失败，{timespan.TotalSeconds}秒后进行第{retryCount}次重试");
                });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // HttpClient is owned and managed by HttpClientManager — do not dispose here.
                _httpClient = null;
            }
            _disposed = true;
        }
    }
}
