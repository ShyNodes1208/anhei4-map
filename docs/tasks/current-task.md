# 当前任务

## 任务编号
STAGE-01-TASK-09

## 任务名称
构建脚本、测试脚本和基础 README

## 任务类型
CONFIG

## 状态
READY

## 所属阶段
STAGE-01-FOUNDATION（Stage 01 最后一个任务）

## 前置条件
STAGE-01-TASK-08 完成——82 个测试通过。

## 任务目标
提供 `scripts/build.ps1` 和 `scripts/test.ps1` 构建/测试入口，完善 `README.md` 为当前阶段状态。

## 允许修改
- Create/Modify: `scripts/build.ps1`
- Create/Modify: `scripts/test.ps1`
- Modify: `README.md`

## 禁止修改
- 不得修改 `src/**`、`tests/**`、`docs/design/**`、`docs/plans/**`
- 不得添加 NuGet、CI、发布脚本

## build.ps1 要求
- `Set-StrictMode -Version Latest; $ErrorActionPreference = "Stop"`
- 基于 `$PSScriptRoot` 计算仓库根，不依赖当前目录
- 检查 `dotnet` 和 `anhei4-map.sln` 存在
- `dotnet restore` → `dotnet build -c Release --no-restore`
- 每步检查 `$LASTEXITCODE`，失败立即退出
- 不调用 winget/Chocolatey/安装脚本

## test.ps1 要求
- 同上 StrictMode + ErrorAction
- 独立执行 restore → build → test
- `dotnet test -c Release --no-build`
- 失败返回非零退出码

## README 要求
- 项目名、目标、当前状态（Stage 01 Foundation）
- 已完成 8 个组件（列出）
- 环境：Win10/11, .NET 8 SDK, Git
- 构建/测试命令可直接复制执行
- 安全边界、后续阶段说明

## 验证命令
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

## 完成标准
- build.ps1 成功 → 0 warnings 0 errors
- test.ps1 成功 → 82+ tests pass
- README 命令可复制执行
- git diff --check clean

## Git 提交信息
```
chore: add build and test scripts with foundation documentation
```
