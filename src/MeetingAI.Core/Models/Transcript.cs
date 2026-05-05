namespace MeetingAI.Core.Models;

public class Transcript
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public List<TranscriptSegment> Segments { get; set; } = new();
    public string Language { get; set; } = "zh";
    public double Duration { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TranscriptSegment
{
    public int Id { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
