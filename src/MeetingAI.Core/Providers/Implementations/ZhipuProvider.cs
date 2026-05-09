using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers.Implementations;

public class ZhipuProvider : OpenAICompatibleProvider
{
    public override string Id => "zhipu";
    public override string Name => "智谱 AI";
    public override AIProviderType ProviderType => AIProviderType.Zhipu;
    
    public override IReadOnlyList<string> SupportedChatModels { get; } = new[]
    {
        "glm-4", "glm-4-flash", "glm-4-plus", "glm-3-turbo"
    };
    
    public override IReadOnlyList<string> SupportedTranscriptionModels { get; } = Array.Empty<string>();
    
    public override bool SupportsTranscription => false;
    public override bool SupportsChat => true;
    
    protected override void ConfigureHttpClient(HttpClient client)
    {
        // 智谱 AI 特定的配置（如果有）
    }
}
