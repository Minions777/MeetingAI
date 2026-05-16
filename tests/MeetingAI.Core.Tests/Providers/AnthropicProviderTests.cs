using System.Net.Http;
using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Core.Providers.Implementations;
using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests.Providers;

public class AnthropicProviderTests
{
    [Fact]
    public void Properties_ReturnsExpectedValues()
    {
        var provider = new AnthropicProvider();

        provider.Id.Should().Be("anthropic");
        provider.Name.Should().Be("Anthropic");
        provider.ProviderType.Should().Be(AIProviderType.Anthropic);
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeFalse();
    }

    [Fact]
    public void SupportedChatModels_ContainsExpectedModels()
    {
        var provider = new AnthropicProvider();

        provider.SupportedChatModels.Should().Contain("claude-3-5-sonnet-20241022");
        provider.SupportedChatModels.Should().Contain("claude-3-opus-20240229");
    }

    [Fact]
    public void SupportedTranscriptionModels_IsEmpty()
    {
        var provider = new AnthropicProvider();
        provider.SupportedTranscriptionModels.Should().BeEmpty();
    }

    [Fact]
    public void IsConfigured_WithoutConfig_ReturnsFalse()
    {
        var provider = new AnthropicProvider();
        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Configure_WithValidConfig_SetsIsConfiguredTrue()
    {
        var provider = new AnthropicProvider();
        var config = new ProviderConfig
        {
            Id = "test-anthropic",
            Name = "Test Anthropic",
            ProviderType = AIProviderType.Anthropic,
            ApiKey = "sk-ant-test-key",
            BaseUrl = "https://api.anthropic.com/v1/messages",
            Model = "claude-3-5-sonnet-20241022"
        };

        provider.Configure(config);

        provider.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void Configure_WithEmptyApiKey_SetsIsConfiguredFalse()
    {
        var provider = new AnthropicProvider();
        var config = new ProviderConfig
        {
            Id = "test-anthropic",
            Name = "Test Anthropic",
            ProviderType = AIProviderType.Anthropic,
            ApiKey = "",
            BaseUrl = "https://api.anthropic.com/v1/messages",
            Model = "claude-3-5-sonnet-20241022"
        };

        provider.Configure(config);

        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Configure_SetsAnthropicVersionHeader()
    {
        var provider = new AnthropicProvider();
        var config = new ProviderConfig
        {
            Id = "test-anthropic-header",
            Name = "Test Anthropic",
            ProviderType = AIProviderType.Anthropic,
            ApiKey = "sk-ant-test-key",
            BaseUrl = "https://api.anthropic.com/v1/messages",
            Model = "claude-3-5-sonnet-20241022"
        };

        provider.Configure(config);

        // Access protected _httpClient field via reflection
        var httpClientField = typeof(BaseAIProvider).GetField("_httpClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var httpClient = (HttpClient?)httpClientField!.GetValue(provider);

        httpClient.Should().NotBeNull();
        httpClient!.DefaultRequestHeaders.Contains("anthropic-version").Should().BeTrue();
        httpClient.DefaultRequestHeaders.GetValues("anthropic-version").Should().Contain("2023-06-01");
    }

    [Fact]
    public async Task ChatAsync_WithoutConfiguration_ThrowsInvalidOperationException()
    {
        var provider = new AnthropicProvider();
        var request = new ChatRequest
        {
            Model = "claude-3-5-sonnet-20241022",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Hello" }
            }
        };

        await provider.Invoking(p => p.ChatAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task TestConnectionAsync_WithoutConfig_ReturnsFalse()
    {
        var provider = new AnthropicProvider();

        var result = await provider.TestConnectionAsync();

        result.Should().BeFalse();
    }
}
