using FluentAssertions;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Services;
using MeetingAI.Core.Tests.Helpers;
using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class TranscriptionServiceTests
{
    private readonly IConfigurationService _configService;
    private readonly ProviderManager _providerManager;

    public TranscriptionServiceTests()
    {
        _configService = TestHelpers.CreateMockConfigService();
        _providerManager = new ProviderManager(_configService);
    }

    [Fact]
    public void Constructor_WithValidConfig_DoesNotThrow()
    {
        // Act
        var sut = new TranscriptionService(_configService, _providerManager);

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task TranscribeAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var sut = new TranscriptionService(_configService, _providerManager);

        // Act & Assert
        await sut.Invoking(s => s.TranscribeAsync("nonexistent.wav"))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task TranscribeAsync_FileNotFound_ContainsFilePathInMessage()
    {
        // Arrange
        var sut = new TranscriptionService(_configService, _providerManager);
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