using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers.Implementations;

public class DeepSeekProvider : OpenAICompatibleProvider
{
    public override string Id => "deepseek";
    public override string Name => "DeepSeek";
    public override AIProviderType ProviderType => AIProviderType.DeepSeek;
    
    public override IReadOnlyList<string> SupportedChatModels { get; } = new[]
    {
        "deepseek-chat", "deepseek-coder"
    };
    
    public override IReadOnlyList<string> SupportedTranscriptionModels { get; } = Array.Empty<string>();
    
    public override bool SupportsTranscription => false;
    public override bool SupportsChat => true;
    
    protected override void ConfigureHttpClient(HttpClient client)
    {
        // DeepSeek 特定的配置（如果有）
    }
}
