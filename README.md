# MeetingAI

智能会议助手 — 基于 .NET 10 + Avalonia 的实时语音转文字与 AI 会议摘要系统，支持 Windows 与 macOS。

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS-blue.svg)](https://avaloniaui.net/)

## 功能特性

- **会议录音** — 系统音频 + 麦克风混合录制（Windows/macOS 平台适配）
- **语音转文字** — 支持 Whisper API / Ollama 本地转录
- **AI 摘要** — 支持 OpenAI / Claude / DeepSeek / Ollama / 智谱 / MiniMax，流式输出
- **一键复制** — 快速复制会议摘要
- **多模型配置** — 可配置多个 AI Provider 并随时切换
- **全局快捷键** — `Ctrl+Shift+R` 切换录音，`Ctrl+Shift+S` 停止录音
- **会议历史** — 自动保存会议记录，支持搜索和导出（2 分钟内存缓存）
- **安全加密** — 使用 DPAPI（Windows）加密敏感信息
- **跨平台** — 从 WPF 迁移至 Avalonia UI，支持 macOS

## 快速开始

### 环境要求

| 平台 | 要求 |
|------|------|
| Windows | Windows 10/11 (64位)，.NET 8 SDK |
| macOS | macOS 11+，.NET 8 SDK |
| 其他 | API Key（根据使用的 AI 模型） |

### 构建与运行

```bash
# 克隆仓库
git clone https://github.com/Minions777/MeetingAI.git
cd MeetingAI

# 恢复依赖
dotnet restore

# 构建
dotnet build

# 运行
dotnet run --project src/MeetingAI.Client/MeetingAI.Client.csproj
```

### 发布独立可执行文件

```bash
# Windows
dotnet publish src/MeetingAI.Client/MeetingAI.Client.csproj -c Release -r win-x64 --self-contained -p:Optimize=true

# macOS
dotnet publish src/MeetingAI.Client/MeetingAI.Client.csproj -c Release -r osx-x64 --self-contained -p:Optimize=true
```

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+Shift+R` | 切换录音（开始/暂停） |
| `Ctrl+Shift+S` | 停止录音 |

## AI Provider

| 厂商 | 支持功能 | 默认模型 |
|------|---------|---------|
| OpenAI | 转录 + 摘要 | gpt-4o-mini |
| DeepSeek | 摘要 | deepseek-chat |
| Anthropic | 摘要 | claude-3-5-sonnet |
| Ollama | 转录 + 摘要 | llama3.2 |
| 智谱 | 摘要 | glm-4 |
| MiniMax | 摘要 | MiniMax-Text-01 |

## 项目结构

```
MeetingAI/
├── src/
│   ├── MeetingAI.Client/        # Avalonia 主客户端 (MVVM)
│   │   ├── Views/               # .axaml 视图
│   │   ├── ViewModels/          # 视图模型（按职责拆分）
│   │   └── Themes/              # 主题样式
│   ├── MeetingAI.Core/          # 核心业务逻辑
│   │   ├── Services/
│   │   │   ├── RecordingService.cs     # 音频录制（平台适配）
│   │   │   ├── TranscriptionService.cs  # 语音转文字
│   │   │   ├── SummaryService.cs        # AI 摘要（支持流式）
│   │   │   ├── MeetingHistoryService.cs # 历史管理（含内存缓存）
│   │   │   └── WindowsAudioCapture.cs / MacAudioCapture.cs
│   │   ├── Providers/           # AI Provider 抽象层
│   │   │   ├── Abstractions/     # 接口定义
│   │   │   └── Implementations/  # OpenAI/DeepSeek/Claude/Ollama/智谱/MiniMax
│   │   ├── Resilience/          # 弹性策略（Polly）
│   │   └── Models/              # 数据模型
│   └── MeetingAI.Shared/         # 跨平台共享基础设施
│       ├── Configuration/        # 配置管理（加密存储、备份恢复）
│       ├── Helpers/              # 全局热键（Windows/macOS 分平台实现）
│       └── Logging/              # 结构化日志（Serilog）
└── tests/
    └── MeetingAI.Core.Tests/     # 单元测试（xUnit + Moq + FluentAssertions）
        ├── Providers/            # Provider 工厂和配置测试
        ├── Services/             # Service 层测试
        └── Resilience/           # 弹性策略测试
```

## 核心设计

### Provider 架构

采用工厂模式，按 `AIProviderType` 枚举创建对应 Provider 实例：

```csharp
var provider = ProviderFactory.Create(AIProviderType.OpenAI, config);
var response = await provider.ChatAsync(request, cancellationToken);
```

### 配置加密

敏感信息（API Key）通过 DPAPI（Windows）加密存储，支持安全导出（Key 脱敏）：

```csharp
secureStorage.EncryptConfig(providerConfig);
secureStorage.DecryptConfig(providerConfig);
```

### 历史服务缓存

`MeetingHistoryService` 内置 2 分钟 TTL 内存缓存，写入/删除时自动失效，减少重复磁盘 I/O。

### 弹性策略

使用 Polly 实现重试与熔断，Provider 失败时自动切换：

```csharp
var resilientProvider = new ResilientAiProvider(provider, retryCount: 3);
```

## 测试

```bash
# 运行全部测试
dotnet test

# 带覆盖率
dotnet test --collect:"XPlat Code Coverage"
```

**当前测试覆盖**：58 个测试，涵盖 SecureStorage、Providers、Services、Resilience。

## 开发指南

### 添加新的 AI Provider

1. 在 `Providers/Implementations/` 继承 `BaseAIProvider`
2. 在 `ProviderFactory.Create()` 中注册新类型
3. 添加对应的单元测试

### 添加单元测试

1. 在 `tests/MeetingAI.Core.Tests/` 下按模块创建测试类
2. 使用 `Moq` 模拟依赖，`FluentAssertions` 优化断言可读性
3. 参考现有测试的 `IClassFixture` 模式

## 技术栈

| 分类 | 技术 |
|------|------|
| 框架 | .NET 8，Avalonia 11.2（跨平台 UI） |
| MVVM | CommunityToolkit.Mvvm 8.2 |
| 日志 | Serilog |
| 弹性 | Polly 8.3 |
| 测试 | xUnit + Moq + FluentAssertions |
| 加密 | System.Security.Cryptography（DPAPI） |

## License

MIT License — see [LICENSE](LICENSE)

## 致谢

- [Avalonia](https://avaloniaui.net/) — 跨平台 UI 框架
- [NAudio](https://github.com/naudio/NAudio) — 音频处理
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) — MVVM 工具包
- [Serilog](https://serilog.net/) — 结构化日志