namespace MeetingAI.Models;

public class TranscriptSegment
{
    public int Id { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Speaker { get; set; } = "Speaker 1";
    public string Text { get; set; } = "";
    public double Confidence { get; set; } = 1.0;
}