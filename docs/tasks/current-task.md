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
| MainWindow XAML | 本任务 | — |
| WebView2 控件声明 | 本任务 | — |
| App.xaml StartupUri 移除 | 本任务 | — |
| OnStartup → MainWindow.Show() | 本任务 | — |
| 设置加载 + 窗口边界恢复 | 本任务 | — |
| WS_EX_TOOLWINDOW 应用 | 本任务 | — |
| CoreWebView2Environment 创建 | — | TASK-07 |
| EnsureCoreWebView2Async | — | TASK-07 |
| 导航 helltides.com | — | TASK-07 |
| NavigationStarting 白名单 | — | TASK-07 |
| 所有 WebView2 事件处理 | — | TASK-07 |

---

## 允许修改的精确路径

- `src/Anhei4Map.App/MainWindow.xaml`（重写）
- `src/Anhei4Map.App/MainWindow.xaml.cs`（重写）
- `src/Anhei4Map.App/App.xaml`（移除 StartupUri）
- `src/Anhei4Map.App/App.xaml.cs`（OnStartup 中补充启动流程）

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.Infrastructure/**`
- `tests/**`
- `docs/design/**`
- `*.csproj`
- `*.sln`
- 不创建 CoreWebView2Environment
- 不调用 EnsureCoreWebView2Async
- 不设置 WebView2.Source
- 不注册任何 WebView2 事件
- 不导航任何 URL
- 不处理窗口 Closing
- 不保存设置
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

**冻结属性表：**

| 属性 | 值 | 原因 |
|------|-----|------|
| WindowStyle | None | 无边框 |
| ResizeMode | CanResize | 允许用户调整大小 |
| Topmost | True | 始终置顶 |
| ShowInTaskbar | False | 不显示在任务栏 |
| Width | 640 | 默认宽度 |
| Height | 360 | 默认高度 |
| Background | Transparent | 透明画刷——避免窗口加载闪烁时的非透明底色；WebView2 填充整个客户区后看不到 |
| AllowsTransparency | **不设置（默认 False）** | True 与 WebView2 硬件渲染不兼容，会导致控件变黑/不可见 |
| WebView2 Name | webView | 供 TASK-07 引用 |
| Grid 子元素 | 仅 WebView2 | 单一控件填满窗口客户区 |
| WebView2.Source | **不设置** | 保持 null |

---

## 步 3：MainWindow.xaml.cs

```csharp
using System.Windows;
using System.Windows.Interop;

namespace Anhei4Map.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var win32 = new Win32Interop();

        // IntPtr.Zero 是合法的零样式值——始终 OR 追加 WS_EX_TOOLWINDOW
        var exStyle = win32.GetWindowLongPtr(hwnd, Win32Native.GWL_EXSTYLE);
        var newExStyle = new IntPtr(
            exStyle.ToInt64() | unchecked((long)Win32Native.WS_EX_TOOLWINDOW));
        win32.SetWindowLongPtr(hwnd, Win32Native.GWL_EXSTYLE, newExStyle);

        // SetWindowPos 刷新窗口样式
        win32.SetWindowPos(
            hwnd,
            Win32Native.HWND_TOPMOST,
            0, 0, 0, 0,
            Win32Native.SWP_NOACTIVATE |
            Win32Native.SWP_NOMOVE |
            Win32Native.SWP_NOSIZE |
            Win32Native.SWP_SHOWWINDOW);
    }
}
```

**WS_EX_TOOLWINDOW 规则（冻结）：**

| 事项 | 规则 |
|------|------|
| 生命周期点 | `SourceInitialized` 事件（此时 HWND 已可用） |
| HWND 获取 | `new WindowInteropHelper(this).Handle` |
| 读取现有扩展样式 | `GetWindowLongPtr(hwnd, GWL_EXSTYLE)` |
| 标志常量 | `WS_EX_TOOLWINDOW = 0x00000080` |
| 计算新样式 | `new IntPtr(exStyle.ToInt64() | 0x80)` —— **始终执行**，即使 exStyle==IntPtr.Zero（零也是合法值） |
| 不判断失败 | 不根据 GetWindowLongPtr 或 SetWindowLongPtr 返回零判断 API 失败（零可能是合法旧值） |
| 不调 GetLastError | 不调 `Marshal.GetLastWin32Error()`，不抛 `Win32Exception`（保持 TASK-04 冻结规则） |
| SetWindowPos 刷新 | 必须调用（新样式需要 `SetWindowPos` 生效） |
| SetWindowPos flags | `SWP_NOACTIVATE \| SWP_NOMOVE \| SWP_NOSIZE \| SWP_SHOWWINDOW` |
| 只添加 WS_EX_TOOLWINDOW | 不添加 WS_EX_TRANSPARENT、WS_EX_LAYERED、鼠标穿透或热键注册 |

**MainWindow 构造函数规则：**
- 无参构造，不注入任何依赖
- 直接在构造函数中创建 `new Win32Interop()`
- 不访问 Infrastructure 层
- 不依赖 IWin32Interop 接口注入

---

## 步 4：App.xaml.cs — 启动流程

在 `OnStartup` 中 Runtime 检查通过后、`base.OnStartup(e)` 之前插入：

```csharp
// 3. Load settings
var settingsDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Anhei4Map");
var settingsStore = new JsonSettingsStore(settingsDir);
AppSettings settings;
try
{
    settings = settingsStore.LoadAsync().GetAwaiter().GetResult();
}
catch
{
    settings = AppSettings.CreateDefaults();
}

// 4. Get monitor working areas for bounds normalization
WorkArea[] workAreas;
try
{
    var win32 = new Win32Interop();
    var screenInfos = win32.GetMonitorWorkingAreas();
    workAreas = screenInfos
        .Select(s => new WorkArea(s.Left, s.Top, s.Width, s.Height))
        .ToArray();
}
catch
{
    workAreas = [];
}

// 5. Normalize window bounds
var placement = WindowBoundsNormalizer.Normalize(settings.Placement, workAreas);

// 6. Create and show MainWindow
var mainWindow = new MainWindow
{
    Left = placement.Left,
    Top = placement.Top,
    Width = placement.Width,
    Height = placement.Height,
    Opacity = placement.Opacity
};
mainWindow.Show();
```

**启动流程（冻结顺序）：**

| 步 | 操作 | 失败处理 |
|----|------|----------|
| 1 | Mutex 检查 | 已有实例 → Shutdown（TASK-05 已实现） |
| 2 | Runtime 检测 | 缺失/异常 → MessageBox + Shutdown（TASK-05 已实现） |
| 3 | 加载设置 | 任何异常 → `AppSettings.CreateDefaults()` |
| 4 | 获取工作区 | 任何异常 → `Array.Empty<WorkArea>()` |
| 5 | 规范化窗口边界 | 纯逻辑，不抛异常 |
| 6 | 创建 MainWindow + 赋值 Left/Top/Width/Height/Opacity + Show() | 不抛异常 |
| — | base.OnStartup(e) | 最后调用 |

**设置恢复规则（冻结）：**

| 事项 | 规则 |
|------|------|
| 设置目录 | `%LocalAppData%\Anhei4Map` |
| 加载方式 | 同步等待 `LoadAsync().GetAwaiter().GetResult()` |
| 加载失败 | 使用 `AppSettings.CreateDefaults()` |
| 工作区获取 | `Win32Interop.GetMonitorWorkingAreas()` → `ScreenInfo[]` → `WorkArea[]` |
| 工作区获取失败 | 空 `WorkArea[]`（Normalizer 有安全回退） |
| 应用字段 | `Left, Top, Width, Height, Opacity` |
| ZoomLevel | 不应用到 MainWindow（由 WebView2 缩放控制，Stage 03+） |
| 不保存设置 | 推迟到后续阶段 |

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
- `dotnet test -c Release --no-build` 160/160 PASS
- `git diff --check` clean

---

## Git 提交信息

```
feat: create MainWindow shell with WebView2 control

SCAFFOLD: MainWindow is borderless (WindowStyle=None), topmost, with
WebView2 control filling the client area. WS_EX_TOOLWINDOW applied at
SourceInitialized. App.xaml StartupUri removed; OnStartup loads settings,
normalizes bounds via WindowBoundsNormalizer, and shows the window.
No WebView2 navigation or event wiring — deferred to TASK-07.
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
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
AllowsTransparency: NOT set (default false — WebView2 compatibility)
WS_EX_TOOLWINDOW: Applied at SourceInitialized, with SetWindowPos refresh
WebView2 Control: Named "webView", no Source, no events, no navigation
Settings: Loaded, bounds restored, fallback to defaults on failure
```
