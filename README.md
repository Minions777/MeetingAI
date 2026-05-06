# 🎙️ MeetingAI v2

重构后的 MeetingAI 会议助手，采用模块化架构，支持多 AI Provider。

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)

## ✨ 功能特性

- 🔴 **会议录音** - 系统音频 + 麦克风混合录制
- 🔊 **语音转文字** - 支持 Whisper API / Ollama 本地转录
- 🤖 **AI 摘要** - 支持 OpenAI / Claude / DeepSeek / Ollama / 智谱 / MiniMax
- 📋 **一键复制** - 快速复制会议摘要
- ⚙️ **多模型配置** - 可配置多个 AI Provider 并随时切换
- ⌨️ **全局快捷键** - 支持 Ctrl+Shift+R 切换录音，Ctrl+Shift+S 停止录音
- 📁 **会议历史** - 自动保存会议记录，支持搜索和导出
- 🔒 **安全加密** - 使用 DPAPI 加密敏感信息

## 🚀 快速开始

### 环境要求

- Windows 10/11 (64位)
- .NET 8 SDK
- API Key (根据使用的 AI 模型)

### 运行项目

```bash
# 克隆仓库
git clone https://github.com/Minions777/MeetingAI.git
cd MeetingAI

# 进入客户端目录
cd src/MeetingAI.Client

# 恢复依赖
dotnet restore

# 运行
dotnet run
```

### 构建独立可执行文件

```bash
cd src/MeetingAI.Client

# 发布 Release 版本（单文件自包含）
dotnet publish -c Release -p:Optimize=true

# 可执行文件位置
# bin/Release/net8.0-windows/win-x64/publish/MeetingAI.Client.exe
```

## 🎹 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+Shift+R` | 切换录音（开始/暂停） |
| `Ctrl+Shift+S` | 停止录音 |

> 💡 快捷键支持全局捕获，即使应用窗口未聚焦也能响应。

## ⚙️ AI Provider 配置

| 厂商 | 支持功能 | 默认模型 |
|------|---------|---------|
| OpenAI | 转录 + 摘要 | gpt-4o-mini |
| DeepSeek | 摘要 | deepseek-chat |
| Anthropic | 摘要 | claude-3-5-sonnet |
| Ollama | 转录 + 摘要 | llama3.2 |
| 智谱 | 摘要 | glm-4 |
| MiniMax | 摘要 | MiniMax-Text-01 |

## 🏗️ 架构设计

```
MeetingAI/
├── src/
│   ├── MeetingAI.Client/      # WPF 主客户端 (MVVM)
│   │   ├── Views/           # 视图
│   │   ├── ViewModels/      # 视图模型
│   │   └── Themes/          # 主题样式
│   ├── MeetingAI.Core/      # 核心业务逻辑
│   │   ├── Services/        # 业务服务
│   │   │   ├── RecordingService      # 录音服务
│   │   │   ├── TranscriptionService   # 转录服务
│   │   │   ├── SummaryService        # 摘要服务
│   │   │   └── MeetingHistoryService # 历史管理
│   │   ├── Providers/       # AI Provider 抽象层
│   │   │   ├── Abstractions/        # 接口定义
│   │   │   ├── BaseAIProvider.cs    # 基类
│   │   │   └── Implementations/      # 具体实现
│   │   └── Models/          # 数据模型
│   └── MeetingAI.Shared/    # 共享基础设施
│       ├── Configuration/    # 配置管理
│       ├── Helpers/          # 辅助工具
│       │   └── GlobalHotkeyService.cs # 全局快捷键
│       ├── Logging/          # 日志服务
│       └── i18n/            # 多语言支持
└── tests/
    └── MeetingAI.Core.Tests/ # 单元测试
```

### 核心设计模式

#### Provider 抽象层

参考 Cherry Studio 的 Provider 设计模式，便于扩展新的 AI 服务商：

```csharp
// 定义接口
public interface IAIProvider
{
    string Id { get; }
    string Name { get; }
    bool IsConfigured { get; }
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct);
    Task<Transcript> TranscribeAsync(AudioData audio, TranscriptionOptions? options, CancellationToken ct);
    Task<bool> TestConnectionAsync(CancellationToken ct);  // 新增
}

// 使用工厂创建
var provider = ProviderFactory.Create(AIProviderType.OpenAI);
```

#### 配置加密

使用 DPAPI 加密敏感信息（API Key）：

```csharp
// 加密存储
SecureStorage.EncryptConfig(providerConfig);

// 解密使用
SecureStorage.DecryptConfig(providerConfig);

// 验证加密
SecureStorage.ValidateEncryption(apiKey);
```

## 📊 会议历史管理

```csharp
var historyService = new MeetingHistoryService();

// 保存会议记录
await historyService.SaveAsync(meetingRecord);

// 搜索会议
var results = await historyService.SearchAsync("关键词");

// 导出为 Markdown
var markdown = historyService.ExportToMarkdown(record);

// 获取统计信息
var stats = await historyService.GetStatsAsync();
```

## 🧪 测试

```bash
# 运行所有测试
dotnet test

# 运行特定测试
dotnet test --filter "FullyQualifiedName~SecureStorageTests"
```

## 📝 项目结构说明

### 核心服务

| 服务 | 职责 |
|------|------|
| `RecordingService` | 音频录制，混合系统音频和麦克风 |
| `TranscriptionService` | 语音转文字，调用 AI API |
| `SummaryService` | AI 摘要生成，支持多 Provider |
| `MeetingHistoryService` | 会议记录持久化和搜索 |
| `ConfigurationService` | 配置管理，支持备份恢复 |

### 新增特性

- **全局快捷键** (`GlobalHotkeyService`)
  - 使用 Win32 API 实现系统级热键
  - 支持快捷键可用性检测
  
- **配置管理增强**
  - 配置缓存过期机制
  - 支持备份恢复
  - 配置验证
  - 安全导出

- **摘要解析增强**
  - 多级 fallback 解析
  - 更强的容错能力

## 🔧 开发指南

### 添加新的 AI Provider

1. 在 `Providers/Implementations/` 创建新类，继承 `BaseAIProvider`
2. 实现抽象方法：
   ```csharp
   public class MyProvider : BaseAIProvider
   {
       public override string Id => "my-provider";
       public override string Name => "我的 Provider";
       // ... 实现其他抽象成员
   }
   ```
3. 在 `ProviderFactory` 中注册

### 添加单元测试

1. 在 `tests/MeetingAI.Core.Tests/` 添加测试类
2. 继承 `IClassFixture<T>` 使用共享上下文
3. 使用 Moq 模拟依赖

## 📄 License

MIT License - see [LICENSE](LICENSE) for details

## 🙏 致谢

- [NAudio](https://github.com/naudio/NAudio) - 音频处理
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM 框架
- [Serilog](https://serilog.net/) - 结构化日志
- 所有贡献者的努力 👏

---

⭐ 如果这个项目对你有帮助，请给一个 Star！
