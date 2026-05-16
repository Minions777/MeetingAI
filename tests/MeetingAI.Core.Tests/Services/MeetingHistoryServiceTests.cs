using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public sealed class MeetingHistoryServiceTests : IDisposable
{
    private readonly string _historyDirectory;
    private readonly IMeetingHistoryService _sut;

    public MeetingHistoryServiceTests()
    {
        _historyDirectory = Path.Combine(Path.GetTempPath(), "MeetingAI.Tests", Guid.NewGuid().ToString("N"));
        _sut = new MeetingHistoryService(_historyDirectory);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsOnlyRequestedNewestRecords()
    {
        var first = CreateRecord("first", DateTime.UtcNow.AddDays(-2));
        var second = CreateRecord("second", DateTime.UtcNow.AddDays(-1));
        var third = CreateRecord("third", DateTime.UtcNow);

        await _sut.SaveAsync(first);
        await _sut.SaveAsync(second);
        await _sut.SaveAsync(third);

        var recent = await _sut.GetRecentAsync(2);

        recent.Should().HaveCount(2);
        recent.Select(r => r.Id).Should().Equal("third", "second");
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsEmpty_WhenCountIsZero()
    {
        await _sut.SaveAsync(CreateRecord("record", DateTime.UtcNow));

        var recent = await _sut.GetRecentAsync(0);

        recent.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentAsync_UsesRecordTime_WhenFileWriteTimeIsOlder()
    {
        var oldRecord = CreateRecord("old", DateTime.UtcNow.AddDays(-10));
        var newRecord = CreateRecord("new", DateTime.UtcNow);

        await _sut.SaveAsync(oldRecord);
        await _sut.SaveAsync(newRecord);
        File.SetLastWriteTimeUtc(Path.Combine(_historyDirectory, "new.json"), DateTime.UtcNow.AddDays(-30));

        var recent = await _sut.GetRecentAsync(1);

        recent.Single().Id.Should().Be("new");
    }

    [Fact]
    public async Task SaveAsync_NormalizesRecordId_ToPreventPathTraversal()
    {
        var record = CreateRecord(@"..\outside", DateTime.UtcNow);

        await _sut.SaveAsync(record);

        var files = Directory.GetFiles(_historyDirectory, "*.json");

        files.Should().HaveCount(1);
        Path.GetFullPath(files[0]).Should().StartWith(Path.GetFullPath(_historyDirectory));
        File.Exists(Path.Combine(Directory.GetParent(_historyDirectory)!.FullName, "outside.json")).Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_PopulatesCache_OnFirstCall()
    {
        var record = CreateRecord("cached-record", DateTime.UtcNow);
        await _sut.SaveAsync(record);

        // First call loads from disk and caches
        var first = await _sut.GetAllAsync();
        first.Should().HaveCount(1);

        // Add another record directly to disk (bypassing service)
        var newRecord = CreateRecord("new-record", DateTime.UtcNow);
        newRecord.Id = "new-id";
        var json = System.Text.Json.JsonSerializer.Serialize(newRecord, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(_historyDirectory, "new-id.json"), json);

        // Second call should return cached result (still 1 record)
        var second = await _sut.GetAllAsync();
        second.Should().HaveCount(1); // Cache hit, new file not visible
    }

    [Fact]
    public async Task SaveAsync_InvalidatesCache_NewRecordVisibleAfterNextCall()
    {
        var first = CreateRecord("first-record", DateTime.UtcNow);
        await _sut.SaveAsync(first);

        var initial = await _sut.GetAllAsync();
        initial.Should().HaveCount(1);

        // Save via service — this invalidates cache
        var second = CreateRecord("second-record", DateTime.UtcNow);
        await _sut.SaveAsync(second);

        var after = await _sut.GetAllAsync();
        after.Should().HaveCount(2);
        after.Select(r => r.Id).Should().Contain("second-record");
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCache_DeletedRecordGoneAfterNextCall()
    {
        var first = CreateRecord("to-delete", DateTime.UtcNow);
        await _sut.SaveAsync(first);

        await _sut.GetAllAsync(); // Populate cache

        await _sut.DeleteAsync("to-delete");

        var after = await _sut.GetAllAsync();
        after.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ByTitle_ReturnsMatchingRecord()
    {
        await _sut.SaveAsync(CreateRecord("meeting-alpha", DateTime.UtcNow, title: "Alpha Project Review"));
        await _sut.SaveAsync(CreateRecord("meeting-beta", DateTime.UtcNow, title: "Beta Sprint Planning"));

        var results = await _sut.SearchAsync("Alpha");

        results.Should().HaveCount(1);
        results[0].Title.Should().Be("Alpha Project Review");
    }

    [Fact]
    public async Task SearchAsync_ByTranscriptText_ReturnsMatchingRecord()
    {
        var record = CreateRecord("with-transcript", DateTime.UtcNow);
        record.Transcript = new Transcript { Text = "We discussed the deployment pipeline" };
        await _sut.SaveAsync(record);

        var results = await _sut.SearchAsync("deployment");

        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_BySummaryOverview_ReturnsMatchingRecord()
    {
        var record = CreateRecord("with-summary", DateTime.UtcNow);
        record.Summary = new Summary { Overview = "Quarterly revenue review" };
        await _sut.SaveAsync(record);

        var results = await _sut.SearchAsync("revenue");

        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_ByKeyPoints_ReturnsMatchingRecord()
    {
        var record = CreateRecord("with-keypoints", DateTime.UtcNow);
        record.Summary = new Summary { KeyPoints = { "Migration to Kubernetes completed" } };
        await _sut.SaveAsync(record);

        var results = await _sut.SearchAsync("Kubernetes");

        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_EmptyKeyword_ReturnsAllRecords()
    {
        await _sut.SaveAsync(CreateRecord("rec-1", DateTime.UtcNow));
        await _sut.SaveAsync(CreateRecord("rec-2", DateTime.UtcNow));

        var results = await _sut.SearchAsync("");

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_NoMatch_ReturnsEmpty()
    {
        await _sut.SaveAsync(CreateRecord("rec-1", DateTime.UtcNow, title: "Design Review"));

        var results = await _sut.SearchAsync("nonexistent-term");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExportToMarkdown_ContainsExpectedSections()
    {
        var record = CreateRecord("export-md", DateTime.UtcNow, title: "Weekly Standup");
        record.Summary = new Summary
        {
            Overview = "Team sync on sprint progress",
            KeyPoints = { "Feature A done", "Feature B in progress" },
            Decisions = { "Extend deadline by 1 week" }
        };
        record.Transcript = new Transcript { Text = "Hello everyone, let's start." };
        await _sut.SaveAsync(record);

        var loaded = await _sut.LoadAsync("export-md");
        var md = _sut.ExportToMarkdown(loaded!);

        md.Should().Contain("# Weekly Standup");
        md.Should().Contain("## 基本信息");
        md.Should().Contain("## AI 摘要");
        md.Should().Contain("Team sync on sprint progress");
        md.Should().Contain("Feature A done");
        md.Should().Contain("## 转录文本");
        md.Should().Contain("Hello everyone");
    }

    [Fact]
    public async Task ExportToText_ContainsExpectedContent()
    {
        var record = CreateRecord("export-txt", DateTime.UtcNow, title: "Retrospective");
        record.Summary = new Summary
        {
            Overview = "Sprint 5 retrospective",
            KeyPoints = { "Good velocity", "Need better testing" }
        };
        await _sut.SaveAsync(record);

        var loaded = await _sut.LoadAsync("export-txt");
        var text = _sut.ExportToText(loaded!);

        text.Should().Contain("Retrospective");
        text.Should().Contain("日期:");
        text.Should().Contain("【概要】Sprint 5 retrospective");
        text.Should().Contain("【关键要点】");
        text.Should().Contain("1. Good velocity");
    }

    [Fact]
    public async Task GetStatsAsync_WithRecords_ReturnsCorrectStats()
    {
        var now = DateTime.UtcNow;
        await _sut.SaveAsync(CreateRecord("stats-1", now.AddDays(-2), duration: TimeSpan.FromMinutes(30)));
        await _sut.SaveAsync(CreateRecord("stats-2", now.AddDays(-1), duration: TimeSpan.FromMinutes(60)));
        await _sut.SaveAsync(CreateRecord("stats-3", now, duration: TimeSpan.FromMinutes(45)));

        var stats = await _sut.GetStatsAsync();

        stats.TotalRecords.Should().Be(3);
        stats.TotalDuration.Should().Be(TimeSpan.FromMinutes(135));
        stats.AverageDuration.Should().Be(TimeSpan.FromMinutes(45));
        stats.FirstMeeting.Should().BeCloseTo(now.AddDays(-2), TimeSpan.FromSeconds(5));
        stats.LastMeeting.Should().BeCloseTo(now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetStatsAsync_EmptyHistory_ReturnsZeroStats()
    {
        var stats = await _sut.GetStatsAsync();

        stats.TotalRecords.Should().Be(0);
        stats.TotalDuration.Should().Be(TimeSpan.Zero);
        stats.AverageDuration.Should().Be(TimeSpan.Zero);
        stats.FirstMeeting.Should().BeNull();
        stats.LastMeeting.Should().BeNull();
    }

    [Fact]
    public async Task GetByDateRangeAsync_FiltersCorrectly()
    {
        var now = DateTime.UtcNow;
        await _sut.SaveAsync(CreateRecord("old", now.AddDays(-10)));
        await _sut.SaveAsync(CreateRecord("recent", now.AddDays(-1)));
        await _sut.SaveAsync(CreateRecord("today", now));

        var results = await _sut.GetByDateRangeAsync(now.AddDays(-3), now.AddDays(1));

        results.Should().HaveCount(2);
        results.Select(r => r.Id).Should().Contain("recent");
        results.Select(r => r.Id).Should().Contain("today");
    }

    [Fact]
    public async Task GetByDateRangeAsync_NoMatch_ReturnsEmpty()
    {
        await _sut.SaveAsync(CreateRecord("old", DateTime.UtcNow.AddDays(-30)));

        var results = await _sut.GetByDateRangeAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        results.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_historyDirectory))
        {
            Directory.Delete(_historyDirectory, recursive: true);
        }
    }

    private static MeetingRecord CreateRecord(string id, DateTime startedAt,
        string? title = null, TimeSpan? duration = null)
    {
        var dur = duration ?? TimeSpan.FromMinutes(30);
        return new MeetingRecord
        {
            Id = id,
            Title = title ?? id,
            StartedAt = startedAt,
            EndedAt = startedAt.Add(dur),
            Duration = dur
        };
    }
}
