using System.Text.Json.Serialization;

namespace MeetingAI.Shared.Configuration;

public class ProviderConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "默认配置";
    public AIProviderType ProviderType { get; set; } = AIProviderType.OpenAI;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public string WhisperModel { get; set; } = "whisper-1";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 0.9;
    public int TimeoutSeconds { get; set; } = 120;
    public string SystemPrompt { get; set; } = "你是一个专业的会议助手，负责总结会议内容、提取关键信息、生成结构化的会议报告。";
    public bool EnableThinking { get; set; } = false;
    public int ThinkingBudget { get; set; } = 1024;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Computed properties based on ProviderType
    [JsonIgnore]
    public bool SupportsChat => ProviderType switch
    {
        AIProviderType.OpenAI or AIProviderType.DeepSeek or AIProviderType.Anthropic 
          or AIProviderType.Ollama or AIProviderType.Zhipu or AIProviderType.MiniMax => true,
        _ => false
    };
    
    [JsonIgnore]
    public bool SupportsTranscription => ProviderType switch
    {
        AIProviderType.OpenAI or AIProviderType.Ollama or AIProviderType.MiniMax => true,
        _ => false
    };
    
    public static ProviderConfig CreateDefault(AIProviderType type)
    {
        return type switch
        {
            AIProviderType.OpenAI => new ProviderConfig
            {
                Name = "OpenAI - 默认",
                ProviderType = AIProviderType.OpenAI,
                BaseUrl = "https://api.openai.com/v1",
                Model = "gpt-4o-mini",
                WhisperModel = "whisper-1"
            },
            AIProviderType.DeepSeek => new ProviderConfig
            {
                Name = "DeepSeek - 默认",
                ProviderType = AIProviderType.DeepSeek,
                BaseUrl = "https://api.deepseek.com/v1",
                Model = "deepseek-chat",
                MaxTokens = 8192
            },
            AIProviderType.Anthropic => new ProviderConfig
            {
                Name = "Claude - 默认",
                ProviderType = AIProviderType.Anthropic,
                BaseUrl = "https://api.anthropic.com/v1",
                Model = "claude-3-5-sonnet-20241022"
            },
            AIProviderType.Ollama => new ProviderConfig
            {
                Name = "Ollama - 本地",
                ProviderType = AIProviderType.Ollama,
                BaseUrl = "http://localhost:11434/v1",
                Model = "llama3.2",
                WhisperModel = "whisper-onnx"
            },
            AIProviderType.Zhipu => new ProviderConfig
            {
                Name = "智谱 - 默认",
                ProviderType = AIProviderType.Zhipu,
                BaseUrl = "https://open.bigmodel.cn/api/paas/v4",
                Model = "glm-4"
            },
            AIProviderType.MiniMax => new ProviderConfig
            {
                Name = "MiniMax - 默认",
                ProviderType = AIProviderType.MiniMax,
                BaseUrl = "https://api.minimax.chat/v1",
                Model = "MiniMax-Text-01",
                MaxTokens = 8192
            },
            _ => new ProviderConfig()
        };
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AIProviderType
{
    OpenAI,
    Anthropic,
    DeepSeek,
    Ollama,
    Zhipu,
    MiniMax,
    Custom
}
