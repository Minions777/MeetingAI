using System;
using System.Collections.Generic;

namespace MeetingAI.Core.Models
{
    public class MeetingState
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RecordingStartedAt { get; set; }
        public DateTime? RecordingStoppedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public MeetingStatus Status { get; set; }
        public List<TranscriptionSegment> Transcriptions { get; set; } = new();
        public List<AiAnalysisResult> Analyses { get; set; } = new();
        public TimeSpan? RecordingDuration =>
            RecordingStartedAt.HasValue && RecordingStoppedAt.HasValue
                ? RecordingStoppedAt.Value - RecordingStartedAt.Value
                : null;
        public bool IsRecording => Status == MeetingStatus.Recording;
        public bool IsCompleted => Status == MeetingStatus.Completed;
    }

    public enum MeetingStatus
    {
        Created,
        Recording,
        Recorded,
        Analyzing,
        Completed,
        Archived
    }
}