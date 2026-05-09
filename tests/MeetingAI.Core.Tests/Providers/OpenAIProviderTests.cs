using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Implementations;
using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests.Providers;

public class OpenAIProviderTests
{
    [Fact]
    public void Properties_ReturnsExpectedValues()
    {
        // Arrange
        var provider = new OpenAIProvider();

        // Assert
        provider.Id.Should().Be("openai");
        provider.Name.Should().Be("OpenAI");
        provider.ProviderType.Should().Be(AIProviderType.OpenAI);
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeTrue();
    }

    [Fact]
    public void SupportedChatModels_ContainsExpectedModels()
    {
        // Arrange
        var provider = new OpenAIProvider();

        // Assert
        provider.SupportedChatModels.Should().Contain("gpt-4o");
        provider.SupportedChatModels.Should().Contain("gpt-4o-mini");
        provider.SupportedChatModels.Should().Contain("gpt-4-turbo");
        provider.SupportedChatModels.Should().Contain("gpt-3.5-turbo");
    }

    [Fact]
    public void SupportedTranscriptionModels_ContainsWhisper()
    {
        // Arrange
        var provider = new OpenAIProvider();

        // Assert
        provider.SupportedTranscriptionModels.Should().Contain("whisper-1");
    }

    [Fact]
    public void IsConfigured_WithoutConfig_ReturnsFalse()
    {
        // Arrange
        var provider = new OpenAIProvider();

        // Assert
        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Configure_WithValidConfig_SetsIsConfiguredTrue()
    {
        // Arrange
        var provider = new OpenAIProvider();
        var config = new ProviderConfig
        {
            Id = "test-openai",
            Name = "Test OpenAI",
            ProviderType = AIProviderType.OpenAI,
            ApiKey = "sk-test-key",
            BaseUrl = "https://api.openai.com/v1",
            Model = "gpt-4o-mini"
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
        var provider = new OpenAIProvider();
        var config = new ProviderConfig
        {
            Id = "test-openai",
            Name = "Test OpenAI",
            ProviderType = AIProviderType.OpenAI,
            ApiKey = "",
            BaseUrl = "https://api.openai.com/v1",
            Model = "gpt-4o-mini"
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
        var provider = new OpenAIProvider();
        var request = new ChatRequest
        {
            Model = "gpt-4o-mini",
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

    [Fact]
    public async Task TranscribeAsync_WithoutConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = new OpenAIProvider();
        var audioData = new AudioData
        {
            Bytes = new byte[] { 0, 1, 2 },
            Format = "wav",
            SampleRate = 16000,
            Channels = 1,
            Duration = TimeSpan.FromSeconds(1)
        };

        // Act & Assert
        await provider.Invoking(p => p.TranscribeAsync(audioData))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }
}