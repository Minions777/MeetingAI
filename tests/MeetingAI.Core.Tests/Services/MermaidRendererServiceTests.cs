using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class MermaidRendererServiceTests
{
    private readonly MermaidRendererService _sut = new();
    private readonly Summary _testSummary;
    private readonly Summary _discussionSummary;

    public MermaidRendererServiceTests()
    {
        _testSummary = new Summary
        {
            Overview = "Team discussed Q4 roadmap and resource allocation",
            KeyPoints = { "New feature pipeline established", "Budget approved for Q4", "Hiring plan finalized" },
            ActionItems =
            {
                ActionItem.Create("Draft Q4 roadmap document", "Alice"),
                ActionItem.Create("Submit budget proposal", "Bob", DateTime.UtcNow.AddDays(7))
            },
            Decisions = { "Adopt microservices architecture", "Migrate to cloud by EOY" },
            Questions = { "Timeline for legacy system deprecation?" }
        };

        _discussionSummary = new Summary
        {
            Overview = "General team discussion about office layout",
            KeyPoints = { "New desks arranged", "Parking situation discussed" }
        };
    }

    [Fact]
    public void Render_WithSummary_ReturnsResult()
    {
        var result = _sut.Render(_testSummary);
        result.Should().NotBeNull();
        result.MermaidSyntax.Should().NotBeNullOrEmpty();
        result.HtmlContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Render_WithDiscussionSummary_DefaultIsMindMap()
    {
        var result = _sut.Render(_discussionSummary);
        result.ChartType.Should().Be(MermaidChartType.MindMap);
        result.MermaidSyntax.Should().Contain("mindmap");
    }

    [Fact]
    public void Render_WithFlowchart_ReturnsFlowchartSyntax()
    {
        var result = _sut.Render(_testSummary, MermaidChartType.Flowchart);
        result.ChartType.Should().Be(MermaidChartType.Flowchart);
        result.MermaidSyntax.Should().Contain("flowchart");
    }

    [Fact]
    public void Render_WithGantt_ReturnsGanttSyntax()
    {
        var result = _sut.Render(_testSummary, MermaidChartType.Gantt);
        result.ChartType.Should().Be(MermaidChartType.Gantt);
        result.MermaidSyntax.Should().Contain("gantt");
    }

    [Fact]
    public void Render_WithPie_ReturnsPieSyntax()
    {
        var result = _sut.Render(_testSummary, MermaidChartType.Pie);
        result.ChartType.Should().Be(MermaidChartType.Pie);
        result.MermaidSyntax.Should().Contain("pie");
    }

    [Fact]
    public void DetectChartType_WithDecisionKeywords_ReturnsFlowchart()
    {
        var summary = new Summary
        {
            Overview = "Final decision on architecture approach",
            Decisions = { "Selected event-driven design" }
        };
        var result = _sut.DetectChartType(summary);
        result.Should().Be(MermaidChartType.Flowchart);
    }

    [Fact]
    public void DetectChartType_WithProjectKeywords_ReturnsGantt()
    {
        var summary = new Summary
        {
            Overview = "Project plan for Q1 release",
            KeyPoints = { "Milestone 1 due Jan", "Milestone 2 due Feb" },
            ActionItems =
            {
                ActionItem.Create("Task 1"),
                ActionItem.Create("Task 2"),
                ActionItem.Create("Task 3")
            }
        };
        var result = _sut.DetectChartType(summary);
        result.Should().Be(MermaidChartType.Gantt);
    }

    [Fact]
    public void DetectChartType_Discussion_ReturnsMindMap()
    {
        var summary = new Summary
        {
            Overview = "General discussion about team building",
            KeyPoints = { "Team morale is good" }
        };
        var result = _sut.DetectChartType(summary);
        result.Should().Be(MermaidChartType.MindMap);
    }

    [Fact]
    public void GenerateHtml_ReturnsValidHtml()
    {
        var html = _sut.GenerateHtml("graph TD; A-->B;");
        html.Should().Contain("mermaid");
        html.Should().Contain("<!DOCTYPE html>");
    }

    [Fact]
    public void Render_EmptySummary_ReturnsEmptyResult()
    {
        var emptySummary = new Summary();
        var result = _sut.Render(emptySummary);
        result.MermaidSyntax.Should().NotBeNullOrEmpty();
        result.HtmlContent.Should().NotBeNullOrEmpty();
    }
}
