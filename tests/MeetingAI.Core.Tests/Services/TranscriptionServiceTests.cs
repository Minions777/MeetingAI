using FluentAssertions;
using MeetingAI.Core.Services;
using MeetingAI.Core.Tests.Helpers;
using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class TranscriptionServiceTests
{
    private readonly IConfigurationService _configService;

    public TranscriptionServiceTests()
    {
        _configService = TestHelpers.CreateMockConfigService();
    }

    [Fact]
    public void Constructor_WithValidConfig_DoesNotThrow()
    {
        // Act
        var sut = new TranscriptionService(_configService);

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task TranscribeAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var sut = new TranscriptionService(_configService);

        // Act & Assert
        await sut.Invoking(s => s.TranscribeAsync("nonexistent.wav"))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task TranscribeAsync_FileNotFound_ContainsFilePathInMessage()
    {
        // Arrange
        var sut = new TranscriptionService(_configService);
        var nonexistentPath = "C:\\nonexistent\\audio.wav";

        // Act & Assert
        try
        {
            await sut.TranscribeAsync(nonexistentPath);
            false.Should().BeTrue("Should have thrown exception");
        }
        catch (FileNotFoundException ex)
        {
            ex.FileName.Should().Contain("nonexistent");
        }
    }
}