using FluentAssertions;
using MeetingAI.Core.Services;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class SummaryServiceParseTests
{
    [Fact]
    public void ParseSummaryResponse_NullContent_ReturnsDefaultOverview()
    {
        var result = SummaryService.ParseSummaryResponse(null!);
        result.Overview.Should().Be("[摘要生成失败：AI 返回内容为空]");
    }

    [Fact]
    public void ParseSummaryResponse_EmptyContent_ReturnsDefaultOverview()
    {
        var result = SummaryService.ParseSummaryResponse("");
        result.Overview.Should().Be("[摘要生成失败：AI 返回内容为空]");
    }

    [Fact]
    public void ParseSummaryResponse_WhitespaceContent_ReturnsDefaultOverview()
    {
        var result = SummaryService.ParseSummaryResponse("   ");
        result.Overview.Should().Be("[摘要生成失败：AI 返回内容为空]");
    }

    [Fact]
    public void ParseSummaryResponse_ChineseSections_ParsesCorrectly()
    {
        var content = @"**会议概要**: 讨论了项目进展
**关键要点**:
- 完成了第一阶段
- 需要优化性能
**行动项**:
- 张三负责测试
- 李四负责部署
**决议**: 下周二前完成";

        var result = SummaryService.ParseSummaryResponse(content);

        result.Overview.Should().Contain("讨论了项目进展");
        result.KeyPoints.Should().HaveCount(2);
        result.KeyPoints[0].Should().Contain("第一阶段");
        result.ActionItems.Should().HaveCount(2);
        result.Decisions.Should().HaveCount(1);
        result.Decisions[0].Should().Contain("下周二");
    }

    [Fact]
    public void ParseSummaryResponse_EnglishSections_ParsesCorrectly()
    {
        var content = @"Overview: This is a project review meeting
Key Points:
- Phase 1 completed
- Performance needs optimization
Action Items:
- Alice to write tests
- Bob to deploy
Decisions: Approved the plan";

        var result = SummaryService.ParseSummaryResponse(content);

        result.Overview.Should().Contain("project review");
        result.KeyPoints.Should().HaveCount(2);
        result.KeyPoints[0].Should().Contain("Phase 1");
        result.ActionItems.Should().HaveCount(2);
        result.Decisions.Should().HaveCount(1);
        result.Decisions[0].Should().Contain("Approved");
    }

    [Fact]
    public void ParseSummaryResponse_InlineSectionText_ExtractsCorrectly()
    {
        var content = "## 会议概要：讨论了Q2季度规划\n" +
                      "## 关键要点：完成了预算审批";

        var result = SummaryService.ParseSummaryResponse(content);

        result.Overview.Should().Contain("Q2季度规划");
        result.KeyPoints[0].Should().Contain("预算审批");
    }

    [Fact]
    public void ParseSummaryResponse_NoSections_FallsBackToRawContent()
    {
        var content = "这是一段普通的文本，没有结构化分段。";

        var result = SummaryService.ParseSummaryResponse(content);

        result.Should().NotBeNull();
        result.Overview.Should().Contain("普通的文本");
    }

    [Fact]
    public void ParseSummaryResponse_QuestionsSection_ParsesCorrectly()
    {
        var content = @"Overview: Discussion about architecture
Key Points: Need to evaluate options
待解决问题:
- Database scaling
- API versioning";

        var result = SummaryService.ParseSummaryResponse(content);

        result.Questions.Should().HaveCount(2);
        result.Questions[0].Should().Contain("Database");
    }

    [Fact]
    public void ParseSummaryResponse_LongContent_UsesFallback()
    {
        var content = new string('x', 500);
        var result = SummaryService.ParseSummaryResponse(content);
        result.Overview.Should().Be(content);
    }

    [Fact]
    public void ParseSummaryResponse_MarkdownFormatting_HandledCorrectly()
    {
        var content = @"### **1. Overview / 会议概要**
Key topics discussed.

### **2. Key Points / 关键要点**
- Point one
- Point two";

        var result = SummaryService.ParseSummaryResponse(content);

        result.Overview.Should().NotBeNullOrEmpty();
        result.KeyPoints.Should().HaveCount(2);
    }
}
