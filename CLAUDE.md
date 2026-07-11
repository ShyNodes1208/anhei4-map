# anhei4-map

## 项目目标

开发 Windows 桌面置顶小地图，使用 WebView2 显示 https://helltides.com/，用于 Diablo IV 无边框窗口模式。

## Agent 职责

### Claude Code + DeepSeek

负责产品、架构、Gstack、Superpowers、Worktree、任务、审核裁决、验证、合并和发布。
原则上不得修改 src 和 tests。

### Cursor

只按 docs/tasks/current-task.md 写测试和代码并提交。
不得改需求、架构和范围。

### Codex

只做独立审核和复审，默认不得修改生产代码和测试代码。

## 固定技术栈

- Windows 10/11
- C#
- .NET 8
- WPF
- Microsoft.Web.WebView2
- xUnit
- Win32 RegisterHotKey
- Win32 扩展窗口样式
- JSON 本地设置
- Git Worktree

未经用户确认不得替换。

## 安全红线

不得读取游戏内存、游戏文件、游戏网络，不得注入、Hook、自动键鼠、自动寻路、获取玩家坐标、调用私有 API、抓取或重新分发地图素材。

## 工作流

设计：/office-hours → /plan-ceo-review → /plan-eng-review → 冻结基线。

开发：using-git-worktrees → writing-plans → current-task → Cursor TDD → Claude 检查 → Codex 审核 → Claude 裁决 → Cursor 修复 → Codex 复审 → verification-before-completion → finishing-a-development-branch。

## Git 规则

- main 只保存已验收阶段
- 禁止直接在 main 写业务代码
- 每阶段独立 Worktree
- 禁止 force push
- 合并使用 --no-ff
- 没有新鲜测试证据不得声称完成

## 事实来源优先级

CLAUDE.md → AGENTS.md → docs/design → 当前计划 → current-task → Git → 聊天内容。

## Gstack 技能

Gstack 已安装于 `~/.claude/skills/gstack`（v1.60.1.0）。

本项目的 Gstack 技能仅用于设计、架构、评审和调查阶段，由 Claude Code + DeepSeek 调用。不得通过 Gstack 技能直接修改 src 或 tests。

### 可用技能

| 技能 | 用途 | 项目阶段 |
|------|------|----------|
| `/office-hours` | 需求诊断与头脑风暴 | 设计 |
| `/plan-ceo-review` | CEO 视角战略评审 | 设计 |
| `/plan-eng-review` | 工程架构评审 | 设计 |
| `/review` | 代码评审（含专家面板） | 开发 |
| `/qa` | QA 测试与验证 | 开发 |
| `/cso` | OWASP/STRIDE 安全审计 | 开发/发布 |
| `/investigate` | 系统化根因调查 | 调试 |

### 角色分工不变

Gstack 技能遵循 CLAUDE.md 既定角色分工：Claude Code 负责产品、架构、评审、裁决、合并和发布；Cursor 负责写代码；Codex 负责独立审核。Gstack 不引入自动编码行为。
