# Shared Agent Instructions

## 开始任务前必须读取

- CLAUDE.md
- AGENTS.md
- docs/design/*
- 当前阶段计划
- docs/tasks/current-task.md
- docs/status/project-status.md

## Cursor TDD 纪律

### 任务状态检查（第一步）

执行前必须读取 `docs/tasks/current-task.md`，确认 **任务状态** = `READY`。

状态为 UNASSIGNED、BLOCKED、DONE 时停止。状态为 IN_PROGRESS 时不重复执行。

确认以下字段全部存在：

- Branch
- Worktree
- Task
- Task Type
- Task Status
- Allowed Paths
- Verification Commands

缺少任一字段时停止，等待 Claude 补全。

### 执行纪律（按任务类型）

**BEHAVIOR 和 FIX 类型：**

1. 先写失败测试。
2. 运行定向测试并确认预期失败。
3. 写最小实现。
4. 运行定向测试。
5. 重构。
6. 运行相关完整测试。
7. git diff --check。
8. 检查 diff。
9. 原子提交。

**SCAFFOLD 类型：**

1. 按 current-task.md 的 GREEN 最小实现执行。
2. 运行验证命令（restore → build → test）。
3. git diff --check。
4. 检查 diff —— 确认未修改禁止路径。
5. 原子提交。

**不得**为 SCAFFOLD 任务编造没有业务价值的假测试。验证方式是构建和命令级检查，不是单元测试。

### 有效 RED 标准

**有效 RED（可接受）：**
- 测试断言失败（预期 true，实际 false）
- 测试引用尚不存在的目标类型/方法/成员 -> CS0234/CS0117/CS1061
- 测试先于实现创建，语法正确，项目引用正确

**无效 RED（不可接受）：**
- 测试自身语法错误、拼写错误、错误命名空间
- 错误项目引用、缺少无关依赖、环境未安装
- 故意破坏项目文件、与目标行为无关的编译失败

**优先：** 可先建最小骨架让 RED 表现为断言失败，但不强制额外提交。

**DOCS / CONFIG / RELEASE 类型：**

按 current-task.md 的具体指令执行，不强制 RED-GREEN-REFACTOR。

### 无法单元测试的 Windows UI 行为

- 把 Win32/WPF 调用隔离在接口后；
- 单元测试状态和决策逻辑；
- 增加人工 smoke test；
- 不写没有断言价值的假测试。

## 环境依赖规则（所有 Agent）

1. Agent 可以检测系统依赖（如 `dotnet --version`、`node --version`）。
2. Agent **不得**自行安装、升级或卸载系统软件。
3. 缺少依赖时必须返回 **BLOCKED_ENVIRONMENT**。
4. 必须列出：
   - 缺少的工具
   - 最低版本
   - 推荐官方安装方式
   - 安装影响
5. 只有用户明确确认后，才能执行安装。
6. **不得**自行使用 winget、Chocolatey、npm -g、PowerShell 安装脚本或修改系统 PATH。

## 依赖路径规则（所有 Agent）

当任务需要新增项目引用（ProjectReference）或 NuGet 包引用时，`current-task.md` 的允许修改清单必须显式包含对应 `.csproj` 文件。

Claude 派发任务前检查：如果新代码需要引用另一个项目或包，必须在允许修改中列出 csproj。遗漏不构成 Cursor 错误——分类为 TASK_SPEC_PATH_OMISSION，记录偏差并更新规则。

## Codex Review guidelines

重点检查：

- 行为缺陷
- 资源泄漏
- 热键生命周期
- 鼠标穿透恢复
- UI 线程错误
- WebView2 初始化和导航
- 重定向绕过
- 重试风暴
- 设置损坏
- 多显示器越界
- 单实例竞争
- 安全边界
- 范围偏差

严重级别：BLOCKER、HIGH、MEDIUM、LOW、APPROVED。

每个问题必须有文件、位置、证据、影响、最小修复建议。
