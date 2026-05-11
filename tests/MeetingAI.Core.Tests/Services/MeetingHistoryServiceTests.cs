using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public sealed class MeetingHistoryServiceTests : IDisposable
{
    private readonly string _historyDirectory;
    private readonly MeetingHistoryService _sut;

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

    public void Dispose()
    {
        if (Directory.Exists(_historyDirectory))
        {
            Directory.Delete(_historyDirectory, recursive: true);
        }
    }

    private static MeetingRecord CreateRecord(string id, DateTime startedAt)
    {
        return new MeetingRecord
        {
            Id = id,
            Title = id,
            StartedAt = startedAt,
            EndedAt = startedAt.AddMinutes(30),
            Duration = TimeSpan.FromMinutes(30)
        };
    }
}
