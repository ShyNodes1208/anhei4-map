# anhei4-map

Windows 桌面置顶 Helltides 地图工具（Diablo IV 无边框窗口模式）。

## 当前状态

**Stage 01 — Foundation**（`feature/01-foundation`）

本阶段已完成可测试的 Core/Infrastructure 纯逻辑与项目骨架。**尚未实现** WebView2 宿主、置顶窗口、鼠标穿透、全局快捷键或完整 UI 行为（计划在后续 Stage 中交付）。

## 已完成组件（Stage 01）

| 组件 | 说明 |
|------|------|
| 解决方案骨架 | `anhei4-map.sln`，App / Core / Infrastructure / Tests 四层结构 |
| AppSettings | 默认配置与输入校验（12 项单元测试） |
| JsonSettingsStore | JSON 设置保存/读取（6 项测试） |
| 设置恢复 | 原子写入 + 损坏 JSON `.bak` 备份（8 项测试） |
| WindowBoundsNormalizer | 多显示器窗口边界规范化（14 项测试） |
| DomainPolicy | Helltides HTTPS 域名白名单（20 项测试） |
| RetryPolicy | 有限指数退避重试策略（10 项测试） |
| FileLogger | 本地文件日志抽象与实现（12 项测试） |

当前单元测试：**82** 项全部通过。

## 环境要求

- Windows 10 或 Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

> **后续阶段**将需要 [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)。Stage 01 构建与测试不依赖 WebView2。

## 构建

在仓库根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

或从任意目录（脚本通过 `$PSScriptRoot` 定位仓库）：

```powershell
powershell -ExecutionPolicy Bypass -File D:\path\to\anhei4-map\scripts\build.ps1
```

## 测试

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

`test.ps1` 独立执行 restore → build → test，失败时返回非零退出码。

也可直接使用 dotnet：

```powershell
dotnet test anhei4-map.sln -c Release
```

## 仓库结构

```
anhei4-map/
├── scripts/
│   ├── build.ps1          # Release 构建
│   └── test.ps1           # Release 测试
├── src/
│   ├── Anhei4Map.App/     # WPF 应用（Stage 01：模板骨架）
│   ├── Anhei4Map.Core/    # 纯逻辑（可单元测试）
│   └── Anhei4Map.Infrastructure/  # 文件 IO、日志
├── tests/
│   └── Anhei4Map.Tests/   # xUnit 单元测试
└── docs/                  # 设计与任务文档
```

## 安全边界

- 不读取游戏进程、内存、文件或网络
- 不注入、Hook 或模拟游戏键鼠
- 不调用 Helltides 私有 API 或抓取地图素材
- WebView2 导航将限制为 `https://helltides.com` / `https://www.helltides.com`（策略已在 Core 实现，UI 集成待后续 Stage）

## 后续阶段（未开始）

- Stage 02+：WebView2 集成、置顶窗口、编辑/锁定/隐藏状态机、全局快捷键、鼠标穿透、设置持久化 UI 绑定等

## 技术栈

C# / .NET 8 / WPF / xUnit / System.Text.Json
