using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MeetingAI.Models
{
    public enum AIProvider
    {
        OpenAI, Anthropic, DeepSeek, Ollama, Zhipu, MiniMax, Custom
    }

    public class AIModelConfig
    {
        public string Name { get; set; } = "默认配置";
        [JsonIgnore] public bool IsSelected { get; set; } = false;
        public AIProvider Provider { get; set; } = AIProvider.OpenAI;
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int MaxTokens { get; set; } = 4096;
        public double Temperature { get; set; } = 0.7;
        public double TopP { get; set; } = 0.9;
        public int TimeoutSeconds { get; set; } = 120;
        public string SystemPrompt { get; set; } = "你是一个专业的会议助手，负责总结会议内容、提取关键信息、生成结构化的会议报告。";
        public bool EnableThinking { get; set; } = false;
        public int ThinkingBudget { get; set; } = 1024;

        public static AIModelConfig CreateDefault(AIProvider provider)
        {
            var config = new AIModelConfig
            {
                Name = GetProviderDisplayName(provider) + " - 默认",
                Provider = provider,
                Model = ProviderDefaults.TryGetValue(provider, out var defaults) ? defaults.FirstOrDefault() ?? "gpt-4o" : "gpt-4o",
                BaseUrl = ProviderBaseUrls.TryGetValue(provider, out var url) ? url : "https://api.openai.com/v1",
                MaxTokens = ProviderMaxTokens.TryGetValue(provider, out var tokens) ? tokens : 4096,
                Temperature = 0.7, TopP = 0.9, TimeoutSeconds = 120,
                SystemPrompt = "你是一个专业的会议助手，负责总结会议内容、提取关键信息、生成结构化的会议报告。",
                EnableThinking = false, ThinkingBudget = 1024
            };
            return config;
        }

        public void ApplyDefaultsForProvider()
        {
            Model = ProviderDefaults.TryGetValue(Provider, out var defaults) ? defaults.FirstOrDefault() ?? Model : Model;
            BaseUrl = ProviderBaseUrls.TryGetValue(Provider, out var url) ? url : BaseUrl;
            MaxTokens = ProviderMaxTokens.TryGetValue(Provider, out var tokens) ? tokens : MaxTokens;
            Name = GetProviderDisplayName(Provider) + " - " + Model;
        }

        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiKey)) return (false, "API Key 不能为空");
            if (string.IsNullOrWhiteSpace(BaseUrl)) return (false, "Base URL 不能为空");
            if (string.IsNullOrWhiteSpace(Model)) return (false, "模型名称不能为空");
            if (MaxTokens <= 0 || MaxTokens > 200000) return (false, "MaxTokens 必须在 1 到 200000 之间，当前值: " + MaxTokens);
            if (Temperature < 0 || Temperature > 2.0) return (false, "Temperature 必须在 0.0 到 2.0 之间，当前值: " + Temperature);
            if (TopP < 0 || TopP > 1.0) return (false, "TopP 必须在 0.0 到 1.0 之间，当前值: " + TopP);
            if (TimeoutSeconds <= 0 || TimeoutSeconds > 600) return (false, "TimeoutSeconds 必须在 1 到 600 之间，当前值: " + TimeoutSeconds);
            if (EnableThinking && ThinkingBudget <= 0) return (false, "启用思考模式时，ThinkingBudget 必须大于 0");
            return (true, null);
        }

        public AIModelConfig Clone()
        {
            return new AIModelConfig
            {
                Name = Name + " (副本)", Provider = Provider, ApiKey = ApiKey, BaseUrl = BaseUrl, Model = Model,
                MaxTokens = MaxTokens, Temperature = Temperature, TopP = TopP, TimeoutSeconds = TimeoutSeconds,
                SystemPrompt = SystemPrompt, EnableThinking = EnableThinking, ThinkingBudget = ThinkingBudget, IsSelected = false
            };
        }

        public static readonly Dictionary<AIProvider, List<string>> ProviderDefaults = new()
        {
            [AIProvider.OpenAI] = new List<string> { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo" },
            [AIProvider.Anthropic] = new List<string> { "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20241022", "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307" },
            [AIProvider.DeepSeek] = new List<string> { "deepseek-chat", "deepseek-reasoner" },
            [AIProvider.Zhipu] = new List<string> { "glm-4", "glm-4-plus", "glm-4-flash", "glm-3-turbo" },
            [AIProvider.MiniMax] = new List<string> { "abab6.5-chat", "abab6.5s-chat", "abab5.5-chat" },
            [AIProvider.Ollama] = new List<string> { "llama3", "llama3.1", "llama3.2", "qwen2.5", "deepseek-v2", "mistral", "mixtral" },
            [AIProvider.Custom] = new List<string> { "custom-model" }
        };

        public static readonly Dictionary<AIProvider, string> ProviderBaseUrls = new()
        {
            [AIProvider.OpenAI] = "https://api.openai.com/v1",
            [AIProvider.Anthropic] = "https://api.anthropic.com/v1",
            [AIProvider.DeepSeek] = "https://api.deepseek.com/v1",
            [AIProvider.Zhipu] = "https://open.bigmodel.cn/api/paas/v4",
            [AIProvider.MiniMax] = "https://api.minimaxi.com/v1",
            [AIProvider.Ollama] = "http://localhost:11434/v1",
            [AIProvider.Custom] = ""
        };

        public static readonly Dictionary<AIProvider, int> ProviderMaxTokens = new()
        {
            [AIProvider.OpenAI] = 128000, [AIProvider.Anthropic] = 200000, [AIProvider.DeepSeek] = 64000,
            [AIProvider.Zhipu] = 128000, [AIProvider.MiniMax] = 204800, [AIProvider.Ollama] = 8192, [AIProvider.Custom] = 4096
        };

        public static readonly Dictionary<AIProvider, string> ProviderDisplayNames = new()
        {
            [AIProvider.OpenAI] = "OpenAI (GPT)", [AIProvider.Anthropic] = "Anthropic (Claude)",
            [AIProvider.DeepSeek] = "深度求索 (DeepSeek)", [AIProvider.Zhipu] = "智谱 AI (GLM)",
            [AIProvider.MiniMax] = "MiniMax (海螺AI)", [AIProvider.Ollama] = "Ollama (本地)", [AIProvider.Custom] = "自定义"
        };

        public static string GetProviderDisplayName(AIProvider provider) => ProviderDisplayNames.TryGetValue(provider, out var name) ? name : provider.ToString();
        public static IEnumerable<AIProvider> GetAllProviders() => Enum.GetValues<AIProvider>();
        public static List<string> GetModelsForProvider(AIProvider provider) => ProviderDefaults.TryGetValue(provider, out var models) ? models : new List<string>();
    }
}