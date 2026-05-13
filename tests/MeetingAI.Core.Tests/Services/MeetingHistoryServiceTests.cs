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
