namespace MeetingAI.Core.Models;

public class MeetingRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string AudioFilePath { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public DateTime SavedAt { get; set; }
    public Transcript? Transcript { get; set; }
    public Summary? Summary { get; set; }
    public RecordingStatus Status { get; set; } = RecordingStatus.Pending;
}

public enum RecordingStatus
{
    Pending,
    Recording,
    Paused,
    Completed,
    Transcribing,
    Summarizing,
    Failed
}
