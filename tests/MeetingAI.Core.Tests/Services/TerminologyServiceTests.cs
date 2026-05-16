using System.Text.Json;
using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public sealed class TerminologyServiceTests : IDisposable
{
    private readonly TerminologyService _sut = new();
    private readonly string _tempDir;

    public TerminologyServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MeetingAI.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void GetDefault_Returns26Terms()
    {
        var db = _sut.GetDefault();

        db.Terms.Should().HaveCount(26);
    }

    [Fact]
    public void GetDefault_AllTermsHaveProtectFlag()
    {
        var db = _sut.GetDefault();

        db.Terms.Should().AllSatisfy(t => t.Protect.Should().BeTrue());
    }

    [Fact]
    public void GetDefault_ContainsExpectedTerms()
    {
        var db = _sut.GetDefault();

        db.Terms.Select(t => t.En).Should().Contain("Kubernetes");
        db.Terms.Select(t => t.En).Should().Contain("API");
        db.Terms.Select(t => t.En).Should().Contain("CI/CD");
        db.Terms.Select(t => t.En).Should().Contain("DevOps");
    }

    [Fact]
    public void FormatTerminologyList_WithTerms_ReturnsFormattedString()
    {
        var db = new TerminologyDb
        {
            Terms =
            {
                new Term { En = "API", Zh = "API", Protect = true },
                new Term { En = "SDK", Zh = "SDK", Protect = true }
            }
        };

        var result = _sut.FormatTerminologyList(db);

        result.Should().Be("API = API\nSDK = SDK");
    }

    [Fact]
    public void FormatTerminologyList_EmptyDb_ReturnsEmptyString()
    {
        var db = new TerminologyDb();

        var result = _sut.FormatTerminologyList(db);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadFromFileAsync_WithValidJson_ReturnsTerminologyDb()
    {
        var db = new TerminologyDb
        {
            Terms =
            {
                new Term { En = "GPU", Zh = "GPU", Protect = true },
                new Term { En = "RAM", Zh = "内存", Protect = false }
            }
        };
        var filePath = Path.Combine(_tempDir, "terms.json");
        var json = JsonSerializer.Serialize(db, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        await File.WriteAllTextAsync(filePath, json);

        var result = await _sut.LoadFromFileAsync(filePath);

        result.Should().NotBeNull();
        result!.Terms.Should().HaveCount(2);
        result.Terms[0].En.Should().Be("GPU");
        result.Terms[1].Zh.Should().Be("内存");
    }

    [Fact]
    public async Task LoadFromFileAsync_NonExistentFile_ReturnsNull()
    {
        var filePath = Path.Combine(_tempDir, "does-not-exist.json");

        var result = await _sut.LoadFromFileAsync(filePath);

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadFromFileAsync_InvalidJson_ReturnsNull()
    {
        var filePath = Path.Combine(_tempDir, "invalid.json");
        await File.WriteAllTextAsync(filePath, "{ this is not valid json !!!");

        var result = await _sut.LoadFromFileAsync(filePath);

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadFromFileAsync_EmptyJson_ReturnsNull()
    {
        var filePath = Path.Combine(_tempDir, "empty.json");
        await File.WriteAllTextAsync(filePath, "");

        var result = await _sut.LoadFromFileAsync(filePath);

        result.Should().BeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
