using MeetingAI.Core.Models;

namespace MeetingAI.Core.Services;

public enum MermaidChartType
{
    MindMap,
    Flowchart,
    Gantt,
    Pie
}

public sealed record MermaidRenderRequest(
    Summary Summary,
    MermaidChartType ChartType);

public sealed record MermaidRenderResult(
    string MermaidSyntax,
    MermaidChartType ChartType,
    string HtmlContent);

public interface IMermaidRendererService
{
    MermaidRenderResult Render(Summary summary);

    MermaidRenderResult Render(Summary summary, MermaidChartType chartType);

    MermaidChartType DetectChartType(Summary summary);

    string GenerateHtml(string mermaidSyntax);
}