# 当前任务

## 阶段
STAGE-02-APP-SHELL

## 任务编号
STAGE-02-TASK-06

## 类型
SCAFFOLD

## 状态
READY

## 组件
MainWindow + WebView2 Control Shell

## 下一执行者
Cursor

---

## TASK-06 / TASK-07 边界

| 内容 | TASK-06 | TASK-07 |
|------|---------|---------|
| MainWindow XAML + 属性 | 本任务 | — |
| WebView2 控件声明 | 本任务 | — |
| 窗口位置/大小恢复 | 本任务 | — |
| App.xaml StartupUri 移除 | 本任务 | — |
| 在 OnStartup 中显示窗口 | 本任务 | — |
| CoreWebView2 Environment 创建 | — | TASK-07 |
| EnsureCoreWebView2Async | — | TASK-07 |
| 导航到 helltides.com | — | TASK-07 |
| NavigationStarting 白名单 | — | TASK-07 |
| 其他事件处理 | — | TASK-07 |

---

## 允许修改的精确路径

- `src/Anhei4Map.App/MainWindow.xaml`（重写）
- `src/Anhei4Map.App/MainWindow.xaml.cs`（重写）
- `src/Anhei4Map.App/App.xaml`（移除 StartupUri）
- `src/Anhei4Map.App/App.xaml.cs`（OnStartup 中创建并显示 MainWindow）

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.Infrastructure/**`
- `tests/**`
- `docs/design/**`
- `*.csproj`
- `*.sln`
- 不创建 CoreWebView2Environment
- 不调用 EnsureCoreWebView2Async
- 不执行导航
- 不设置导航白名单
- 不注册事件处理
- 不处理关闭/保存逻辑
- 不实现 TASK-07

---

## 步 1：App.xaml — 移除 StartupUri

```xml
<Application x:Class="Anhei4Map.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:Anhei4Map.App">
    <Application.Resources>

    </Application.Resources>
</Application>
```

删除 `StartupUri="MainWindow.xaml"` 属性。MainWindow 改由 App.xaml.cs 代码创建。

---

## 步 2：MainWindow.xaml

```xml
<Window x:Class="Anhei4Map.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:wpf="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
        WindowStyle="None"
        ResizeMode="CanResize"
        Topmost="True"
        ShowInTaskbar="False"
        Width="640"
        Height="360"
        Background="Transparent">
    <Grid>
        <wpf:WebView2 x:Name="webView" />
    </Grid>
</Window>
```

**冻结属性：**

| 属性 | 值 | 原因 |
|------|-----|------|
| WindowStyle | None | 无边框 |
| ResizeMode | CanResize | 允许用户调整大小 |
| Topmost | True | 始终置顶 |
| ShowInTaskbar | False | 不显示在任务栏 |
| Width | 640 | 默认宽度 |
| Height | 360 | 默认高度 |
| Background | Transparent | 透明背景 |
| WebView2 Name | webView | 控件名称 |

不带 `AllowsTransparency="True"`——该属性与 WebView2 的硬件渲染不兼容。

---

## 步 3：MainWindow.xaml.cs

```csharp
using System.Windows;

namespace Anhei4Map.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

- 构造函数仅调用 `InitializeComponent()`
- 不创建 CoreWebView2Environment
- 不调用 EnsureCoreWebView2Async
- 不设置 WebView2 Source
- 不注册任何事件处理
- 不处理加载、关闭、日志或设置

---

## 步 4：App.xaml.cs — 创建并显示 MainWindow

修改 `OnStartup` 的最后部分（Runtime 检查通过之后、`base.OnStartup(e)` 之前）：

```csharp
// Runtime check passed — create and show the main window
var mainWindow = new MainWindow();

// Load settings and restore window bounds
try
{
    var settingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Anhei4Map");
    var settingsStore = new JsonSettingsStore(settingsDir);
    var settings = await settingsStore.LoadAsync();
    var placement = WindowBoundsNormalizer.Normalize(
        settings.Placement,
        []); // Empty work areas → falls back to safe defaults via existing logic

    mainWindow.Left = placement.Left;
    mainWindow.Top = placement.Top;
    mainWindow.Width = placement.Width;
    mainWindow.Height = placement.Height;
}
catch
{
    // Use XAML defaults (640x360) on any load failure
}

mainWindow.Show();
base.OnStartup(e);
```

注意：由于 `OnStartup` 不是 async，设置加载使用同步等待：

```csharp
var settings = settingsStore.LoadAsync().GetAwaiter().GetResult();
```

- 设置加载失败（任何异常）→ 使用 XAML 默认值 640×360
- 不记录日志
- 不带入工作区数组（留空让 Normalizer 回退到安全默认值）
- 不使用 IWin32Interop 获取真实显示器

---

## 步 5：MainWindow.xaml.cs 中的 WS_EX_TOOLWINDOW

在 MainWindow 构造函数中添加：

```csharp
public MainWindow()
{
    InitializeComponent();
    SourceInitialized += (_, _) => SetToolWindow();
}

private void SetToolWindow()
{
    var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
    var exStyle = Win32Native.GetWindowLongPtr64(hwnd, Win32Native.GWL_EXSTYLE);
    Win32Native.SetWindowLongPtr64(hwnd, Win32Native.GWL_EXSTYLE,
        new IntPtr(exStyle.ToInt64() | Win32Native.WS_EX_TOOLWINDOW));
}
```

或使用封装的 `Win32Interop` 如果已可用。若 `Win32Interop` 已可用则优先使用它。

- WS_EX_TOOLWINDOW 使窗口不在 Alt+Tab 列表中显示
- 在 SourceInitialized 事件中设置（需要窗口句柄）

---

## 验证命令

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
git diff --check
```

## 验证标准

- `dotnet build -c Release` 0 errors, 0 warnings
- `dotnet test -c Release --no-build` 全部通过 (160/160)
- `git diff --check` clean

---

## Git 提交信息

```
feat: create MainWindow shell with WebView2 control

SCAFFOLD: MainWindow is a borderless, topmost, transparent-background
window hosting a named WebView2 control. App.xaml StartupUri removed;
window created and shown in OnStartup. Settings loaded to restore
window bounds with fallback to 640x360 defaults. WebView2 navigation
and event handling deferred to TASK-07.
```

---

## Cursor 最终报告格式

```
TASK_COMPLETE

Task: STAGE-02-TASK-06
Component: MainWindow + WebView2 Control Shell
Type: SCAFFOLD
Status: DONE
Commit: <hash>
Files Modified:
  - src/Anhei4Map.App/MainWindow.xaml
  - src/Anhei4Map.App/MainWindow.xaml.cs
  - src/Anhei4Map.App/App.xaml
  - src/Anhei4Map.App/App.xaml.cs
WebView2 Control: Named "webView", no navigation
Window Properties: WindowStyle=None, Topmost=True, ShowInTaskbar=False
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
```
