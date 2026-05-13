using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using MeetingAI.Core.Tests.Helpers;
using MeetingAI.Shared.Configuration;
using NSubstitute;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class SummaryServiceTests
{
    private readonly IConfigurationService _configService;

    public SummaryServiceTests()
    {
        _configService = TestHelpers.CreateMockConfigService();
    }

    [Fact]
    public void Constructor_WithValidConfig_DoesNotThrow()
    {
        // Act
        var sut = new SummaryService(_configService);

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public void DefaultSummaryPrompt_IsNotNullOrEmpty()
    {
        // Assert
        SummaryService.DefaultSummaryPrompt.Should().NotBeNullOrEmpty();
        SummaryService.DefaultSummaryPrompt.Should().Contain("meeting");
        SummaryService.DefaultSummaryPrompt.Should().Contain("Overview");
    }

    [Fact]
    public void Constructor_WithMockConfig_CreatesSuccessfully()
    {
        // Arrange
        var mockConfig = Substitute.For<IConfigurationService>();
        mockConfig.Load().Returns(TestHelpers.CreateTestSettings());

        // Act
        var sut = new SummaryService(mockConfig);

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_WithMissingDefaultProvider_FallsBackWithoutConfigurationLookupFailure()
    {
        // Arrange
        var settings = TestHelpers.CreateTestSettings();
        settings.DefaultProviderId = "missing-provider";
        var configService = TestHelpers.CreateMockConfigService(settings);
        var sut = new SummaryService(configService);
        var transcript = new Transcript { Text = "test transcript" };

        // Act
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sut.SummarizeAsync(transcript, ct: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
