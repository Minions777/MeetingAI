using FluentAssertions;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Implementations;
using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests.Providers;

public sealed class HttpClientManagerTests
{
    [Fact]
    public void GetOrCreateClient_UsesSeparateClients_ForDifferentProviderConfigs()
    {
        try
        {
            var first = new ProviderConfig
            {
                Id = "openai-a",
                ProviderType = AIProviderType.OpenAI,
                ApiKey = "key-a",
                BaseUrl = "https://api.example.com/v1"
            };
            var second = new ProviderConfig
            {
                Id = "openai-b",
                ProviderType = AIProviderType.OpenAI,
                ApiKey = "key-b",
                BaseUrl = "https://api.example.com/v1"
            };

            var firstClient = HttpClientManager.GetOrCreateClient("openai", first);
            var secondClient = HttpClientManager.GetOrCreateClient("openai", second);

            firstClient.Should().NotBeSameAs(secondClient);
            firstClient.DefaultRequestHeaders.Authorization!.Parameter.Should().Be("key-a");
            secondClient.DefaultRequestHeaders.Authorization!.Parameter.Should().Be("key-b");
        }
        finally
        {
            HttpClientManager.ClearAll();
        }
    }

    [Fact]
    public void ProviderDispose_DoesNotDisposeSharedHttpClient()
    {
        try
        {
            var config = new ProviderConfig
            {
                Id = "shared-openai",
                ProviderType = AIProviderType.OpenAI,
                ApiKey = "key",
                BaseUrl = "https://api.example.com/v1"
            };

            var provider = new OpenAIProvider();
            provider.Configure(config);
            provider.Dispose();

            var client = HttpClientManager.GetOrCreateClient("openai", config);
            var act = () => _ = client.Timeout;

            act.Should().NotThrow<ObjectDisposedException>();
        }
        finally
        {
            HttpClientManager.ClearAll();
        }
    }

    [Fact]
    public void GetOrCreateClient_ConfigChange_DoesNotDisposeOldClientImmediately()
    {
        try
        {
            var first = new ProviderConfig
            {
                Id = "shared-openai",
                ProviderType = AIProviderType.OpenAI,
                ApiKey = "key-a",
                BaseUrl = "https://api.example.com/v1"
            };
            var second = new ProviderConfig
            {
                Id = "shared-openai",
                ProviderType = AIProviderType.OpenAI,
                ApiKey = "key-b",
                BaseUrl = "https://api.example.com/v1"
            };

            var oldClient = HttpClientManager.GetOrCreateClient("openai", first);
            var newClient = HttpClientManager.GetOrCreateClient("openai", second);
            var act = () => _ = oldClient.Timeout;

            newClient.Should().NotBeSameAs(oldClient);
            act.Should().NotThrow<ObjectDisposedException>();
        }
        finally
        {
            HttpClientManager.ClearAll();
        }
    }
}
