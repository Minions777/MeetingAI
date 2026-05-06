using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Core.Providers.Implementations;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers;

public static class ProviderFactory
{
    private static readonly Dictionary<AIProviderType, Func<IAIProvider>> _providers = new()
    {
        [AIProviderType.OpenAI] = () => new OpenAIProvider(),
        [AIProviderType.DeepSeek] = () => new DeepSeekProvider(),
        [AIProviderType.Anthropic] = () => new AnthropicProvider(),
        [AIProviderType.Ollama] = () => new OllamaProvider(),
        [AIProviderType.MiniMax] = () => new MiniMaxProvider(),
        [AIProviderType.Zhipu] = () => new ZhipuProvider(),
    };
    
    public static IAIProvider Create(AIProviderType type)
    {
        if (_providers.TryGetValue(type, out var factory))
            return factory();
            
        throw new NotSupportedException($"Provider type {type} is not supported");
    }
    
    public static IAIProvider Create(ProviderConfig config)
    {
        var provider = Create(config.ProviderType);
        provider.Configure(config);
        return provider;
    }
    
    public static IReadOnlyList<AIProviderType> SupportedTypes => _providers.Keys.ToList();
}
