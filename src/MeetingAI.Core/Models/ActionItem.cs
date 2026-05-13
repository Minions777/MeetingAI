namespace MeetingAI.Core.Models;

public sealed record ActionItem(
    string Id,
    string Description,
    string? Assignee,
    DateTime? DueDate,
    TimeSpan? ReferencedTimestamp,
    Priority Priority,
    bool IsCompleted)
{
    public static ActionItem Create(string description, string? assignee = null, DateTime? dueDate = null,
        TimeSpan? referencedTimestamp = null, Priority priority = Priority.Medium)
        => new(Guid.NewGuid().ToString(), description, assignee, dueDate, referencedTimestamp, priority, false);
}

public enum Priority { Low, Medium, High, Critical }