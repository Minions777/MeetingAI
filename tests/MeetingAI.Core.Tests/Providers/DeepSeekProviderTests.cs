using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Implementations;
using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests.Providers;

public class DeepSeekProviderTests
{
    [Fact]
    public void Properties_ReturnsExpectedValues()
    {
        // Arrange
        var provider = new DeepSeekProvider();

        // Assert
        provider.Id.Should().Be("deepseek");
        provider.Name.Should().Be("DeepSeek");
        provider.ProviderType.Should().Be(AIProviderType.DeepSeek);
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeFalse();
    }

    [Fact]
    public void SupportedChatModels_ContainsExpectedModels()
    {
        // Arrange
        var provider = new DeepSeekProvider();

        // Assert
        provider.SupportedChatModels.Should().Contain("deepseek-chat");
        provider.SupportedChatModels.Should().Contain("deepseek-coder");
    }

    [Fact]
    public void SupportedTranscriptionModels_IsEmpty()
    {
        // Arrange
        var provider = new DeepSeekProvider();

        // Assert
        provider.SupportedTranscriptionModels.Should().BeEmpty();
    }

    [Fact]
    public void IsConfigured_WithoutConfig_ReturnsFalse()
    {
        // Arrange
        var provider = new DeepSeekProvider();

        // Assert
        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Configure_WithValidConfig_SetsIsConfiguredTrue()
    {
        // Arrange
        var provider = new DeepSeekProvider();
        var config = new ProviderConfig
        {
            Id = "test-deepseek",
            Name = "Test DeepSeek",
            ProviderType = AIProviderType.DeepSeek,
            ApiKey = "sk-test-key",
            BaseUrl = "https://api.deepseek.com/v1",
            Model = "deepseek-chat"
        };

        // Act
        provider.Configure(config);

        // Assert
        provider.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void Configure_WithEmptyApiKey_SetsIsConfiguredFalse()
    {
        // Arrange
        var provider = new DeepSeekProvider();
        var config = new ProviderConfig
        {
            Id = "test-deepseek",
            Name = "Test DeepSeek",
            ProviderType = AIProviderType.DeepSeek,
            ApiKey = "",
            BaseUrl = "https://api.deepseek.com/v1",
            Model = "deepseek-chat"
        };

        // Act
        provider.Configure(config);

        // Assert
        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task ChatAsync_WithoutConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = new DeepSeekProvider();
        var request = new ChatRequest
        {
            Model = "deepseek-chat",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Hello" }
            }
        };

        // Act & Assert
        await provider.Invoking(p => p.ChatAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }
}