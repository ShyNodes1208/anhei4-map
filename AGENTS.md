# Shared Agent Instructions

## 开始任务前必须读取

- CLAUDE.md
- AGENTS.md
- docs/design/*
- 当前阶段计划
- docs/tasks/current-task.md
- docs/status/project-status.md

## Cursor TDD 纪律

1. 先写失败测试。
2. 运行定向测试并确认预期失败。
3. 写最小实现。
4. 运行定向测试。
5. 重构。
6. 运行相关完整测试。
7. git diff --check。
8. 检查 diff。
9. 原子提交。

无法单元测试的 Windows UI 行为：

- 把 Win32/WPF 调用隔离在接口后；
- 单元测试状态和决策逻辑；
- 增加人工 smoke test；
- 不写没有断言价值的假测试。

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
