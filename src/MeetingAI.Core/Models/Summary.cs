namespace MeetingAI.Core.Models;

public class Summary
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Overview { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
    public List<ActionItem> ActionItems { get; set; } = new();
    public List<string> Decisions { get; set; } = new();
    public List<string> Questions { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
