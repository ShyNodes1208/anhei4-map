# 当前任务

## 阶段
STAGE-04

## 状态
READY

## 当前任务
STAGE-04-TASK-01

## 任务名称
Minimal map viewport diagnostics

## 计划审批
APPROVED_BY_CODEX

## 用户批准
APPROVED

## Cursor 派发
ALLOWED

## 下一执行者
Cursor

---

## HANDOFF_READY

Stage: STAGE-04
Task ID: STAGE-04-TASK-01
Task Type: DIAGNOSTIC_IMPLEMENTATION
Branch: feature/04-map-viewport-redesign
Worktree: D:\AIProjects\anhei4-map-worktrees\stage-04-map-viewport-redesign
Base Commit: 04673bdfcf8aa35ce37197df499415f091ac98af
Objective: 使用最小诊断能力确定地图截图约为 3.17:1、纵向显示区域过短的真实原因。

## Allowed Paths
- src/Anhei4Map.App/App.xaml.cs
- src/Anhei4Map.App/RendererWindow.xaml.cs
- src/Anhei4Map.App/Diagnostics/MapViewportDiagnostics.cs

## Forbidden Paths
其他 src/**, tests/**, *.csproj, *.sln, NuGet, OverlayWindow, DomainPolicy, MainWindow, MapRegion model.

## Output
%LocalAppData%\Anhei4Map\diagnostics\latest\ — diagnostics.json, capture-full.png, capture-crop.png

## Verification
dotnet restore + build 0e0w + tests all pass + git diff --check clean

## Commit Message
feat: add minimal map viewport diagnostics

## Cursor Rules
- Only modify Allowed Paths
- Do NOT push, do NOT merge
- Read docs/governance/project-development-rules.md first
- Overengineering Check MUST PASS before commit
