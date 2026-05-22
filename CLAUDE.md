# MeetingAI

智能会议助手 — 基于 .NET 10 + Avalonia 的实时语音转文字与 AI 会议摘要系统，支持 Windows 与 macOS。

## 技术栈

- **框架**: .NET 10, Avalonia 11.2（跨平台 UI）
- **MVVM**: CommunityToolkit.Mvvm 8.2
- **日志**: Serilog
- **音频**: NAudio（Windows）、CoreAudio（macOS）
- **AI**: 多 Provider 架构（OpenAI, DeepSeek, Anthropic, Ollama, 智谱, MiniMax）
- **弹性**: Polly 8.3（重试/熔断）
- **安全**: AES 加密存储 API 密钥（跨平台）
- **测试**: xUnit + Moq + FluentAssertions

## 项目结构

```
src/
├── MeetingAI.Client/     # Avalonia 客户端（Views/, ViewModels/, Themes/）
│   └── ViewModels/       # Recording/Provider/History/Summary/Main/Settings
├── MeetingAI.Core/       # 业务逻辑（Services/, Providers/）
│   ├── Services/         # RecordingService, TranscriptionService, SummaryService,
│   │                     # MeetingHistoryService, AIAssistantService,
│   │                     # WindowsAudioCapture, MacAudioCapture
│   └── Providers/        # OpenAI/DeepSeek/Claude/Ollama/智谱/MiniMax 实现
└── MeetingAI.Shared/     # 基础设施（Configuration/, Helpers/, Logging/, i18n/）
tests/
└── MeetingAI.Core.Tests/ # 单元测试
docs/                     # 设计文档
```

## 构建命令

```bash
dotnet build
dotnet test
dotnet run --project src/MeetingAI.Client/MeetingAI.Client.csproj

# 发布（Windows）
dotnet publish src/MeetingAI.Client/MeetingAI.Client.csproj -c Release -r win-x64 --self-contained -p:Optimize=true

# 发布（macOS）
dotnet publish src/MeetingAI.Client/MeetingAI.Client.csproj -c Release -r osx-arm64 --self-contained -p:Optimize=true
```

## 核心架构

### AI Provider 模式

```
IAIProvider（接口）
    └── 各 Provider 实现（OpenAI, DeepSeek, Anthropic, Ollama, Zhipu, MiniMax）
ProviderFactory → 根据 AIProviderType 创建实例
BaseAIProvider → 内置 Polly 重试策略
```

### 平台适配

- **热键**: `IPlatformHotkeyService` — `WindowsHotkeyService` / `MacHotkeyService`
- **音频捕获**: `IAudioCapture` — `WindowsAudioCapture` / `MacAudioCapture`

## 开发工作流

遵循 [AGENTS.md](AGENTS.md) 中的开发规范：

1. **研究优先** — 查找现有实现/模式
2. **规划** — 设计实现方案，明确验收标准
3. **测试驱动** — 核心逻辑变更需覆盖单元测试
4. **代码审查** — 提交前进行自查
5. **提交** — 约定式提交（feat/fix/refactor/docs/test/chore/perf/ci）

## 安全要求

- API 密钥通过 `SecureStorage`（AES）加密存储
- 禁止硬编码凭据
- 用户输入需验证格式

## 代码审查触发

**强制审查**:
- 编写/修改代码后
- 提交到共享分支前
- 安全敏感代码（认证、用户数据、文件操作）
- 架构更改

## 测试要求

最低覆盖率 80%:
- 单元测试（函数/工具/组件）
- 集成测试（API/数据库）

## 关键文件位置

| 组件 | 路径 |
|------|------|
| AI Provider 实现 | `src/MeetingAI.Core/Providers/Implementations/` |
| 核心服务 | `src/MeetingAI.Core/Services/` |
| 客户端 UI | `src/MeetingAI.Client/Views/`（.axaml） |
| ViewModels | `src/MeetingAI.Client/ViewModels/` |
| 共享配置 | `src/MeetingAI.Shared/Configuration/` |
| 平台热键 | `src/MeetingAI.Shared/Helpers/` |
| 测试 | `tests/MeetingAI.Core.Tests/` |

## 注意事项

- PostToolUse 钩子：`dotnet format` 自动格式化 C# 文件
- 代码变更后运行 `dotnet build` 验证编译
- 新增 Provider 时同时添加单元测试
- macOS 热键使用 CGEventTap，Windows 使用 RegisterHotKey