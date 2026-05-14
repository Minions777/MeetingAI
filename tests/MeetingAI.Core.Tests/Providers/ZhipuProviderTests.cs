using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Implementations;
using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests.Providers;

public class ZhipuProviderTests
{
    [Fact]
    public void Properties_ReturnsExpectedValues()
    {
        var provider = new ZhipuProvider();

        provider.Id.Should().Be("zhipu");
        provider.Name.Should().Be("智谱 AI");
        provider.ProviderType.Should().Be(AIProviderType.Zhipu);
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeFalse();
    }

    [Fact]
    public void SupportedChatModels_ContainsExpectedModels()
    {
        var provider = new ZhipuProvider();

        provider.SupportedChatModels.Should().Contain("glm-4");
        provider.SupportedChatModels.Should().Contain("glm-4-flash");
        provider.SupportedChatModels.Should().Contain("glm-4-plus");
    }

    [Fact]
    public void SupportedTranscriptionModels_IsEmpty()
    {
        var provider = new ZhipuProvider();
        provider.SupportedTranscriptionModels.Should().BeEmpty();
    }

    [Fact]
    public void IsConfigured_WithoutConfig_ReturnsFalse()
    {
        var provider = new ZhipuProvider();
        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Configure_WithValidConfig_SetsIsConfiguredTrue()
    {
        var provider = new ZhipuProvider();
        var config = new ProviderConfig
        {
            Id = "test-zhipu",
            Name = "Test Zhipu",
            ProviderType = AIProviderType.Zhipu,
            ApiKey = "test-key",
            BaseUrl = "https://open.bigmodel.cn/api/paas/v4",
            Model = "glm-4"
        };

        provider.Configure(config);

        provider.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void Configure_WithEmptyApiKey_SetsIsConfiguredFalse()
    {
        var provider = new ZhipuProvider();
        var config = new ProviderConfig
        {
            Id = "test-zhipu",
            Name = "Test Zhipu",
            ProviderType = AIProviderType.Zhipu,
            ApiKey = "",
            BaseUrl = "https://open.bigmodel.cn/api/paas/v4",
            Model = "glm-4"
        };

        provider.Configure(config);

        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task ChatAsync_WithoutConfiguration_ThrowsInvalidOperationException()
    {
        var provider = new ZhipuProvider();
        var request = new ChatRequest
        {
            Model = "glm-4",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Hello" }
            }
        };

        await provider.Invoking(p => p.ChatAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }
}
