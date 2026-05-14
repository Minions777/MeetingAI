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
        var sut = new SummaryService(_configService, _providerManager);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void DefaultSummaryPrompt_IsNotNullOrEmpty()
    {
        SummaryService.DefaultSummaryPrompt.Should().NotBeNullOrEmpty();
        SummaryService.DefaultSummaryPrompt.Should().Contain("meeting");
        SummaryService.DefaultSummaryPrompt.Should().Contain("Overview");
    }

    [Fact]
    public void Constructor_WithMockConfig_CreatesSuccessfully()
    {
        var mockConfig = new Mock<IConfigurationService>();
        mockConfig.Setup(x => x.Load()).Returns(TestHelpers.CreateTestSettings());
        var providerManager = new ProviderManager(mockConfig.Object);

        var sut = new SummaryService(mockConfig.Object, providerManager);

        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task SummarizeAsync_WithMissingDefaultProvider_FallsBackWithoutConfigurationLookupFailure()
    {
        var settings = TestHelpers.CreateTestSettings();
        settings.DefaultProviderId = "missing-provider";
        var configService = TestHelpers.CreateMockConfigService(settings);
        var providerManager = new ProviderManager(configService);
        var sut = new SummaryService(configService, providerManager);
        var transcript = new Transcript { Text = "test transcript" };

        var act = () => sut.SummarizeAsync(transcript);

        var ex = await Record.ExceptionAsync(act);
        ex.Should().NotBeNull();
        ex.Should().NotBeOfType<InvalidOperationException>();
    }
}
