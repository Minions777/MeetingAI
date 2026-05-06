using System;

namespace MeetingAI.Core.Models
{
    public class TranscriptionSegment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string MeetingId { get; set; }
        public string Text { get; set; }
        public string Speaker { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime CreatedAt { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public double? Confidence { get; set; }
    }
}