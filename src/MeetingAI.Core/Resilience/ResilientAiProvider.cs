using System;
using System.Threading.Tasks;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;

namespace MeetingAI.Core.Resilience
{
    public class ResilientAiProvider : IAiProvider
    {
        private readonly IAiProvider _innerProvider;
        private readonly int _maxRetries;
        private readonly TimeSpan _baseDelay;

        public string ProviderName => _innerProvider.ProviderName;

        public ResilientAiProvider(IAiProvider innerProvider, int maxRetries = 3, TimeSpan? baseDelay = null)
        {
            _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
            _maxRetries = maxRetries;
            _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
        }

        public async Task<AIResponse> AnalyzeAsync(AnalysisRequest request)
        {
            Exception lastException = null;
            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    var response = await _innerProvider.AnalyzeAsync(request);
                    if (response.IsSuccess) return response;
                    if (attempt < _maxRetries)
                    {
                        await Task.Delay(CalculateDelay(attempt));
                        continue;
                    }
                    return response;
                }
                catch (TimeoutException ex)
                {
                    lastException = ex;
                    if (attempt < _maxRetries)
                    {
                        await Task.Delay(CalculateDelay(attempt));
                        continue;
                    }
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    if (attempt < _maxRetries)
                    {
                        await Task.Delay(CalculateDelay(attempt));
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    return new AIResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"分析失败: {ex.Message}",
                        Provider = ProviderName
                    };
                }
            }

            return new AIResponse
            {
                IsSuccess = false,
                ErrorMessage = $"分析失败，已重试 {_maxRetries} 次: {lastException?.Message}",
                Provider = ProviderName
            };
        }

        public async Task<IAsyncEnumerable<string>> StreamAnalyzeAsync(AnalysisRequest request)
        {
            return await _innerProvider.StreamAnalyzeAsync(request);
        }

        private TimeSpan CalculateDelay(int attempt)
        {
            var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
            var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, (int)delay.TotalMilliseconds / 2));
            return delay + jitter;
        }
    }
}