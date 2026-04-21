using System;
using System.Collections.Generic;
using System.Linq;

namespace MeetingAI.Models
{
    /// <summary>
    /// AI 厂商提供商枚举
    /// </summary>
    public enum AIProvider
    {
        /// <summary>OpenAI (GPT 系列)</summary>
        OpenAI,
        
        /// <summary>Anthropic (Claude 系列)</summary>
        Anthropic,
        
        /// <summary>深度求索 (DeepSeek 系列)</summary>
        DeepSeek,
        
        /// <summary>Ollama (本地部署模型)</summary>
        Ollama,
        
        /// <summary>智谱 AI (GLM 系列)</summary>
        Zhipu,
        
        /// <summary>MiniMax (海螺 AI)</summary>
        MiniMax,
        
        /// <summary>自定义/其他厂商</summary>
        Custom
    }

    /// <summary>
    /// AI 模型配置类
    /// </summary>
    public class AIModelConfig
    {
        /// <summary>
        /// 选定的 AI 服务提供商
        /// </summary>
        public AIProvider Provider { get; set; } = AIProvider.OpenAI;

        /// <summary>
        /// API 密钥
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API Base URL
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// 模型名称
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 最大 Token 数量
        /// </summary>
        public int MaxTokens { get; set; } = 4096;

        /// <summary>
        /// Temperature 参数 (0.0 - 2.0)
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Top P 参数 (0.0 - 1.0)
        /// </summary>
        public double TopP { get; set; } = 0.9;

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// 系统提示词
        /// </summary>
        public string SystemPrompt { get; set; } = 
            "你是一个专业的会议助手，负责总结会议内容、提取关键信息、生成结构化的会议报告。";

        /// <summary>
        /// 深度思考模式 (部分厂商支持，如 DeepSeek-R1)
        /// </summary>
        public bool EnableThinking { get; set; } = false;

        /// <summary>
        /// 思考预算 Token (仅 Claude/Gemini 等支持)
        /// </summary>
        public int ThinkingBudget { get; set; } = 1024;

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static AIModelConfig CreateDefault(AIProvider provider)
        {
            var config = new AIModelConfig
            {
                Provider = provider,
                ApiKey = string.Empty,
                Model = ProviderDefaults.TryGetValue(provider, out var defaults) 
                    ? defaults.FirstOrDefault() ?? "gpt-4o" 
                    : "gpt-4o",
                BaseUrl = ProviderBaseUrls.TryGetValue(provider, out var url) 
                    ? url 
                    : "https://api.openai.com/v1",
                MaxTokens = ProviderMaxTokens.TryGetValue(provider, out var tokens) 
                    ? tokens 
                    : 4096,
                Temperature = 0.7,
                TopP = 0.9,
                TimeoutSeconds = 120,
                SystemPrompt = "你是一个专业的会议助手，负责总结会议内容、提取关键信息、生成结构化的会议报告。",
                EnableThinking = false,
                ThinkingBudget = 1024
            };

            return config;
        }

        /// <summary>
        /// 切换 Provider 时自动应用默认值
        /// </summary>
        public void ApplyDefaultsForProvider()
        {
            Model = ProviderDefaults.TryGetValue(Provider, out var defaults) 
                ? defaults.FirstOrDefault() ?? Model 
                : Model;
            BaseUrl = ProviderBaseUrls.TryGetValue(Provider, out var url) 
                ? url 
                : BaseUrl;
            MaxTokens = ProviderMaxTokens.TryGetValue(Provider, out var tokens) 
                ? tokens 
                : MaxTokens;
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                return (false, "API Key 不能为空");

            if (string.IsNullOrWhiteSpace(BaseUrl))
                return (false, "Base URL 不能为空");

            if (string.IsNullOrWhiteSpace(Model))
                return (false, "模型名称不能为空");

            if (MaxTokens <= 0 || MaxTokens > 200000)
                return (false, $"MaxTokens 必须在 1 到 200000 之间，当前值: {MaxTokens}");

            if (Temperature < 0 || Temperature > 2.0)
                return (false, $"Temperature 必须在 0.0 到 2.0 之间，当前值: {Temperature}");

            if (TopP < 0 || TopP > 1.0)
                return (false, $"TopP 必须在 0.0 到 1.0 之间，当前值: {TopP}");

            if (TimeoutSeconds <= 0 || TimeoutSeconds > 600)
                return (false, $"TimeoutSeconds 必须在 1 到 600 之间，当前值: {TimeoutSeconds}");

            return (true, null);
        }

        /// <summary>
        /// 复制配置
        /// </summary>
        public AIModelConfig Clone()
        {
            return new AIModelConfig
            {
                Provider = Provider,
                ApiKey = ApiKey,
                BaseUrl = BaseUrl,
                Model = Model,
                MaxTokens = MaxTokens,
                Temperature = Temperature,
                TopP = TopP,
                TimeoutSeconds = TimeoutSeconds,
                SystemPrompt = SystemPrompt,
                EnableThinking = EnableThinking,
                ThinkingBudget = ThinkingBudget
            };
        }

        // ==================== 厂商配置 ====================

        /// <summary>
        /// 厂商默认模型列表
        /// </summary>
        public static readonly Dictionary<AIProvider, List<string>> ProviderDefaults = new()
        {
            [AIProvider.OpenAI] = new List<string>
            {
                // GPT-5 系列 (最新)
                "gpt-5.4",
                "gpt-5.4-mini",
                "gpt-5.1-codex",
                "gpt-5.1",
                
                // GPT-4.1 系列
                "gpt-4.1",
                "gpt-4.1-mini",
                "gpt-4.1-nano",
                
                // GPT-4o 系列
                "gpt-4o",
                "gpt-4o-mini",
                "gpt-4o-2024-08-06",
                "gpt-4o-mini-2024-07-18",
                
                // GPT-4 Turbo 系列
                "gpt-4-turbo",
                "gpt-4-turbo-2024-04-09",
                
                // GPT-4 系列
                "gpt-4",
                "gpt-4-0613",
                
                // GPT-3.5 Turbo 系列
                "gpt-3.5-turbo",
                "gpt-3.5-turbo-16k"
            },

            [AIProvider.Anthropic] = new List<string>
            {
                // Claude 4.5 系列 (最新)
                "claude-opus-4.5",
                "claude-sonnet-4.6",
                "claude-haiku-4.6",
                
                // Claude 4 系列
                "claude-opus-4-20250514",
                "claude-sonnet-4-20250514",
                "claude-haiku-4-20250514",
                "claude-opus-4-20250108",
                "claude-sonnet-4-20250108",
                "claude-haiku-4-20250108",
                
                // Claude 3.5 系列
                "claude-sonnet-3.5-20241022",
                "claude-sonnet-3.5-20240620",
                "claude-haiku-3.5-20241022",
                
                // Claude 3 系列
                "claude-3-opus-20240229",
                "claude-3-sonnet-20240229",
                "claude-3-haiku-20240307"
            },

            [AIProvider.DeepSeek] = new List<string>
            {
                // DeepSeek-V3 系列 (最新，含思考模式)
                "deepseek-v3.2",           // ⭐ 推荐，支持思考模式
                "deepseek-v3.2-exp",
                "deepseek-v3.1",
                "deepseek-v3",
                
                // DeepSeek-R1 系列 (推理专用)
                "deepseek-r1",
                "deepseek-r1-0528",
                "deepseek-r1-0117",
                "deepseek-r1-distill-qwen-32b",
                "deepseek-r1-distill-llama-70b",
                
                // DeepSeek-Coder 系列
                "deepseek-coder-33b-instruct",
                
                // DeepSeek-Chat 系列
                "deepseek-chat"
            },

            [AIProvider.Zhipu] = new List<string>
            {
                // GLM-4 系列 (最新)
                "glm-4-0520",
                "glm-4-plus",
                "glm-4-flash",
                "glm-4-0415",
                "glm-4-0116",
                
                // GLM-3 系列
                "glm-3-turbo",
                
                // GLM-4V (视觉模型)
                "glm-4v-0520",
                "glm-4v-plus",
                "glm-4v-flash",
                
                // CogView (图像生成)
                "cogview-3",
                "cogview-3-plus",
                
                // CogVideo (视频生成)
                "cogvideox",
                "cogvideox-flash",
                
                // 思维链模型
                "glm-z1-32b",
                "glm-z1-flash"
            },

            [AIProvider.MiniMax] = new List<string>
            {
                // MiniMax-Text 系列 (最新)
                "MiniMax-Text-01",
                "MiniMax-Text-01-preview",
                
                // abab 系列
                "abab7-preview",
                "abab7-chat",
                "abab6.5s-chat",
                "abab6.5-chat",
                "abab5.5-chat",
                
                // MiniMax-VL (视觉语言模型)
                "MiniMax-VL-01",
                "MiniMax-VL-01-preview",
                
                // 学术模型
                "academic-3.5",
                
                // 代码模型
                "minimax-llm-code"
            },

            [AIProvider.Ollama] = new List<string>
            {
                // 通用模型
                "llama3",
                "llama3.1",
                "llama3.2",
                "llama3.3",
                
                // Phi 系列
                "phi3",
                "phi3.5",
                
                // Mistral 系列
                "mistral",
                "mistral-nemo",
                "mixtral",
                "mixtral-8x7b",
                
                // Qwen 系列
                "qwen2.5",
                "qwen2.5-coder",
                "qwen2.5-math",
                
                // DeepSeek 系列
                "deepseek-v2",
                "deepseek-coder",
                
                // CodeLlama
                "codellama",
                
                // Gemma 系列
                "gemma2",
                "gemma2-27b",
                
                // 中文优化
                "yi",
                "yi-chat",
                
                // 其他
                "starcoder2",
                "nomicron"
            },

            [AIProvider.Custom] = new List<string>
            {
                // 自定义模型需要手动配置
                "custom-model"
            }
        };

        /// <summary>
        /// 厂商默认 Base URL
        /// </summary>
        public static readonly Dictionary<AIProvider, string> ProviderBaseUrls = new()
        {
            // 国际厂商
            [AIProvider.OpenAI] = "https://api.openai.com/v1",
            [AIProvider.Anthropic] = "https://api.anthropic.com/v1",
            
            // 国内厂商
            [AIProvider.DeepSeek] = "https://api.deepseek.com/v1",
            [AIProvider.Zhipu] = "https://open.bigmodel.cn/api/paas/v4",      // 智谱 GLM-4 API
            [AIProvider.MiniMax] = "https://api.minimaxi.com/v1",            // MiniMax 海螺 AI
            
            // 本地部署
            [AIProvider.Ollama] = "http://localhost:11434/v1",
            
            // 自定义
            [AIProvider.Custom] = ""
        };

        /// <summary>
        /// 厂商默认最大 Token 数
        /// </summary>
        public static readonly Dictionary<AIProvider, int> ProviderMaxTokens = new()
        {
            [AIProvider.OpenAI] = 128000,      // GPT-4o 支持 128K
            [AIProvider.Anthropic] = 200000,  // Claude 支持 200K
            [AIProvider.DeepSeek] = 64000,     // DeepSeek-V3 支持 64K
            [AIProvider.Zhipu] = 128000,       // GLM-4 支持 128K
            [AIProvider.MiniMax] = 100000,    // MiniMax-Text-01 支持 100K
            [AIProvider.Ollama] = 8192,       // 本地模型通常较小
            [AIProvider.Custom] = 4096
        };

        /// <summary>
        /// 厂商显示名称（中文）
        /// </summary>
        public static readonly Dictionary<AIProvider, string> ProviderDisplayNames = new()
        {
            [AIProvider.OpenAI] = "OpenAI (GPT)",
            [AIProvider.Anthropic] = "Anthropic (Claude)",
            [AIProvider.DeepSeek] = "深度求索 (DeepSeek)",
            [AIProvider.Zhipu] = "智谱 AI (GLM)",
            [AIProvider.MiniMax] = "MiniMax (海螺AI)",
            [AIProvider.Ollama] = "Ollama (本地)",
            [AIProvider.Custom] = "自定义"
        };

        /// <summary>
        /// 厂商官网
        /// </summary>
        public static readonly Dictionary<AIProvider, string> ProviderWebsites = new()
        {
            [AIProvider.OpenAI] = "https://platform.openai.com/",
            [AIProvider.Anthropic] = "https://www.anthropic.com/",
            [AIProvider.DeepSeek] = "https://platform.deepseek.com/",
            [AIProvider.Zhipu] = "https://open.bigmodel.cn/",
            [AIProvider.MiniMax] = "https://platform.minimaxi.com/",
            [AIProvider.Ollama] = "https://ollama.com/",
            [AIProvider.Custom] = ""
        };

        /// <summary>
        /// 厂商特色说明
        /// </summary>
        public static readonly Dictionary<AIProvider, string> ProviderFeatures = new()
        {
            [AIProvider.OpenAI] = "通用能力强，多模态支持，生态完善",
            [AIProvider.Anthropic] = "安全性强，长上下文，上下文窗口大",
            [AIProvider.DeepSeek] = "高性价比，推理能力强，代码能力强",
            [AIProvider.Zhipu] = "中文优化好，多模态丰富，开源友好",
            [AIProvider.MiniMax] = "语音合成超强，适合音频场景，多模态",
            [AIProvider.Ollama] = "本地部署，隐私保护，无网络依赖",
            [AIProvider.Custom] = "支持其他兼容 OpenAI 格式的 API"
        };

        /// <summary>
        /// 获取所有 Provider 列表（用于 UI 绑定）
        /// </summary>
        public static IEnumerable<AIProvider> GetAllProviders()
        {
            return Enum.GetValues<AIProvider>();
        }

        /// <summary>
        /// 获取指定 Provider 的模型列表
        /// </summary>
        public static List<string> GetModelsForProvider(AIProvider provider)
        {
            return ProviderDefaults.TryGetValue(provider, out var models) 
                ? models 
                : new List<string>();
        }
    }
}