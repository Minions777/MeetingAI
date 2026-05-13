using FluentAssertions;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Core.Providers.Implementations;
using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests.Providers;

public class BaseAIProviderTests
{
    [Fact]
    public void OpenAIProvider_DefaultConfig_IsNotConfigured()
    {
        var provider = new OpenAIProvider();
        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void OpenAIProvider_AfterConfigure_IsConfigured()
    {
        var provider = new OpenAIProvider();
        var config = new ProviderConfig
        {
            Id = "test-openai",
            ApiKey = "sk-test-key",
            BaseUrl = "https://api.openai.com/v1",
            Model = "gpt-4o-mini"
        };
        provider.Configure(config);
        provider.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void OpenAIProvider_SupportsBothTranscriptionAndChat()
    {
        var provider = new OpenAIProvider();
        provider.SupportsTranscription.Should().BeTrue();
        provider.SupportsChat.Should().BeTrue();
    }

    [Fact]
    public void DeepSeekProvider_SupportsChatOnly()
    {
        var provider = new DeepSeekProvider();
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeFalse();
    }

    [Fact]
    public void AnthropicProvider_SupportsChatOnly()
    {
        var provider = new AnthropicProvider();
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeFalse();
    }

    [Fact]
    public void OllamaProvider_SupportsBoth()
    {
        var provider = new OllamaProvider();
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeTrue();
    }

    [Fact]
    public void ZhipuProvider_SupportsChatOnly()
    {
        var provider = new ZhipuProvider();
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeFalse();
    }

    [Fact]
    public void MiniMaxProvider_SupportsBoth()
    {
        var provider = new MiniMaxProvider();
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsTranscription.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnection_WithoutConfig_ReturnsIsConfigured()
    {
        var provider = new OpenAIProvider();
        var result = await provider.TestConnectionAsync();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ChatAsync_UnimplementedProvider_ThrowsNotSupported()
    {
        var provider = new TestOnlyTranscriptionProvider();
        await provider.Invoking(p => p.ChatAsync(new MeetingAI.Core.Models.ChatRequest()))
            .Should().ThrowAsync<NotSupportedException>();
    }

    private sealed class TestOnlyTranscriptionProvider : BaseAIProvider
    {
        public override string Id => "test";
        public override string Name => "Test";
        public override AIProviderType ProviderType => AIProviderType.Custom;
        public override IReadOnlyList<string> SupportedChatModels => Array.Empty<string>();
        public override IReadOnlyList<string> SupportedTranscriptionModels => Array.Empty<string>();
        public override bool SupportsTranscription => true;
        public override bool SupportsChat => false;
    }
}
