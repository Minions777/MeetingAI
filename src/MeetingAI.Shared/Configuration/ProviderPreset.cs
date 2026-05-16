namespace MeetingAI.Shared.Configuration;

/// <summary>
/// Provider 预设配置
/// 为每个主流厂商提供默认 URL 和可选模型列表
/// </summary>
public class ProviderPreset
{
    public AIProviderType ProviderType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultUrl { get; set; } = string.Empty;
    public List<string> ChatModels { get; set; } = new();
    public List<string> WhisperModels { get; set; } = new();
    public string DefaultChatModel { get; set; } = string.Empty;
    public string DefaultWhisperModel { get; set; } = string.Empty;
    public bool RequiresApiKey { get; set; } = true;

    /// <summary>
    /// 获取所有内置预设
    /// </summary>
    public static List<ProviderPreset> GetAll()
    {
        return new List<ProviderPreset>
        {
            // OpenAI
            new ProviderPreset
            {
                ProviderType = AIProviderType.OpenAI,
                DisplayName = "OpenAI",
                Description = "GPT-4o / GPT-4 / Whisper 语音转录",
                DefaultUrl = "https://api.openai.com/v1",
                ChatModels = new List<string>
                {
                    "gpt-4o",
                    "gpt-4o-mini",
                    "gpt-4-turbo",
                    "gpt-4",
                    "gpt-3.5-turbo",
                    "o1-preview",
                    "o1-mini"
                },
                WhisperModels = new List<string>
                {
                    "whisper-1"
                },
                DefaultChatModel = "gpt-4o-mini",
                DefaultWhisperModel = "whisper-1"
            },

            // DeepSeek
            new ProviderPreset
            {
                ProviderType = AIProviderType.DeepSeek,
                DisplayName = "DeepSeek",
                Description = "DeepSeek-V3 / DeepSeek-R1 推理模型",
                DefaultUrl = "https://api.deepseek.com/v1",
                ChatModels = new List<string>
                {
                    "deepseek-chat",
                    "deepseek-reasoner"
                },
                WhisperModels = new List<string>(),
                DefaultChatModel = "deepseek-chat"
            },

            // Anthropic (Claude)
            new ProviderPreset
            {
                ProviderType = AIProviderType.Anthropic,
                DisplayName = "Claude (Anthropic)",
                Description = "Claude 3.5 Sonnet / Claude 3 Opus",
                DefaultUrl = "https://api.anthropic.com/v1",
                ChatModels = new List<string>
                {
                    "claude-sonnet-4-20250514",
                    "claude-3-5-sonnet-20241022",
                    "claude-3-5-haiku-20241022",
                    "claude-3-opus-20240229"
                },
                WhisperModels = new List<string>(),
                DefaultChatModel = "claude-3-5-sonnet-20241022"
            },

            // 智谱 (Zhipu)
            new ProviderPreset
            {
                ProviderType = AIProviderType.Zhipu,
                DisplayName = "智谱 AI",
                Description = "GLM-4 系列模型",
                DefaultUrl = "https://open.bigmodel.cn/api/paas/v4",
                ChatModels = new List<string>
                {
                    "glm-4-plus",
                    "glm-4",
                    "glm-4-flash",
                    "glm-4-long",
                    "glm-4-air",
                    "glm-4-airx",
                    "glm-4v-plus",
                    "glm-4v"
                },
                WhisperModels = new List<string>(),
                DefaultChatModel = "glm-4-flash"
            },

            // MiniMax
            new ProviderPreset
            {
                ProviderType = AIProviderType.MiniMax,
                DisplayName = "MiniMax",
                Description = "MiniMax-Text / 语音转录",
                DefaultUrl = "https://api.minimax.chat/v1",
                ChatModels = new List<string>
                {
                    "MiniMax-Text-01",
                    "abab6.5s-chat",
                    "abab6.5-chat",
                    "abab5.5-chat"
                },
                WhisperModels = new List<string>
                {
                    "whisper-1"
                },
                DefaultChatModel = "MiniMax-Text-01",
                DefaultWhisperModel = "whisper-1"
            },

            // Ollama (本地)
            new ProviderPreset
            {
                ProviderType = AIProviderType.Ollama,
                DisplayName = "Ollama (本地)",
                Description = "本地部署的开源模型，无需 API Key",
                DefaultUrl = "http://localhost:11434/v1",
                ChatModels = new List<string>
                {
                    "llama3.2",
                    "llama3.1",
                    "llama3",
                    "qwen2.5",
                    "qwen2",
                    "deepseek-r1",
                    "mistral",
                    "mixtral",
                    "phi3",
                    "gemma2"
                },
                WhisperModels = new List<string>
                {
                    "whisper",
                    "whisper-onnx"
                },
                DefaultChatModel = "llama3.2",
                DefaultWhisperModel = "whisper",
                RequiresApiKey = false
            },

            // 通义千问 (阿里云)
            new ProviderPreset
            {
                ProviderType = AIProviderType.Custom,
                DisplayName = "通义千问 (阿里云)",
                Description = "Qwen 系列模型",
                DefaultUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",
                ChatModels = new List<string>
                {
                    "qwen-max",
                    "qwen-plus",
                    "qwen-turbo",
                    "qwen-long",
                    "qwen-vl-max",
                    "qwen-vl-plus"
                },
                WhisperModels = new List<string>(),
                DefaultChatModel = "qwen-plus"
            },

            // 月之暗面 (Kimi)
            new ProviderPreset
            {
                ProviderType = AIProviderType.Custom,
                DisplayName = "月之暗面 (Kimi)",
                Description = "Moonshot 系列模型",
                DefaultUrl = "https://api.moonshot.cn/v1",
                ChatModels = new List<string>
                {
                    "moonshot-v1-128k",
                    "moonshot-v1-32k",
                    "moonshot-v1-8k"
                },
                WhisperModels = new List<string>(),
                DefaultChatModel = "moonshot-v1-32k"
            },

            // 百度文心
            new ProviderPreset
            {
                ProviderType = AIProviderType.Custom,
                DisplayName = "百度文心一言",
                Description = "ERNIE 系列模型",
                DefaultUrl = "https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop",
                ChatModels = new List<string>
                {
                    "ernie-4.0-turbo-8k",
                    "ernie-4.0-8k",
                    "ernie-3.5-8k",
                    "ernie-speed-128k",
                    "ernie-lite-8k"
                },
                WhisperModels = new List<string>(),
                DefaultChatModel = "ernie-3.5-8k"
            },

            // 讯飞星火
            new ProviderPreset
            {
                ProviderType = AIProviderType.Custom,
                DisplayName = "讯飞星火",
                Description = "Spark 系列模型",
                DefaultUrl = "https://spark-api-open.xf-yun.com/v1",
                ChatModels = new List<string>
                {
                    "generalv3.5",
                    "generalv3",
                    "generalv2",
                    "4.0Ultra",
                    "max-32k"
                },
                WhisperModels = new List<string>(),
                DefaultChatModel = "generalv3.5"
            },

            // 自定义
            new ProviderPreset
            {
                ProviderType = AIProviderType.Custom,
                DisplayName = "自定义 (OpenAI 兼容)",
                Description = "任何兼容 OpenAI API 格式的服务",
                DefaultUrl = "https://your-api-endpoint.com/v1",
                ChatModels = new List<string>
                {
                    "your-model-name"
                },
                WhisperModels = new List<string>(),
                DefaultChatModel = "your-model-name",
                RequiresApiKey = true
            }
        };
    }

    /// <summary>
    /// 根据 ProviderType 获取默认预设
    /// </summary>
    public static ProviderPreset GetDefault(AIProviderType type)
    {
        return GetAll().FirstOrDefault(p => p.ProviderType == type)
               ?? GetAll().Last(); // fallback to Custom
    }
}
