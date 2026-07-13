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
- `src/Anhei4Map.App/RendererWindow.xaml.cs`（添加 `CaptureAndCropMapAsync` 方法）
- `src/Anhei4Map.App/App.xaml.cs`（创建 OverlayWindow + 单次手动截图调用）

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.Infrastructure/**`
- `RendererWindow.xaml`
- `MainWindow.xaml` / `MainWindow.xaml.cs`
- `tests/**`
- `docs/design/**`
- `*.csproj`
- 不实现定时器（TASK-03）
- 不实现鼠标穿透、热键、人物同步

---

## 步 1：OverlayWindow.xaml

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
        Opacity="0.7"
        Background="Transparent">
    <Image x:Name="MapImage"
           Stretch="UniformToFill" />
</Window>
```

**冻结属性表：**

| 属性 | 值 | 原因 |
|------|-----|------|
| WindowStyle | None | 无边框叠加层 |
| ResizeMode | NoResize | 固定尺寸 |
| ShowInTaskbar | False | 不显示在任务栏 |
| Topmost | True | 始终置顶 |
| Width | 300 | 默认宽度 |
| Height | 300 | 默认高度 |
| Opacity | 0.7 | 默认不透明度 |
| Background | Transparent | 透明背景 |
| Image Name | MapImage | 供代码引用 |
| Stretch | UniformToFill | 裁剪图片填满控件 |

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

- `croppedBitmap` 在传入前必须已经 `Freeze()`（RendererWindow 中处理）
- `BitmapSource` 可由 UI 线程安全访问

---

## 步 3：CaptureAndCropMapAsync 方法

在 `RendererWindow.xaml.cs` 中添加：

```csharp
public async Task<BitmapSource?> CaptureAndCropMapAsync()
{
    var region = await TryGetMapRegionAsync();
    if (region == null) return null;

    var stream = new MemoryStream();
    try
    {
        await webView.CoreWebView2.CapturePreviewAsync(
            CoreWebView2CapturePreviewImageFormat.Png, stream);
        stream.Position = 0;

        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var bitmapWidth = frame.PixelWidth;
        var bitmapHeight = frame.PixelHeight;

        // 缩放比例
        var scaleX = bitmapWidth / webView.ActualWidth;
        var scaleY = bitmapHeight / webView.ActualHeight;

        // 裁剪坐标
        var cropX = (int)Math.Floor(region.Left * scaleX);
        var cropY = (int)Math.Floor(region.Top * scaleY);
        var cropRight = (int)Math.Ceiling((region.Left + region.Width) * scaleX);
        var cropBottom = (int)Math.Ceiling((region.Top + region.Height) * scaleY);

        // 边界保护
        cropX = Math.Max(0, cropX);
        cropY = Math.Max(0, cropY);
        cropRight = Math.Min(bitmapWidth, cropRight);
        cropBottom = Math.Min(bitmapHeight, cropBottom);

        var cropWidth = cropRight - cropX;
        var cropHeight = cropBottom - cropY;

        if (cropWidth <= 0 || cropHeight <= 0) return null;

        var cropped = new CroppedBitmap(frame, new Int32Rect(cropX, cropY, cropWidth, cropHeight));
        cropped.Freeze();
        return cropped;
    }
    catch
    {
        return null;
    }
    finally
    {
        stream.Dispose();
    }
}
```

**截图规则（冻结）：**
- 格式：PNG（`CoreWebView2CapturePreviewImageFormat.Png`）
- MemoryStream 使用后释放（finally 块）
- 裁剪按 floor/ceiling 确保完整覆盖
- 无效裁剪区域（≤0）→ 返回 null
- `CroppedBitmap.Freeze()` 后返回（跨线程安全）
- 任何异常 → 返回 null

---

## 步 4：App.xaml.cs 修改

在 `OnStartup` 中 RendererWindow 之后添加：

```csharp
_overlayWindow = new OverlayWindow();
_overlayWindow.Left = 20;
_overlayWindow.Top = 20;
_overlayWindow.Show();

// 单次截图（用于验收），异步 fire-and-forget
_ = Task.Run(async () =>
{
    await Task.Delay(5000); // 等待 WebView2 初始化和导航完成
    var bitmap = await _rendererWindow!.CaptureAndCropMapAsync();
    if (bitmap != null)
    {
        Dispatcher.Invoke(() => _overlayWindow.UpdateMapImage(bitmap));
    }
});
```

**规则：**
- Task.Delay(5000) 给 WebView2 足够的初始化和首次导航时间
- Dispatcher.Invoke 确保 UI 线程更新 Image
- 本任务只支持单次截图，不实现循环刷新
- OverlayWindow 实例保存为 `App` 字段

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
feat: implement screenshot capture, crop, and overlay window

STAGE-03-TASK-02: CapturePreviewAsync PNG screenshot → crop by DOM
coordinates scaled to bitmap dimensions → display on OverlayWindow
at (20,20) 300x300 opacity 0.7. Single manual capture triggered 5s
after startup. CroppedBitmap frozen for thread safety.
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
Capture Format: PNG
Crop Scaling: bitmapPixels / webView.ActualSize
Overlay: (20,20), 300x300, Opacity 0.7, Topmost=True
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
```
