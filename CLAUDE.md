# MeetingAI

智能会议助手 — 基于 .NET 8 WPF 的实时语音转文字与 AI 会议总结系统。

## 技术栈

- **框架**: .NET 8.0-windows, WPF (MVVM)
- **UI**: CommunityToolkit.Mvvm 8.2.2, Serilog
- **音频**: NAudio 2.2.1 (系统音频 + 麦克风混合录制)
- **AI**: 多 Provider 架构 (OpenAI, DeepSeek, Anthropic, Ollama, 智谱, MiniMax)
- **弹性**: Polly 8.3.1 (重试/熔断)
- **安全**: DPAPI 加密存储 API 密钥
- **测试**: xUnit + Moq + FluentAssertions

## 项目结构

```
src/
├── MeetingAI.Client/     # WPF 入口 (Views/, ViewModels/, Converters/)
├── MeetingAI.Core/        # 业务逻辑 (Providers/, Services/, Repositories/)
├── MeetingAI.Shared/      # 基础设施 (Configuration/, Helpers/, Logging/)
tests/
└── MeetingAI.Core.Tests/  # 单元测试
docs/                      # 设计文档
```

## 构建命令

```bash
dotnet build
dotnet test
dotnet publish -c Release -p:Optimize=true  # 单文件自包含 exe
```

发布产物: `src/MeetingAI.Client/bin/Release/net8.0-windows/win-x64/publish/MeetingAI.Client.exe`

## 核心架构

### AI Provider 模式

```
IAIProvider (接口)
    └── BaseAIProvider (公共逻辑: HttpClient, 配置, 连接测试)
            ├── OpenAIProvider
            ├── DeepSeekProvider
            ├── AnthropicProvider
            ├── OllamaProvider
            ├── ZhipuProvider
            └── MiniMaxProvider
ProviderFactory → 根据 AIProviderType 创建实例
```

### 全局热键

- `Ctrl+Shift+R` — 开始/停止录制
- `Ctrl+Shift+S` — 停止录音

## 开发工作流

遵循 [用户规则](../rules/zh/):

1. **研究优先** — 查找现有实现/模式
2. **规划** — 使用 planner 代理设计实现方案
3. **TDD** — 使用 tdd-guide 代理，先写测试 (RED → GREEN → IMPROVE)
4. **代码审查** — 使用 code-reviewer 代理，80%+ 覆盖率
5. **提交** — 约定式提交 (feat/fix/refactor/docs/test/chore/perf/ci)

## 安全要求

- API 密钥通过 `SecureStorage` (DPAPI) 加密存储
- 禁止硬编码凭据
- 用户输入需验证

## 代码审查触发

**强制审查**:
- 编写/修改代码后
- 提交到共享分支前
- 安全敏感代码 (认证、用户数据、文件操作)
- 架构更改

## 测试要求

最低覆盖率 80%:
- 单元测试 (函数/工具/组件)
- 集成测试 (API/数据库)

## 关键文件位置

| 组件 | 路径 |
|------|------|
| AI Provider 实现 | `src/MeetingAI.Core/Providers/Implementations/` |
| 核心服务 | `src/MeetingAI.Core/Services/` |
| 客户端 UI | `src/MeetingAI.Client/Views/` (XAML) |
| ViewModels | `src/MeetingAI.Client/ViewModels/` |
| 共享配置 | `src/MeetingAI.Shared/Configuration/` |
| 测试 | `tests/MeetingAI.Core.Tests/` |
| 设计文档 | `docs/UI-Design-System.md` |

## 注意事项

- PostToolUse 钩子: `dotnet format` 自动格式化 C# 文件
- 代码变更后运行 `dotnet build` 验证编译