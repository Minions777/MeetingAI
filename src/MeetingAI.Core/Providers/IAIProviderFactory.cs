using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers;

public interface IAIProviderFactory
{
    IAIProvider Create(AIProviderType type);
    IAIProvider Create(ProviderConfig config);
    IReadOnlyList<AIProviderType> SupportedTypes { get; }
}

public sealed class AIProviderFactory : IAIProviderFactory
{
    private readonly IConfigurationService _configService;

    public AIProviderFactory(IConfigurationService configService)
    {
        _configService = configService;
    }

    public IAIProvider Create(AIProviderType type)
    {
        var provider = ProviderFactory.Create(type);
        var settings = _configService.Load();
        var config = settings.Providers.FirstOrDefault(p => p.ProviderType == type && p.IsEnabled);
        if (config != null)
        {
            provider.Configure(config);
        }
        return provider;
    }

    public IAIProvider Create(ProviderConfig config)
    {
        var provider = ProviderFactory.Create(config);
        return provider;
    }

    public IReadOnlyList<AIProviderType> SupportedTypes => ProviderFactory.SupportedTypes;
}
