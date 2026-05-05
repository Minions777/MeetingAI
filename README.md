# 🎙️ MeetingAI v2

重构后的 MeetingAI 会议助手，采用模块化架构，支持多 AI Provider。

## 功能特性

- 🔴 **会议录音** - 系统音频 + 麦克风混合录制
- 🔊 **语音转文字** - 支持 Whisper API / Ollama 本地转录
- 🤖 **AI 摘要** - 支持 OpenAI / Claude / DeepSeek / Ollama / 智谱 / MiniMax
- 📋 **一键复制** - 快速复制会议摘要
- ⚙️ **多模型配置** - 可配置多个 AI Provider 并随时切换

## 架构设计

```
MeetingAI/
├── src/
│   ├── MeetingAI.Client/     # WPF 主客户端 (MVVM)
│   ├── MeetingAI.Core/       # 核心业务逻辑
│   │   ├── Services/        # 录音、转录、摘要服务
│   │   ├── Providers/       # AI Provider 抽象层
│   │   └── Models/          # 数据模型
│   └── MeetingAI.Shared/    # 共享基础设施
│       ├── Configuration/    # 配置管理
│       ├── Logging/          # 日志服务
│       └── i18n/             # 多语言支持
└── tests/
    └── MeetingAI.Core.Tests/ # 单元测试
```

## 技术栈

- .NET 8 + WPF
- CommunityToolkit.Mvvm (MVVM)
- NAudio (音频录制)
- Serilog (日志)
- xUnit + Moq (测试)

## 快速开始

### 环境要求

- Windows 10/11
- .NET 8 SDK
- API Key (根据使用的 AI 模型)

### 运行项目

```bash
cd src/MeetingAI.Client
dotnet restore
dotnet run
```

### AI Provider 配置

| 厂商 | 支持功能 | 默认模型 |
|------|---------|---------|
| OpenAI | 转录 + 摘要 | gpt-4o-mini |
| DeepSeek | 摘要 | deepseek-chat |
| Anthropic | 摘要 | claude-3-5-sonnet |
| Ollama | 转录 + 摘要 | llama3.2 |
| 智谱 | 摘要 | glm-4 |
| MiniMax | 摘要 | MiniMax-Text-01 |

## 项目结构说明

### Provider 抽象层

参考 Cherry Studio 的 Provider 设计模式：

```csharp
public interface IAIProvider
{
    string Id { get; }
    string Name { get; }
    bool IsConfigured { get; }
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct);
    Task<Transcript> TranscribeAsync(AudioData audio, TranscriptionOptions? options, CancellationToken ct);
}
```

### 配置管理

使用 DPAPI 加密敏感信息 (API Key)：

```csharp
SecureStorage.Encrypt(apiKey);   // 加密存储
SecureStorage.Decrypt(apiKey);  // 解密使用
```

## License

MIT License
