using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MeetingAI.Core.Models;

namespace MeetingAI.Core.Providers
{
    public interface IAiProvider
    {
        string ProviderName { get; }
        Task<AIResponse> AnalyzeAsync(AnalysisRequest request);
        Task<IAsyncEnumerable<string>> StreamAnalyzeAsync(AnalysisRequest request);
    }

    public class AnalysisRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = string.Empty;
        public AnalysisType AnalysisType { get; set; }
        public string SystemPrompt { get; set; } = string.Empty;
        public string UserPrompt { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class AIResponse
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public bool IsSuccess { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan? ProcessingDuration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}