using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers.Implementations;

public class MiniMaxProvider : OpenAICompatibleProvider
{
    public override string Id => "minimax";
    public override string Name => "MiniMax";
    public override AIProviderType ProviderType => AIProviderType.MiniMax;
    
    public override IReadOnlyList<string> SupportedChatModels { get; } = new[]
    {
        "MiniMax-Text-01", "abab6.5s-chat", "abab6.5-chat"
    };
    
    public override IReadOnlyList<string> SupportedTranscriptionModels { get; } = new[]
    {
        "speech-02-hd"
    };
    
    public override bool SupportsTranscription => true;
    public override bool SupportsChat => true;
    
    protected override string ChatEndpoint => "/text/chatcompletion_v2";
    
    protected override void ConfigureHttpClient(HttpClient client)
    {
        // MiniMax 特定的配置（如果有）
    }
    
    protected override ChatResponse ParseChatResponse(string json, ChatRequest request)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        // MiniMax 的响应格式略有不同
        var content = root.GetProperty("choices")[0].GetProperty("messages")[0].GetProperty("text").GetString() ?? "";
        
        return new ChatResponse
        {
            Content = content,
            Model = request.Model ?? _config!.Model,
            TokensUsed = 0,
            IsSuccess = true
        };
    }
}
