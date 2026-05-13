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

    public AIAssistantServiceTests()
    {
        _configService = TestHelpers.CreateMockConfigService();
    }

    [Fact]
    public void Constructor_WithValidConfig_DoesNotThrow()
    {
        var sut = new AIAssistantService(_configService);
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task AskAsync_WithNoProviders_ThrowsInvalidOperationException()
    {
        var emptyConfig = TestHelpers.CreateMockConfigService(new AppSettings());
        var sut = new AIAssistantService(emptyConfig);

        await sut.Invoking(s => s.AskAsync("test", "context").GetAsyncEnumerator().MoveNextAsync().AsTask())
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AskSingleAsync_WithNoProviders_ThrowsInvalidOperationException()
    {
        var emptyConfig = TestHelpers.CreateMockConfigService(new AppSettings());
        var sut = new AIAssistantService(emptyConfig);

        await sut.Invoking(s => s.AskSingleAsync("test", "context"))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var sut = new AIAssistantService(_configService);
        sut.Invoking(s => s.Dispose()).Should().NotThrow();
    }
}
