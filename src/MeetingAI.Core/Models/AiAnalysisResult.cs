using System;

namespace MeetingAI.Core.Models
{
    public class AiAnalysisResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string MeetingId { get; set; }
        public string Content { get; set; }
        public string ContentType { get; set; }
        public AnalysisType Type { get; set; }
        public string Provider { get; set; }
        public string Model { get; set; }
        public DateTime CreatedAt { get; set; }
        public TimeSpan? ProcessingDuration { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }

    public enum AnalysisType
    {
        Summary,
        ActionItems,
        KeyPoints,
        Sentiment,
        Custom
    }
}