namespace MeetingAI.Core.Models
{
    public class AiAnalysisResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string MeetingId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public AnalysisType Type { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public TimeSpan? ProcessingDuration { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
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