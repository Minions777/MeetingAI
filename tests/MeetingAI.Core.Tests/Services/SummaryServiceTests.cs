using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Services;
using MeetingAI.Core.Tests.Helpers;
using MeetingAI.Shared.Configuration;
using Moq;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class SummaryServiceTests
{
    private readonly IConfigurationService _configService;
    private readonly ProviderManager _providerManager;

    public SummaryServiceTests()
    {
        _configService = TestHelpers.CreateMockConfigService();
        _providerManager = new ProviderManager(_configService);
    }

    [Fact]
    public void Constructor_WithValidConfig_DoesNotThrow()
    {
        // Act
        var sut = new SummaryService(_configService, _providerManager);

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
        var mockConfig = new Mock<IConfigurationService>();
        mockConfig.Setup(x => x.Load()).Returns(TestHelpers.CreateTestSettings());
        var providerManager = new ProviderManager(mockConfig.Object);

        // Act
        var sut = new SummaryService(mockConfig.Object, providerManager);

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
        var providerManager = new ProviderManager(configService);
        var sut = new SummaryService(configService, providerManager);
        var transcript = new Transcript { Text = "test transcript" };

        // Act
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sut.SummarizeAsync(transcript, ct: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
