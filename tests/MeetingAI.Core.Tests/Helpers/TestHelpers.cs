using MeetingAI.Shared.Configuration;
using Moq;

namespace MeetingAI.Core.Tests.Helpers;

public static class TestHelpers
{
    public static IConfigurationService CreateMockConfigService(AppSettings? settings = null)
    {
        var mock = new Mock<IConfigurationService>();
        mock.Setup(x => x.Load()).Returns(settings ?? CreateTestSettings());
        return mock.Object;
    }

    public static AppSettings CreateTestSettings()
    {
        return new AppSettings
        {
            Version = "1.0",
            DefaultProviderId = "test-provider",
            Providers = new List<ProviderConfig>
            {
                new ProviderConfig
                {
                    Id = "test-provider",
                    Name = "Test Provider",
                    ProviderType = AIProviderType.OpenAI,
                    ApiKey = "test-key",
                    BaseUrl = "https://api.test.com",
                    Model = "gpt-4o-mini",
                    IsEnabled = true,
                    Temperature = 0.7,
                    MaxTokens = 4096
                }
            }
        };
    }

    public static ProviderConfig CreateTestProviderConfig(AIProviderType type = AIProviderType.OpenAI)
    {
        return new ProviderConfig
        {
            Id = $"test-{type.ToString().ToLower()}",
            Name = $"Test {type} Provider",
            ProviderType = type,
            ApiKey = "test-key",
            BaseUrl = type == AIProviderType.DeepSeek
                ? "https://api.deepseek.com/v1"
                : "https://api.openai.com/v1",
            Model = type == AIProviderType.DeepSeek ? "deepseek-chat" : "gpt-4o-mini",
            IsEnabled = true,
            Temperature = 0.7,
            MaxTokens = 4096
        };
    }
}