using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public sealed class ActionItemExtractorTests
{
    [Fact]
    public void ExportToCsv_FormatsCorrectly()
    {
        var items = new List<ActionItem>
        {
            ActionItem.Create("完成方案设计", "李工", DateTime.UtcNow.AddDays(3), null, Priority.High),
            ActionItem.Create("提交周报", null, null, null, Priority.Medium),
            ActionItem.Create("Review PR #123, 涉及 Kubernetes 集群配置变更", "张总",
                DateTime.UtcNow.AddDays(7), null, Priority.Critical)
        };

        var csv = ActionItemExporter.ExportToCsv(items);

        csv.Should().Contain("完成方案设计");
        csv.Should().Contain("李工");
        csv.Should().Contain("High");
        csv.Should().Contain("提交周报");
        csv.Should().Contain("Review PR #123");
        csv.Should().Contain("Critical");
    }

    [Fact]
    public void ExportToCsv_EscapesCommasInDescription()
    {
        var items = new List<ActionItem>
        {
            ActionItem.Create("完成 API 设计, 包括 endpoint 文档", null, null, null, Priority.Medium)
        };

        var csv = ActionItemExporter.ExportToCsv(items);

        csv.Should().Contain("\"完成 API 设计, 包括 endpoint 文档\"");
    }

    [Fact]
    public void ExportToCsv_HandlesEmptyList()
    {
        var items = new List<ActionItem>();

        var csv = ActionItemExporter.ExportToCsv(items);

        csv.Should().Contain("Description,Assignee,DueDate,Priority,Completed,ReferencedTimestamp");
        csv.Split('\n').Should().HaveCount(2); // header + empty
    }

    [Fact]
    public void ActionItem_Create_SetsDefaultValues()
    {
        var item = ActionItem.Create("测试任务");

        item.Id.Should().NotBeNullOrEmpty();
        item.Description.Should().Be("测试任务");
        item.Assignee.Should().BeNull();
        item.DueDate.Should().BeNull();
        item.Priority.Should().Be(Priority.Medium);
        item.IsCompleted.Should().BeFalse();
        item.ReferencedTimestamp.Should().BeNull();
    }

    [Fact]
    public void ActionItem_Create_SetsAllProperties()
    {
        var ts = TimeSpan.FromMinutes(5);
        var due = DateTime.UtcNow.AddDays(1);

        var item = ActionItem.Create("完整测试", "王五", due, ts, Priority.High);

        item.Description.Should().Be("完整测试");
        item.Assignee.Should().Be("王五");
        item.DueDate.Should().Be(due);
        item.ReferencedTimestamp.Should().Be(ts);
        item.Priority.Should().Be(Priority.High);
    }
}