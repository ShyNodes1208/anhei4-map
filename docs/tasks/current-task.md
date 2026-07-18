# 当前任务

## 阶段
STAGE-06

## 任务编号
STAGE-06-TASK-04-TASKBAR-CONTROLS

## 类型
BEHAVIOR

## 状态
READY

## 组件
新增 ControlWindow（任务栏控制窗口），提取共享刷新方法，实现手动刷新和退出功能

## 下一执行者
Cursor

---

## Allowed Paths
- src/Anhei4Map.App/App.xaml.cs
- src/Anhei4Map.App/ControlWindow.xaml（新增）
- src/Anhei4Map.App/ControlWindow.xaml.cs（新增）

## Forbidden
App.xaml, OverlayWindow.xaml, OverlayWindow.xaml.cs, RendererWindow.xaml, RendererWindow.xaml.cs, MainWindow.xaml, MainWindow.xaml.cs, Win32Native.cs, Win32Interop.cs, MapViewportDiagnostics.cs, 其他 src/**, tests/**, *.csproj, artifacts/**. New Dependencies: NONE.

---

## 实现

### 一、新增 ControlWindow.xaml

```xml
<Window x:Class="Anhei4Map.App.ControlWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Anhei4Map"
        WindowStyle="SingleBorderWindow"
        ResizeMode="CanMinimize"
        ShowInTaskbar="True"
        SizeToContent="WidthAndHeight"
        WindowStartupLocation="CenterScreen">
    <StackPanel Margin="12">
        <Button x:Name="RefreshButton"
                Content="刷新"
                Padding="24,8"
                Margin="0,0,0,8"
                Click="OnRefreshClick"/>
        <Button x:Name="ExitButton"
                Content="退出"
                Padding="24,8"
                Click="OnExitClick"/>
    </StackPanel>
</Window>
```

要求：
- 不设置 Topmost
- 不添加其他控件
- 不添加状态栏、设置项、日志区

### 二、新增 ControlWindow.xaml.cs

```csharp
using System.ComponentModel;
using System.Windows;

namespace Anhei4Map.App;

public partial class ControlWindow : Window
{
    private bool _isShuttingDown;

    public event EventHandler? RefreshRequested;

    public ControlWindow()
    {
        InitializeComponent();
    }

    public void SetRefreshEnabled(bool enabled)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetRefreshEnabled(enabled));
            return;
        }

        RefreshButton.IsEnabled = enabled;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        ShutdownApp();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isShuttingDown)
        {
            // 用户点击右上角 X → 触发完整退出
            ShutdownApp();
        }
        // Shutdown 期间 _isShuttingDown=true，允许窗口正常关闭

        base.OnClosing(e);
    }

    private void ShutdownApp()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;
        Application.Current.Shutdown();
    }
}
```

要求：
- 不添加其他事件处理
- 不添加其他方法
- 不引用 OverlayWindow 或 RendererWindow

### 三、修改 App.xaml.cs

#### 3.1 新增字段

```csharp
private ControlWindow? _controlWindow;
```

放在 `private int _initialCaptureStarted;` 之后。

#### 3.2 OnStartup 中创建 ControlWindow

在 `_rendererWindow = new RendererWindow(...)` 之前插入：

```csharp
ShutdownMode = ShutdownMode.OnExplicitShutdown;

_controlWindow = new ControlWindow();
_controlWindow.ShowActivated = false;
_controlWindow.WindowState = WindowState.Minimized;
_controlWindow.RefreshRequested += OnManualRefreshRequested;
_controlWindow.Show();
```

要求：
- `ShowActivated = false` 和 `WindowState = WindowState.Minimized` 必须在 `Show()` 之前设置
- 不设置 Topmost
- 不设置为 OverlayWindow 的 Owner

#### 3.3 提取 RefreshMapOnceAsync 方法

在 `OnHourlyRefreshTick` 之后新增 `RefreshMapOnceAsync` 方法：

```csharp
private async Task<bool> RefreshMapOnceAsync()
{
    if (_rendererWindow == null || _overlayWindow == null)
    {
        return false;
    }

    var reloadOk = await _rendererWindow.ReloadPageAsync();
    if (!reloadOk)
    {
        return false;
    }

    const int maxAttempts = 10;
    const int warmupDelayMs = 10000;
    const int retryDelayMs = 1000;

    await Task.Delay(warmupDelayMs);

    System.Windows.Media.Imaging.BitmapSource? bitmap = null;

    for (var attempt = 0; attempt < maxAttempts; attempt++)
    {
        bitmap = await _rendererWindow.CaptureAndCropMapAsync();

        if (bitmap is not null)
        {
            break;
        }

        if (attempt < maxAttempts - 1)
        {
            await Task.Delay(retryDelayMs);
        }
    }

    if (bitmap is not null)
    {
        _overlayWindow.UpdateMapImage(bitmap);
        return true;
    }

    return false;
}
```

#### 3.4 重构 OnHourlyRefreshTick

将 `OnHourlyRefreshTick` 中的 Reload + Capture 逻辑替换为对 `RefreshMapOnceAsync` 的调用：

```csharp
private async void OnHourlyRefreshTick(object? sender, EventArgs e)
{
    _hourlyRefreshTimer?.Stop();

    if (_refreshInProgress)
    {
        ScheduleNextHourlyRefresh();
        return;
    }

    _refreshInProgress = true;
    try
    {
        _ = await RefreshMapOnceAsync();
    }
    catch (Exception)
    {
    }
    finally
    {
        _refreshInProgress = false;
        ScheduleNextHourlyRefresh();
    }
}
```

原方法中的以下逻辑移至 `RefreshMapOnceAsync`：
- `_rendererWindow == null || _overlayWindow == null` 检查
- `ReloadPageAsync` 调用
- 10 秒预热
- 10 次 CaptureAndCropMapAsync 重试
- UpdateMapImage 调用

#### 3.5 新增手动刷新处理

在 `OnHourlyRefreshTick` 之后新增：

```csharp
private async void OnManualRefreshRequested(object? sender, EventArgs e)
{
    if (_refreshInProgress || _controlWindow == null)
    {
        return;
    }

    _refreshInProgress = true;
    _controlWindow.SetRefreshEnabled(false);

    try
    {
        _ = await RefreshMapOnceAsync();
    }
    catch (Exception)
    {
    }
    finally
    {
        _refreshInProgress = false;
        _controlWindow.SetRefreshEnabled(true);
    }
}
```

#### 3.6 更新 OnExit

在 `OnExit` 中，timer 清理之后、mutex 释放之前，增加窗口关闭：

```csharp
protected override void OnExit(ExitEventArgs e)
{
    if (_hourlyRefreshTimer != null)
    {
        _hourlyRefreshTimer.Stop();
        _hourlyRefreshTimer.Tick -= OnHourlyRefreshTick;
        _hourlyRefreshTimer = null;
    }

    // 关闭所有窗口，确保进程完全退出
    _controlWindow?.Close();
    _overlayWindow?.Close();
    _rendererWindow?.Close();

    try
    {
        _mutex?.ReleaseMutex();
    }
    catch
    {
        // 忽略——进程退出前释放尽力而为
    }

    _mutex?.Dispose();
    _mutex = null;

    base.OnExit(e);
}
```

### 四、保持现有功能不变

以下行为不得改变：
- 首次地图自动显示（`StartInitialCaptureAsync` 逻辑不变）
- HH:01 自动刷新（`ScheduleNextHourlyRefresh` 逻辑不变）
- 10 秒预热 + 10 次截图重试（移至 `RefreshMapOnceAsync` 后逻辑不变）
- 地图裁剪和尺寸规则（`RendererWindow.CaptureAndCropMapAsync` 不变）
- Overlay Topmost、WS_EX_TRANSPARENT、WS_EX_NOACTIVATE（OverlayWindow 不变）
- 左键、右键、滚轮鼠标穿透（OverlayWindow 不变）
- 新图成功前保留旧图（`UpdateMapImage` 只在 bitmap != null 时调用）

### 五、不得增加

- 托盘图标、热键、设置窗口、刷新间隔配置
- 退出确认弹窗
- 新 NuGet 依赖
- Service、Manager、状态机
- artifacts 发布文件
- 任何对 OverlayWindow / RendererWindow 代码的修改

## 验证

```powershell
dotnet build .\anhei-map.sln -c Release
dotnet test .\anhei-map.sln -c Release --no-build
git diff --check
```

验证：

1. Build 通过（0 errors, 0 warnings）
2. Tests 全通过（预期 160/160）
3. `git diff --check` 无空白警告
4. 不 push

## 提交

```
feat: add taskbar control window with manual refresh and exit
```

## Overengineering Check
PASS — 仅新增 ControlWindow（两个小文件），提取共享刷新方法，增加手动刷新入口，不涉及任何架构变更
