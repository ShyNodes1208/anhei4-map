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

## 任务状态

`docs/tasks/current-task.md` 必须包含以下状态之一：

| 状态 | 含义 | Cursor 行为 |
|------|------|------------|
| UNASSIGNED | 尚未分配 | 停止，等待 Claude 派发 |
| READY | 已派发，可执行 | 允许执行 |
| IN_PROGRESS | Cursor 正在执行 | 不重复执行 |
| BLOCKED | 被前置条件阻塞 | 停止，等待 Claude 解除 |
| DONE | 已完成并验收 | 停止，等待下一任务 |

只有 **READY** 状态允许 Cursor 执行。任何其他状态 Cursor 必须停止。

## 任务类型

| 类型 | 强制 RED 测试 | 验证方式 |
|------|--------------|---------|
| SCAFFOLD | 否 | `dotnet restore` + `dotnet build` + `dotnet test` |
| BEHAVIOR | 是 | 先写失败测试，RED → GREEN → REFACTOR |
| FIX | 是 | 先写回归测试复现缺陷 |
| DOCS | 否 | 文档完整性检查、链接有效性 |
| CONFIG | 视情况 | 可自动测试则测试，否则人工验证 |
| RELEASE | 否 | 发布 + smoke test |

SCAFFOLD 任务不得编造没有业务价值的假测试来满足 TDD 形式。

## Cursor 零产出分类

当 Cursor 返回零代码产出时，Claude 必须先分类再行动：

| 分类 | 条件 | Claude 行动 |
|------|------|------------|
| BLOCKED_NO_TASK | 无正式任务派发，或 current-task.md 为 UNASSIGNED | Claude 补齐任务，重新派发；不是返工 |
| TASK_NOT_EXECUTED | 有 READY 任务但 Cursor 未执行 | 重新派发同一任务；不是 FIX |
| TASK_FAILED | 有代码产出但验收不通过 | 生成 `<task>-FIX1` 返工任务 |

**FIX1 只能用于：** 存在有效实现但代码、测试或验收存在明确问题的场景。不得用于无代码产出或任务未派发的情况。

## Agent 交接门禁

Claude 派发 Cursor 前必须输出 `HANDOFF_READY`，包含：

```
Branch:
Worktree:
Plan:
Task:
Task Type:
Task Status:
Allowed Paths:
Verification Commands:
```

Cursor 在 current-task.md 中找不到以上任一字段时必须停止，等待 Claude 补全。

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
