# PedAndPaw_AI (PAPAI) — 中文说明

[English](../README.md) · **中文**

PedAndPaw_AI 是一个面向 **GTA V Enhanced** 的 ScriptHookVDotNet Enhanced (SHVDNE) 模组：
把洛圣都的街头变成可以聊天的地方。你可以和行人、警察、急救人员交谈，也可以和老鼠、狗、
猫、乌鸦甚至鲨鱼说话。每个 NPC 都有自己的心情、记忆和性格，回答会随时间和天气、你开的车
以及你手里拿着什么而变化。你也可以向 NPC 借车，或者和一只爱挖苦人的海鸥闲聊。

**没有暴力**：动物永远不会受伤，枪械只在靶场可用。**默认不出现裸体**，可在设置中调整。

## 最大的不同：可以完全离线

搭配本地模型（例如 Ollama）时，这个模组不需要 API key、不需要账号，玩家输入的每一句话
都**不会离开这台电脑**。没有任何设置会生成性内容。

## 功能

- 按 **E** 与最近的 NPC 或动物对话，按 **T** 给附近的人发文字消息，按 **Esc** 结束
- 每个 NPC：性格、心情、短期记忆、对你的态度
- 环境感知：时间、天气、地区、载具、通缉等级、武器状态、附近的枪声
- 角色文件是纯文本（`.txt`），玩家可以自己写角色，不需要编程
- 支持任何 OpenAI 兼容接口：Gemini、本地 Ollama、OpenRouter、Groq 或自建服务
- 面向 **13 岁以上**：没有血腥、没有毒品、没有侮辱性语言；裸体可以开启，性内容永远不允许

## 安装

1. 安装 GTA V Enhanced 和 ScriptHookVDotNet Enhanced
2. 把本仓库 `scripts/` 文件夹的内容复制到游戏的 `scripts/` 目录
3. 直接启动游戏即可；默认配置在 `scripts/PedAndPaw_AI.ini`

## 选择模型提供方

**本地模型（免费、离线）**

```ini
[Ai]
Provider = ollama
BaseUrl = http://localhost:11434/v1
Model = llama3.2:3b
```

**Google Gemini**

```ini
[Ai]
Provider = gemini
BaseUrl = https://generativelanguage.googleapis.com/v1beta/openai/
Model = gemini-3-flash-preview
ApiKeyEnvVar = PEDANDPAW_AI_KEY
```

API key 只从环境变量读取，绝不会写进文件。若启动器不传递环境变量，也可以把 key 放在
游戏 `scripts/PedAndPaw_AI.key` 这个文件里（已被 git 忽略）。

## 自己写角色

在 `scripts/PedAndPaw_AI/roles/humans/` 或 `roles/animals/` 里放一个 `.txt`，第一行是：

```
Role layer, Police:
```

后面是角色的说话方式，然后是两个必需的段落：`=== MODES ===`（Friendly、Grumpy、Scared、
Aggressive、Philosophical）和 `=== WEAPON REACTIONS ===`（WeaponDrawn、WeaponAimed、
WeaponFired）。完整语法见 [PROFILES.md](PROFILES.md)，写好后用
`python3 tools/validate_roles.py scripts/PedAndPaw_AI/roles` 检查。

> 本文件是英文 README 的中文节选，如有歧义以英文版为准。欢迎指出翻译问题。
