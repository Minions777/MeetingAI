using System.Text;
using MeetingAI.Core.Models;

namespace MeetingAI.Core.Services;

public static class MermaidTemplateGenerator
{
    public static string GenerateMindMap(Summary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("mindmap");
        sb.AppendLine("  root((会议摘要))");

        if (!string.IsNullOrWhiteSpace(summary.Overview))
        {
            sb.AppendLine($"    会议概要");
            sb.AppendLine($"      {EscapeMermaid(summary.Overview)}");
        }

        if (summary.KeyPoints.Count > 0)
        {
            sb.AppendLine("    关键要点");
            foreach (var point in summary.KeyPoints)
            {
                sb.AppendLine($"      • {EscapeMermaid(point)}");
            }
        }

        if (summary.ActionItems.Count > 0)
        {
            sb.AppendLine("    行动项");
            foreach (var item in summary.ActionItems)
            {
                var assignee = string.IsNullOrWhiteSpace(item.Assignee) ? "" : $" [@{item.Assignee}]";
                sb.AppendLine($"      • {EscapeMermaid(item.Description)}{assignee}");
            }
        }

        if (summary.Decisions.Count > 0)
        {
            sb.AppendLine("    决策");
            foreach (var decision in summary.Decisions)
            {
                sb.AppendLine($"      ✓ {EscapeMermaid(decision)}");
            }
        }

        if (summary.Questions.Count > 0)
        {
            sb.AppendLine("    待解决问题");
            foreach (var q in summary.Questions)
            {
                sb.AppendLine($"      ? {EscapeMermaid(q)}");
            }
        }

        return sb.ToString();
    }

    public static string GenerateFlowchart(Summary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("flowchart TD");

        var nodeId = 1;
        var nodes = new Dictionary<string, string>();

        // Start node
        sb.AppendLine($"    Start([📋 会议开始])");
        nodes["start"] = "Start";

        // Overview node
        if (!string.IsNullOrWhiteSpace(summary.Overview))
        {
            var overviewId = $"A{nodeId++}";
            sb.AppendLine($"    {overviewId}(({EscapeMermaid(summary.Overview)}))");
            sb.AppendLine($"    Start --> {overviewId}");
        }

        // Decision flow if we have decisions
        if (summary.Decisions.Count > 0)
        {
            var decisionNodeId = $"D{nodeId++}";
            sb.AppendLine($"    {decisionNodeId}{{决策过程}}");
            sb.AppendLine($"    Start --> {decisionNodeId}");

            foreach (var decision in summary.Decisions)
            {
                var decisionText = EscapeMermaid(decision.Length > 50 ? decision[..47] + "..." : decision);
                sb.AppendLine($"    {decisionNodeId} --> D{nodeId}[✓ {decisionText}]");
            }
        }

        // Key points as process nodes
        if (summary.KeyPoints.Count > 0)
        {
            var pointsNodeId = $"P{nodeId++}";
            sb.AppendLine($"    {pointsNodeId}{{关键要点}}");

            var entryPoint = summary.Decisions.Count > 0 ? $"D{nodeId - 1}" : "Start";
            sb.AppendLine($"    {entryPoint} --> {pointsNodeId}");

            foreach (var point in summary.KeyPoints)
            {
                var pointText = EscapeMermaid(point.Length > 40 ? point[..37] + "..." : point);
                sb.AppendLine($"    {pointsNodeId} --> P{nodeId}[• {pointText}]");
            }
        }

        // Action items as process nodes
        if (summary.ActionItems.Count > 0)
        {
            var actionNodeId = $"A{nodeId++}";
            sb.AppendLine($"    {actionNodeId}{{行动项}}");

            var entryPoint = summary.KeyPoints.Count > 0 ? $"P{nodeId - 1}" : (summary.Decisions.Count > 0 ? $"D{nodeId - 1}" : "Start");
            sb.AppendLine($"    {entryPoint} --> {actionNodeId}");

            foreach (var item in summary.ActionItems)
            {
                var itemText = EscapeMermaid(item.Description.Length > 40 ? item.Description[..37] + "..." : item.Description);
                var assignee = string.IsNullOrWhiteSpace(item.Assignee) ? "" : $" @{item.Assignee}";
                sb.AppendLine($"    {actionNodeId} --> AI{nodeId}[• {itemText}{assignee}]");
            }
        }

        // End node
        sb.AppendLine($"    End([✅ 会议结束])");

        if (summary.ActionItems.Count > 0)
            sb.AppendLine($"    AI{nodeId - 1} --> End");
        else if (summary.KeyPoints.Count > 0)
            sb.AppendLine($"    P{nodeId - 1} --> End");
        else if (summary.Decisions.Count > 0)
            sb.AppendLine($"    D{nodeId - 1} --> End");
        else
            sb.AppendLine($"    Start --> End");

        return sb.ToString();
    }

    public static string GenerateGantt(Summary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("gantt");
        sb.AppendLine("    title 会议任务安排");
        sb.AppendLine("    dateFormat YYYY-MM-DD");

        var taskId = 1;
        foreach (var item in summary.ActionItems)
        {
            var assignee = string.IsNullOrWhiteSpace(item.Assignee) ? "未分配" : item.Assignee;
            var start = item.DueDate?.AddDays(-7).ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
            var end = item.DueDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");

            sb.AppendLine($"    section 任务{taskId}");
            sb.AppendLine($"    任务{taskId}: {start}, {end}");
            taskId++;
        }

        return sb.ToString();
    }

    public static string GeneratePie(Summary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("pie");
        sb.AppendLine("    title 会议内容分布");

        if (!string.IsNullOrWhiteSpace(summary.Overview))
        {
            sb.AppendLine($"    \"概要\": {Math.Min(summary.Overview.Length, 40)}");
        }

        if (summary.KeyPoints.Count > 0)
        {
            sb.AppendLine($"    \"要点 ({summary.KeyPoints.Count})\": {Math.Min(summary.KeyPoints.Count * 10, 40)}");
        }

        if (summary.ActionItems.Count > 0)
        {
            sb.AppendLine($"    \"行动项 ({summary.ActionItems.Count})\": {Math.Min(summary.ActionItems.Count * 15, 35)}");
        }

        if (summary.Decisions.Count > 0)
        {
            sb.AppendLine($"    \"决策 ({summary.Decisions.Count})\": {Math.Min(summary.Decisions.Count * 20, 30)}");
        }

        return sb.ToString();
    }

    public static MermaidChartType DetectFromContent(Summary summary)
    {
        var text = $"{summary.Overview} {string.Join(" ", summary.KeyPoints)} {string.Join(" ", summary.Decisions)}".ToLowerInvariant();

        var decisionKeywords = new[] { "决定", "决策", "方案", "选择", "结论", "最终", "投票", "通过", "批准",
            "decision", "decided", "chosen", "selected", "approved", "resolved" };
        if (decisionKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)))
            return MermaidChartType.Flowchart;

        var projectKeywords = new[] { "计划", "项目", "任务", "安排", "进度", "里程碑", "deadline", "截止",
            "project", "plan", "schedule", "milestone", "timeline", "roadmap" };
        if (projectKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)) && summary.ActionItems.Count > 2)
            return MermaidChartType.Gantt;

        return MermaidChartType.MindMap;
    }

    private static string EscapeMermaid(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text
            .Replace("\"", "'")
            .Replace("[", "(")
            .Replace("]", ")")
            .Replace("{", "(")
            .Replace("}", ")")
            .Replace("<", "(")
            .Replace(">", ")")
            .Replace("|", "-");
    }
}