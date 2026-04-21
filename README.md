# 🎙️ MeetingAI - 会议纪要助手

Windows 11 会议记录 AI 小工具，支持录音、转录、AI 总结和一键复制。

## ✨ 功能特性

- 🔴 **会议录音** - 系统音频 + 麦克风混合录制
- 🔊 **语音转文字** - 支持 OpenAI Whisper API
- 🤖 **AI 总结** - 支持多种 AI 模型
- 📋 **一键复制** - 快速复制会议摘要
- ⚙️ **多模型配置** - OpenAI / Claude / Gemini / DeepSeek / Ollama

## 🛠️ 技术栈

- .NET 8 + WPF
- NAudio - 音频录制
- MVVM 架构

## 📁 项目结构

```
MeetingAI/
├── Models/              # 数据模型
├── Services/            # 业务服务
├── ViewModels/          # 视图模型
└── Views/               # 界面视图
```

## 🚀 快速开始

### 环境要求
- Windows 10/11
- .NET 8 SDK
- API Key（根据使用的 AI 模型）

### 运行项目

```bash
cd src/MeetingAI
dotnet restore
dotnet run
```

### AI 模型配置

| 厂商 | 推荐模型 | 特点 |
|------|---------|------|
| OpenAI | gpt-4o-mini | 速度快、成本低 |
| Claude | claude-3-5-sonnet | 分析能力强 |
| DeepSeek | deepseek-chat | 性价比高 |
| Gemini | gemini-2.0-flash | 免费额度大 |
| Ollama | llama3.2 | 完全本地 |

## 📝 使用流程

1. 选择 AI 模型配置
2. 点击「开始录音」开始会议录制
3. 会议结束后点击「停止」
4. 点击「转录」将音频转为文字
5. 点击「生成摘要」获得 AI 会议纪要
6. 点击「一键复制」复制摘要内容

## 📄 许可证

MIT License

---

Made with ❤️ for better meetings