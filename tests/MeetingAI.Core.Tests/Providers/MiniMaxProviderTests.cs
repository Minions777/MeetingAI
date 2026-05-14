using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Implementations;
using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests.Providers;

public class MiniMaxProviderTests
{
    [Fact]
    public void Properties_ReturnsExpectedValues()
    {
        var provider = new MiniMaxProvider();

        provider.Id.Should().Be("minimax");
        provider.Name.Should().Be("MiniMax");
        provider.ProviderType.Should().Be(AIProviderType.MiniMax);
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeTrue();
    }

    [Fact]
    public void SupportedChatModels_ContainsExpectedModels()
    {
        var provider = new MiniMaxProvider();

        provider.SupportedChatModels.Should().Contain("MiniMax-Text-01");
        provider.SupportedChatModels.Should().Contain("abab6.5s-chat");
    }

    [Fact]
    public void SupportedTranscriptionModels_ContainsExpectedModels()
    {
        var provider = new MiniMaxProvider();

        provider.SupportedTranscriptionModels.Should().Contain("speech-02-hd");
    }

    [Fact]
    public void IsConfigured_WithoutConfig_ReturnsFalse()
    {
        var provider = new MiniMaxProvider();
        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Configure_WithValidConfig_SetsIsConfiguredTrue()
    {
        var provider = new MiniMaxProvider();
        var config = new ProviderConfig
        {
            Id = "test-minimax",
            Name = "Test MiniMax",
            ProviderType = AIProviderType.MiniMax,
            ApiKey = "test-key",
            BaseUrl = "https://api.minimax.chat/v1",
            Model = "MiniMax-Text-01"
        };

        provider.Configure(config);

        provider.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void Configure_WithEmptyApiKey_SetsIsConfiguredFalse()
    {
        var provider = new MiniMaxProvider();
        var config = new ProviderConfig
        {
            Id = "test-minimax",
            Name = "Test MiniMax",
            ProviderType = AIProviderType.MiniMax,
            ApiKey = "",
            BaseUrl = "https://api.minimax.chat/v1",
            Model = "MiniMax-Text-01"
        };

        provider.Configure(config);

        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task ChatAsync_WithoutConfiguration_ThrowsInvalidOperationException()
    {
        var provider = new MiniMaxProvider();
        var request = new ChatRequest
        {
            Model = "MiniMax-Text-01",
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
