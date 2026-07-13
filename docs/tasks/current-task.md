# 当前任务

## 阶段
STAGE-02-APP-SHELL

## 任务编号
STAGE-02-TASK-05

## 类型
SCAFFOLD

## 状态
READY

## 组件
App Single Instance Mutex + WebView2 Runtime Detection

## 下一执行者
Cursor

---

## 设计依据

[02-architecture.md:290-316](docs/design/02-architecture.md): Single Instance via Mutex
[02-architecture.md:182-188](docs/design/02-architecture.md): WebView2 Runtime detection
[02-architecture.md:411-466](docs/design/02-architecture.md): Startup Sequence

---

## 前置条件

WebView2 Runtime 检测需要 SDK。必须修改 App.csproj 添加 NuGet 引用。

---

## 允许修改的精确路径

- `src/Anhei4Map.App/Anhei4Map.App.csproj`（添加 PackageReference）
- `src/Anhei4Map.App/App.xaml.cs`（修改：添加 Mutex + Runtime 检测）
- `src/Anhei4Map.App/App.xaml`（如需要调整，但通常不需要）

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.Infrastructure/**`
- `tests/**`
- `docs/design/**`
- `anhei4-map.sln`
- `MainWindow.xaml` / `MainWindow.xaml.cs`
- 不创建 MainWindow
- 不实现 WebView2 控件或导航
- 不实现热键注册
- 不实现状态机调用
- 不实现 TASK-06+

---

## NuGet 依赖

在 `src/Anhei4Map.App/Anhei4Map.App.csproj` 中添加：

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2903.40" />
</ItemGroup>
```

（使用当前最新的 1.0.x 稳定版本，`dotnet restore` 自动解析）

---

## 实现 1: 单实例 Mutex

修改 `src/Anhei4Map.App/App.xaml.cs`：

```csharp
using System.Windows;

namespace Anhei4Map.App;

public partial class App : Application
{
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, @"Global\Anhei4Map_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            Shutdown();
            return;
        }

        // Runtime check goes here (see below)
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}
```

**规则：**
- Mutex 名称：`Global\Anhei4Map_SingleInstance`
- 已有实例 → Dispose Mutex → Shutdown()（不弹消息框，静默退出）
- 不创建自定义 Mutex 封装类；直接写在 App 中
- 不记录日志（日志集成属于后续任务，当前是 SCAFFOLD）

---

## 实现 2: WebView2 Runtime 检测

在 `OnStartup` 中 Mutex 检查之后添加：

```csharp
try
{
    var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
    // Runtime 已安装 → 继续
}
catch (WebView2RuntimeNotFoundException)
{
    MessageBox.Show(
        "Microsoft Edge WebView2 Runtime 未安装。\n请从以下链接下载 Evergreen Bootstrapper 后重试：\n\nhttps://go.microsoft.com/fwlink/p/?LinkId=2124703",
        "缺少必需组件",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
    Shutdown();
    return;
}
```

**规则：**
- 只用 `CoreWebView2Environment.GetAvailableBrowserVersionString()` 检测
- 只 catch `WebView2RuntimeNotFoundException`；其他异常不吞
- 显示中文错误消息（含下载链接）
- 用户点确定后 Shutdown（exit code 1）
- Runtime 存在则**不创建 Environment**（由 TASK-06 创建）
- 不记录日志

---

## 启动顺序（冻结）

```
OnStartup:
  1. Mutex check  → collision → Shutdown
  2. Runtime check → missing  → MessageBox → Shutdown
  (3-4 by TASK-06: Create MainWindow)
```

---

## 验证命令

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
git diff --check
```

## 验证标准

- `dotnet restore` 成功（下载 WebView2 NuGet）
- `dotnet build -c Release` 0 errors, 0 warnings
- `dotnet test -c Release --no-build` 全部通过 (160/160)
- SCAFFOLD 类型无需新增测试
- `git diff --check` clean

---

## Git 提交信息

```
feat: add single instance mutex and WebView2 runtime check

SCAFFOLD: App.xaml.cs now enforces single instance via named Mutex and
checks for WebView2 Runtime availability at startup. Adds Microsoft.Web.
WebView2 NuGet reference to App project.
```

---

## Cursor 最终报告格式

```
TASK_COMPLETE

Task: STAGE-02-TASK-05
Component: App Single Instance + Runtime
Type: SCAFFOLD
Status: DONE
Commit: <hash>
Files Modified:
  - src/Anhei4Map.App/Anhei4Map.App.csproj
  - src/Anhei4Map.App/App.xaml.cs
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
NuGet Added: Microsoft.Web.WebView2
```
