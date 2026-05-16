using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class MermaidTemplateGeneratorTests
{
    private static Summary CreateFullSummary() => new()
    {
        Overview = "Team discussed Q4 roadmap",
        KeyPoints = { "New feature pipeline", "Budget approved" },
        ActionItems =
        {
            ActionItem.Create("Draft roadmap", "Alice"),
            ActionItem.Create("Submit budget", "Bob", DateTime.UtcNow.AddDays(7))
        },
        Decisions = { "Adopt microservices" },
        Questions = { "Timeline for migration?" }
    };

    [Fact]
    public void GenerateMindMap_WithFullSummary_ContainsAllSections()
    {
        var summary = CreateFullSummary();
        var result = MermaidTemplateGenerator.GenerateMindMap(summary);

        result.Should().Contain("mindmap");
        result.Should().Contain("会议摘要");
        result.Should().Contain("会议概要");
        result.Should().Contain("关键要点");
        result.Should().Contain("行动项");
        result.Should().Contain("决策");
        result.Should().Contain("待解决问题");
    }

    [Fact]
    public void GenerateMindMap_EscapesSpecialCharacters()
    {
        var summary = new Summary { Overview = "Test [brackets] and {braces}" };
        var result = MermaidTemplateGenerator.GenerateMindMap(summary);

        result.Should().Contain("Test (brackets) and (braces)");
        result.Should().NotContain("[brackets]");
        result.Should().NotContain("{braces}");
    }

    [Fact]
    public void GenerateMindMap_EmptySummary_ProducesValidOutput()
    {
        var summary = new Summary();
        var result = MermaidTemplateGenerator.GenerateMindMap(summary);

        result.Should().Contain("mindmap");
        result.Should().Contain("会议摘要");
    }

    [Fact]
    public void GenerateFlowchart_WithDecisions_ContainsDecisionNodes()
    {
        var summary = new Summary
        {
            Overview = "Architecture review",
            Decisions = { "Use event-driven design", "Deploy to AWS" }
        };
        var result = MermaidTemplateGenerator.GenerateFlowchart(summary);

        result.Should().Contain("flowchart TD");
        result.Should().Contain("决策过程");
        result.Should().Contain("event-driven");
    }

    [Fact]
    public void GenerateFlowchart_WithActionItems_ShowsAssignees()
    {
        var summary = new Summary
        {
            ActionItems = { ActionItem.Create("Write tests", "Charlie") }
        };
        var result = MermaidTemplateGenerator.GenerateFlowchart(summary);

        result.Should().Contain("行动项");
        result.Should().Contain("Write tests");
        result.Should().Contain("@Charlie");
    }

    [Fact]
    public void GenerateFlowchart_EmptySummary_HasStartAndEnd()
    {
        var summary = new Summary();
        var result = MermaidTemplateGenerator.GenerateFlowchart(summary);

        result.Should().Contain("会议开始");
        result.Should().Contain("会议结束");
        result.Should().Contain("Start --> End");
    }

    [Fact]
    public void GenerateGantt_WithActionItemsAndDueDates_ProducesGanttChart()
    {
        var summary = new Summary
        {
            ActionItems =
            {
                ActionItem.Create("Task 1", "Alice", new DateTime(2026, 6, 1)),
                ActionItem.Create("Task 2", "Bob", new DateTime(2026, 6, 15))
            }
        };
        var result = MermaidTemplateGenerator.GenerateGantt(summary);

        result.Should().Contain("gantt");
        result.Should().Contain("会议任务安排");
        result.Should().Contain("任务1");
        result.Should().Contain("任务2");
        result.Should().Contain("2026-06-01");
        result.Should().Contain("2026-06-15");
    }

    [Fact]
    public void GeneratePie_WithContent_ProducesPieChart()
    {
        var summary = CreateFullSummary();
        var result = MermaidTemplateGenerator.GeneratePie(summary);

        result.Should().Contain("pie");
        result.Should().Contain("会议内容分布");
        result.Should().Contain("概要");
        result.Should().Contain("要点");
        result.Should().Contain("行动项");
        result.Should().Contain("决策");
    }

    [Fact]
    public void DetectFromContent_WithDecisionKeywords_ReturnsFlowchart()
    {
        var summary = new Summary
        {
            Overview = "We decided to use microservices architecture",
            Decisions = { "Approved the migration plan" }
        };

        var result = MermaidTemplateGenerator.DetectFromContent(summary);

        result.Should().Be(MermaidChartType.Flowchart);
    }

    [Fact]
    public void DetectFromContent_WithProjectKeywordsAndManyActions_ReturnsGantt()
    {
        var summary = new Summary
        {
            Overview = "Project timeline discussion",
            ActionItems =
            {
                ActionItem.Create("Task 1"),
                ActionItem.Create("Task 2"),
                ActionItem.Create("Task 3")
            }
        };

        var result = MermaidTemplateGenerator.DetectFromContent(summary);

        result.Should().Be(MermaidChartType.Gantt);
    }

    [Fact]
    public void DetectFromContent_WithProjectKeywordsButFewActions_ReturnsMindMap()
    {
        var summary = new Summary
        {
            Overview = "Project kickoff meeting",
            ActionItems = { ActionItem.Create("Task 1") }
        };

        var result = MermaidTemplateGenerator.DetectFromContent(summary);

        result.Should().Be(MermaidChartType.MindMap);
    }

    [Fact]
    public void DetectFromContent_GeneralDiscussion_ReturnsMindMap()
    {
        var summary = new Summary
        {
            Overview = "Team sync about office updates",
            KeyPoints = { "Morale is good" }
        };

        var result = MermaidTemplateGenerator.DetectFromContent(summary);

        result.Should().Be(MermaidChartType.MindMap);
    }

    [Fact]
    public void DetectFromContent_ChineseDecisionKeywords_ReturnsFlowchart()
    {
        var summary = new Summary
        {
            Overview = "最终决定采用新方案"
        };

        var result = MermaidTemplateGenerator.DetectFromContent(summary);

        result.Should().Be(MermaidChartType.Flowchart);
    }
}
