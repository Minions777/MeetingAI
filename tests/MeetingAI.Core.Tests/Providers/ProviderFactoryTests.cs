using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests.Providers;

public class ProviderFactoryTests
{
    [Theory]
    [InlineData(AIProviderType.OpenAI)]
    [InlineData(AIProviderType.DeepSeek)]
    [InlineData(AIProviderType.Anthropic)]
    [InlineData(AIProviderType.Ollama)]
    [InlineData(AIProviderType.MiniMax)]
    [InlineData(AIProviderType.Zhipu)]
    public void Create_WithValidType_ReturnsProvider(AIProviderType type)
    {
        // Act
        var provider = ProviderFactory.Create(type);

        // Assert
        provider.Should().NotBeNull();
        provider.ProviderType.Should().Be(type);
    }

    [Fact]
    public void Create_WithUnsupportedType_ThrowsNotSupportedException()
    {
        // Act & Assert
        var act = () => ProviderFactory.Create(AIProviderType.Custom);

        // Custom provider type is not supported and should throw
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Create_WithConfig_ConfiguresProvider()
    {
        // Arrange
        var config = new ProviderConfig
        {
            Id = "test",
            Name = "Test",
            ProviderType = AIProviderType.OpenAI,
            ApiKey = "test-key",
            BaseUrl = "https://api.openai.com/v1",
            Model = "gpt-4o-mini"
        };

        // Act
        var provider = ProviderFactory.Create(config);

        // Assert
        provider.IsConfigured.Should().BeTrue();
        provider.SupportsChat.Should().BeTrue();
    }

    [Fact]
    public void Create_OpenAIProvider_SupportsChatAndTranscription()
    {
        // Arrange & Act
        var provider = ProviderFactory.Create(AIProviderType.OpenAI);

        // Assert
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeTrue();
    }

    [Fact]
    public void Create_DeepSeekProvider_SupportsChatOnly()
    {
        // Arrange & Act
        var provider = ProviderFactory.Create(AIProviderType.DeepSeek);

        // Assert
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeFalse();
    }

    [Fact]
    public void SupportedTypes_ContainsExpectedProviders()
    {
        // Assert
        ProviderFactory.SupportedTypes.Should().Contain(AIProviderType.OpenAI);
        ProviderFactory.SupportedTypes.Should().Contain(AIProviderType.DeepSeek);
        ProviderFactory.SupportedTypes.Should().Contain(AIProviderType.Anthropic);
    }

    [Fact]
    public void Create_WithConfig_DeepSeek_ReturnsConfiguredProvider()
    {
        // Arrange
        var config = new ProviderConfig
        {
            Id = "deepseek-test",
            Name = "DeepSeek Test",
            ProviderType = AIProviderType.DeepSeek,
            ApiKey = "sk-test-key",
            BaseUrl = "https://api.deepseek.com/v1",
            Model = "deepseek-chat"
        };

        // Act
        var provider = ProviderFactory.Create(config);

        // Assert
        provider.Should().NotBeNull();
        provider.ProviderType.Should().Be(AIProviderType.DeepSeek);
        provider.IsConfigured.Should().BeTrue();
    }
}