# MiniMax 和智谱AI厂商配置指南

## 📋 概述

MeetingAI 项目现已支持 MiniMax（海螺AI）和智谱AI（GLM）两大国内主流AI厂商，提供最新的模型支持和完善的配置选项。

## 🚀 MiniMax 配置

### 🔑 API Key 获取

1. 访问 [MiniMax 开放平台](https://platform.minimaxi.com)
2. 注册并登录账号
3. 在 **接口密钥 > 创建新的 API Key** 页面获取 **Coding Plan Key**
4. **注意**：需要创建 Coding Plan Key，不是普通 API Key

### 🤖 推荐模型

| 模型名称 | 上下文窗口 | 特点 | 推荐用途 |
|---------|-----------|------|---------|
| **MiniMax-M2.7** | 204,800 | ⭐ 最新模型，开启自我迭代 | 复杂任务，需要推理 |
| **MiniMax-M2.7-highspeed** | 204,800 | M2.7极速版，100 TPS | 高性能需求场景 |
| **MiniMax-M2.5** | 204,800 | 顶尖性能与极致性价比 | 通用复杂任务 |
| **MiniMax-M2.5-highspeed** | 204,800 | M2.5极速版，100 TPS | 高性能需求场景 |
| **MiniMax-M2.1** | 204,800 | 强大多语言编程能力 | 编程任务 |
| **MiniMax-M2.1-highspeed** | 204,800 | M2.1极速版，100 TPS | 高性能编程 |
| **MiniMax-M2** | 204,800 | 专为高效编码与Agent工作流 | Agent开发 |

### 🔧 配置参数

```csharp
// Base URL
https://api.minimaxi.com/v1

// API 路径
/chat/completions

// 最大 Token 数
204800

// 系统提示词（可选）
"你是一个专业的会议助手，负责总结会议内容、提取关键信息、生成结构化的会议报告。"
```

## 🚀 智谱AI配置

### 🔑 API Key 获取

1. 访问 [智谱AI开放平台](https://open.bigmodel.cn)
2. 注册并登录账号
3. 在 **API Keys** 管理页面创建 API Key
4. 复制 API Key 供项目使用

### 🤖 推荐模型

| 模型名称 | 特点 | 推荐用途 |
|---------|------|---------|
| **GLM-5.1** | ⭐ 最新模型，代码能力大大增强 | 复杂系统工程，长程Agent |
| **GLM-5** | 擅长复杂系统工程与长程Agent任务 | 长任务处理 |
| **GLM-4.7** | 强化编码能力、长程任务规划 | 编程任务 |
| **GLM-4.7-FlashX** | GLM-4.7的轻量高速版本 | 快速响应 |
| **GLM-4.6** | 超强性能，高级编码能力 | 高级编程 |
| **GLM-4.5-Air** | 高性价比，推理、编码强劲 | 经济型需求 |

### 🔧 配置参数

```csharp
// Base URL
https://open.bigmodel.cn/api/paas/v4

// API 路径
/chat/completions

// 最大 Token 数
128000

// 系统提示词（可选）
"你是一个专业的会议助手，负责总结会议内容、提取关键信息、生成结构化的会议报告。"
```

## 💡 使用建议

### MiniMax 使用场景

- **🎯 音频处理**：MiniMax 在语音合成方面表现优异
- **🚀 高性能需求**：极速版模型提供 100 TPS 的处理速度
- **🤖 Agent 开发**：MiniMax-M2 专为 Agent 工作流优化
- **🎨 多模态任务**：支持文本、图像、视频、音乐生成

### 智谱AI使用场景

- **🇨🇳 中文优化**：对中文理解和使用场景有深度优化
- **💻 编程任务**：GLM-5.1 在代码生成和重构方面表现优异
- **🔍 长文本处理**：支持 128K 上下文窗口
- **🎯 复杂推理**：GLM-5.1 在复杂推理任务中表现突出

## 🛠️ 配置步骤

### 1. 选择厂商

在应用设置中选择对应的厂商：
- **MiniMax (海螺AI)**
- **智谱 AI (GLM)**

### 2. 输入 API Key

在配置界面粘贴获取的 API Key

### 3. 选择模型

从推荐模型列表中选择适合的模型

### 4. 调整参数（可选）

- **Max Tokens**：根据模型能力调整
- **Temperature**：控制输出的随机性（0.0-1.0）
- **Top P**：控制词汇选择范围（0.0-1.0）

## ⚠️ 注意事项

### MiniMax 注意事项

1. **必须使用 Coding Plan Key**，普通 API Key 无法使用
2. **极速版模型**价格更高但速度更快，适合实时场景
3. **支持多模态**：文本、语音、图像、视频、音乐生成
4. **中文支持优秀**：在中文场景下表现优异

### 智谱AI注意事项

1. **支持 JWT Token 鉴权**：提供更高安全性
2. **多模态支持**：GLM-4V 视觉模型，CogView 图像生成
3. **开源友好**：提供丰富的开源工具和文档
4. **中文优化**：在中文理解和生成方面表现优异

## 🔗 相关链接

### MiniMax
- 官方平台：https://platform.minimaxi.com
- 文档中心：https://platform.minimaxi.com/docs
- 定价信息：https://platform.minimaxi.com/docs/guides/pricing-paygo

### 智谱AI
- 官方平台：https://open.bigmodel.cn
- API 文档：https://docs.bigmodel.cn/cn/guide/develop/http/introduction
- 定价信息：https://bigmodel.cn/pricing

## 📞 技术支持

如果在使用过程中遇到问题，请：

1. 检查 API Key 是否正确
2. 确认网络连接正常
3. 查看厂商官方文档
4. 在项目 Issues 中反馈问题

---

*最后更新：2026-04-21*