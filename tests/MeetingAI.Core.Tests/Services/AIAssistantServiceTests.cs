using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Core.Services;
using MeetingAI.Core.Tests.Helpers;
using MeetingAI.Shared.Configuration;
using Moq;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class AIAssistantServiceTests
{
    private readonly IConfigurationService _configService;
    private readonly ProviderManager _providerManager;

    public AIAssistantServiceTests()
    {
        _configService = TestHelpers.CreateMockConfigService();
        _providerManager = new ProviderManager(_configService);
    }

    [Fact]
    public void Constructor_WithValidConfig_DoesNotThrow()
    {
        var sut = new AIAssistantService(_configService, _providerManager);
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task AskAsync_WithNoProviders_ThrowsInvalidOperationException()
    {
        var emptyConfig = TestHelpers.CreateMockConfigService(new AppSettings());
        var emptyProviderManager = new ProviderManager(emptyConfig);
        var sut = new AIAssistantService(emptyConfig, emptyProviderManager);

        await sut.Invoking(s => s.AskAsync("test", "context").GetAsyncEnumerator().MoveNextAsync().AsTask())
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AskSingleAsync_WithNoProviders_ThrowsInvalidOperationException()
    {
        var emptyConfig = TestHelpers.CreateMockConfigService(new AppSettings());
        var emptyProviderManager = new ProviderManager(emptyConfig);
        var sut = new AIAssistantService(emptyConfig, emptyProviderManager);

        await sut.Invoking(s => s.AskSingleAsync("test", "context"))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
