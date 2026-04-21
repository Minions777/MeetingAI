namespace MeetingAI.Models;

public class MeetingRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public TimeSpan Duration { get; set; }
    public string AudioPath { get; set; } = "";
    public List<TranscriptSegment> Segments { get; set; } = new();
    public string Transcript { get; set; } = "";
    public string Summary { get; set; } = "";
    public AIModelConfig? UsedConfig { get; set; }
}