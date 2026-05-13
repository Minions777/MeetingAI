using System.Text;
using MeetingAI.Core.Models;

namespace MeetingAI.Core.Services;

public static class ActionItemExporter
{
    public static string ExportToCsv(IReadOnlyList<ActionItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Description,Assignee,DueDate,Priority,Completed,ReferencedTimestamp");

        foreach (var item in items)
        {
            var description = EscapeCsv(item.Description);
            var assignee = EscapeCsv(item.Assignee ?? "");
            var dueDate = item.DueDate?.ToString("yyyy-MM-dd") ?? "";
            var priority = item.Priority.ToString();
            var completed = item.IsCompleted ? "Yes" : "No";
            var timestamp = item.ReferencedTimestamp?.ToString(@"hh\:mm\:ss") ?? "";

            sb.AppendLine($"{description},{assignee},{dueDate},{priority},{completed},{timestamp}");
        }

        return sb.ToString();
    }

    public static async Task ExportToFileAsync(IReadOnlyList<ActionItem> items, string filePath)
    {
        var csv = ExportToCsv(items);
        await File.WriteAllTextAsync(filePath, csv, Encoding.UTF8);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}