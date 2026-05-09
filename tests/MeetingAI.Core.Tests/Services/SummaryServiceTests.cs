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
        SummaryService.DefaultSummaryPrompt.Should().Contain("会议助手");
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
}