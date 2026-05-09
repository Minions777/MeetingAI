using System.Runtime.CompilerServices;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MeetingAI.Core.Resilience;

public class ResilientAiProvider : IAIProviderWrapper
{
    private readonly IAIProvider _inner;
    private readonly ResiliencePipeline _pipeline;
    private readonly ResiliencePipeline<ChatResponse> _chatPipeline;

    public string ProviderName => _inner.Name;

    public ResilientAiProvider(IAIProvider inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutException>()
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15)
            })
            .Build();

        _chatPipeline = new ResiliencePipelineBuilder<ChatResponse>()
            .AddRetry(new RetryStrategyOptions<ChatResponse>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<ChatResponse>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutException>()
                    .HandleResult(r => !r.IsSuccess)
            })
            .Build();
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        try
        {
            return await _chatPipeline.ExecuteAsync(async token =>
            {
                return await _inner.ChatAsync(request, token);
            }, ct);
        }
        catch (Exception ex)
        {
            LoggerService.Error($"ResilientChat failed after retries for {ProviderName}", ex);
            return new ChatResponse
            {
                Content = string.Empty,
                FinishReason = "error",
                IsSuccess = false
            };
        }
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var chunk in _inner.StreamChatAsync(request, ct))
        {
            yield return chunk;
        }
    }
}