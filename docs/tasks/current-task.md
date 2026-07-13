# 当前任务

## 阶段
STAGE-03-CROPPED-MAP-OVERLAY

## 任务编号
STAGE-03-TASK-02

## 类型
IMPLEMENTATION

## 状态
READY

## 组件
CapturePreview crop + OverlayWindow + top-left defaults

## 下一执行者
Cursor

---

## 允许修改的精确路径

- `src/Anhei4Map.App/OverlayWindow.xaml`（新建）
- `src/Anhei4Map.App/OverlayWindow.xaml.cs`（新建）
- `src/Anhei4Map.App/RendererWindow.xaml.cs`（添加事件 + CaptureAndCropMapAsync）
- `src/Anhei4Map.App/App.xaml.cs`（订阅事件 + 有限重试 + Overlay 管理）

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.Infrastructure/**`
- `RendererWindow.xaml`
- `MainWindow.xaml` / `MainWindow.xaml.cs`
- `tests/**`
- `docs/design/**`
- `*.csproj`
- 不实现 DispatcherTimer
- 不实现 3 秒周期刷新（TASK-03）
- 不实现鼠标穿透、热键、人物同步

---

## 步 1：OverlayWindow.xaml（修正）

```xml
<Window x:Class="Anhei4Map.App.OverlayWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        WindowStyle="None"
        ResizeMode="NoResize"
        ShowInTaskbar="False"
        Topmost="True"
        Width="300"
        Height="300"
        Left="20"
        Top="20"
        WindowStartupLocation="Manual"
        AllowsTransparency="True"
        Background="Transparent"
        Opacity="0.7">
    <Image x:Name="MapImage"
           Stretch="Uniform" />
</Window>
```

**冻结属性：**

| 属性 | 值 |
|------|-----|
| Left | 20 |
| Top | 20 |
| Width | 300 |
| Height | 300 |
| WindowStartupLocation | Manual |
| Topmost | True |
| ShowInTaskbar | False |
| WindowStyle | None |
| ResizeMode | NoResize |
| AllowsTransparency | True |
| Background | Transparent |
| Opacity | 0.7 |
| Image Name | MapImage |
| Stretch | **Uniform**（不裁剪地图边缘） |

---

## 步 2：OverlayWindow.xaml.cs

```csharp
public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void UpdateMapImage(BitmapSource croppedBitmap)
    {
        MapImage.Source = croppedBitmap;
    }
}
```

- 传入的 `croppedBitmap` 在调用前已 `Freeze()`

---

## 步 3：RendererWindow — NavigationReady 事件

```csharp
public event EventHandler? NavigationReady;
```

- 在 `OnNavigationCompleted` 中 `e.IsSuccess == true` 且 `!_isClosed` 时触发：
  ```csharp
  NavigationReady?.Invoke(this, EventArgs.Empty);
  ```
- 仅触发一次——触发后立即解除事件订阅（或在第一次触发后设置标志阻止再次触发）

---

## 步 4：CaptureAndCropMapAsync（冻结）

```csharp
public async Task<BitmapSource?> CaptureAndCropMapAsync()
```

**调用规则：**
- 必须在 WebView2 所属 Dispatcher/UI 线程调用
- 内部调用 `TryGetMapRegionAsync()` 获取 DOM 区域
- 未就绪或无地图区域 → 返回 `null`

**截图流程（冻结）：**
1. `TryGetMapRegionAsync()` → null → 返回 null
2. `var stream = new MemoryStream()`
3. `await webView.CoreWebView2.CapturePreviewAsync(Png, stream)`
4. `stream.Position = 0`
5. `BitmapDecoder.Create(stream, PreservePixelFormat, OnLoad)`
6. 获取 `decoder.Frames[0]`
7. `scaleX = frame.PixelWidth / webView.ActualWidth`
8. `scaleY = frame.PixelHeight / webView.ActualHeight`
9. 裁剪坐标 floor/ceiling 换算
10. 边界保护 → 无效返回 null
11. `new CroppedBitmap(frame, new Int32Rect(...))`
12. `cropped.Freeze()`
13. finally `stream.Dispose()`

**Bitmap 解码规则（冻结）：**
- `BitmapCreateOptions.PreservePixelFormat`
- `BitmapCacheOption.OnLoad`——流释放后图片仍可用
- `decoder.Frames` 至少有一帧；否则返回 null
- CroppedBitmap 创建后 `Freeze()`
- MemoryStream 在 finally 中释放

**裁剪公式（冻结）：**
```
scaleX = frame.PixelWidth / webView.ActualWidth
scaleY = frame.PixelHeight / webView.ActualHeight

cropX = Floor(region.Left * scaleX)
cropY = Floor(region.Top * scaleY)
cropRight = Ceiling((region.Left + region.Width) * scaleX)
cropBottom = Ceiling((region.Top + region.Height) * scaleY)

clamp(cropX, 0, frame.PixelWidth)
clamp(cropY, 0, frame.PixelHeight)
clamp(cropRight, 0, frame.PixelWidth)
clamp(cropBottom, 0, frame.PixelHeight)
```

**返回 null 的条件：**
- TryGetMapRegionAsync 返回 null
- webView.ActualWidth <= 0 或 ActualHeight <= 0
- CapturePreviewAsync 异常
- decoder.Frames 为空
- 裁剪宽度 <= 0 或高度 <= 0
- 任何异常

**所有失败返回 null，不向调用方抛异常。**

---

## 步 5：App.xaml.cs — 事件驱动初始截图

### 字段

```csharp
private OverlayWindow? _overlayWindow;
private int _initialCaptureStarted;
```

### OnStartup 中

```csharp
_rendererWindow = new RendererWindow();
_rendererWindow.NavigationReady += OnRendererNavigationReady;
_rendererWindow.Show();

_overlayWindow = new OverlayWindow();
// 不调用 _overlayWindow.Show() —— 等首次截图成功后

base.OnStartup(e);
```

### 事件处理

```csharp
private void OnRendererNavigationReady(object? sender, EventArgs e)
{
    if (Interlocked.Exchange(ref _initialCaptureStarted, 1) != 0) return;

    _ = StartInitialCaptureAsync();
}

private async Task StartInitialCaptureAsync()
{
    const int maxAttempts = 10;
    const int delayMs = 500;

    for (var attempt = 0; attempt < maxAttempts; attempt++)
    {
        if (attempt > 0) await Task.Delay(delayMs);

        BitmapSource? bitmap;
        Dispatcher.Invoke(() => bitmap = _rendererWindow!.CaptureAndCropMapAsync().GetAwaiter().GetResult());
        // CaptureAndCropMapAsync 必须在 UI 线程

        // 更简单的写法：直接在 UI 线程执行
        // var bitmap = await _rendererWindow!.CaptureAndCropMapAsync();

        if (bitmap != null)
        {
            Dispatcher.Invoke(() =>
            {
                _overlayWindow!.UpdateMapImage(bitmap);
                if (!_overlayWindow.IsVisible)
                {
                    _overlayWindow.Show();
                }
            });
            return;
        }
    }
    // 10 次全部失败 → 静默停止，不弹窗，不退出
}
```

**注意：** `StartInitialCaptureAsync` 中的 `await` 不要与 `Dispatcher.Invoke` 冲突。如果 RendererWindow 是独立窗体，CaptureAndCropMapAsync 必须在 RendererWindow 的 Dispatcher 上执行。使用 `_rendererWindow.Dispatcher.InvokeAsync(...)` 或直接在 RendererWindow 的 Dispatcher 线程调用。

**简化方案（推荐）：** `CaptureAndCropMapAsync` 内部使用 `Application.Current.Dispatcher.InvokeAsync` 确保在 UI 线程执行，或让 TASK-02 约定调用方在 UI 线程调用。

---

## 步 6：初始截图重试规则（冻结）

| 事项 | 规则 |
|------|------|
| 触发源 | NavigationReady 事件 |
| 防重复 | Interlocked.Exchange(_initialCaptureStarted, 1) |
| 最多尝试 | 10 次 |
| 间隔 | 500ms |
| 成功条件 | CaptureAndCropMapAsync 返回非 null |
| 首次成功 | Overlay.UpdateMapImage + Show（如果尚未可见） |
| 全部失败 | 静默停止，不弹窗，不退出程序 |
| 不重复 | 成功后不再尝试 |
| 不循环 | 10 次后结束，不实现永久或周期刷新 |

---

## 验证命令

```powershell
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
feat: implement event-driven screenshot capture with crop overlay

STAGE-03-TASK-02: NavigationReady event drives initial capture with
10 attempts at 500ms intervals. CaptureAndCropMapAsync uses PNG format,
OnLoad cache, and Freeze for thread safety. OverlayWindow shown only
after first successful crop. No periodic timer — deferred to TASK-03.
```

---

## Cursor 最终报告格式

```
TASK_COMPLETE

Task: STAGE-03-TASK-02
Component: CapturePreview crop + OverlayWindow + top-left defaults
Type: IMPLEMENTATION
Status: DONE
Commit: <hash>
Files Created:
  - src/Anhei4Map.App/OverlayWindow.xaml
  - src/Anhei4Map.App/OverlayWindow.xaml.cs
Files Modified:
  - src/Anhei4Map.App/RendererWindow.xaml.cs
  - src/Anhei4Map.App/App.xaml.cs
Capture API: CaptureAndCropMapAsync() → BitmapSource?
Trigger: NavigationReady event → Interlocked guard → 10×500ms retry
Bitmap Decode: PreservePixelFormat + OnLoad, Freeze
Overlay: (20,20) 300×300 Opacity=0.7, shown on first success
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
```
