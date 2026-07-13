# 当前任务

## 阶段
STAGE-03-CROPPED-MAP-OVERLAY

## 任务编号
STAGE-03-ACCEPTANCE-FIX-03

## 类型
FIX

## 状态
READY

## 组件
移除正方形裁剪 + 按比例缩放到 400×250 Overlay

## 父任务
ACCEPTANCE-FIX-01 (875f659) 被撤销; ACCEPTANCE-FIX-02 (fe3d7f4) 保留

## 下一执行者
Cursor

---

## 问题

居中正方形裁剪只保留了地图中央，丢失了地图边缘区域。用户要求完整显示原始地图，保持比例缩放到紧凑的左上角窗口。

## 修复策略

1. RendererWindow: 删除第二次居中正方形裁剪，返回完整 DOM 地图区域
2. OverlayWindow: 按比例缩放，内部最大 400×250

---

## 允许修改的精确路径

- `src/Anhei4Map.App/RendererWindow.xaml.cs`
- `src/Anhei4Map.App/OverlayWindow.xaml`
- `src/Anhei4Map.App/OverlayWindow.xaml.cs`

## 禁止修改范围

- `App.xaml.cs`
- `RendererWindow.xaml`
- `src/Anhei4Map.Core/**`、`src/Anhei4Map.Infrastructure/**`
- `tests/**`、`docs/design/**`、`*.csproj`
- 不实现定时刷新、鼠标穿透、热键、人物同步

---

## 步 1：RendererWindow — 删除正方形裁剪

在 `CaptureAndCropMapCoreAsync` 中，删除 ACCEPTANCE-FIX-01 引入的居中正方形裁剪代码块。DOM 地图区域裁剪完成后直接 `Freeze()` 并返回。

```csharp
// 删除以下代码块:
// 居中正方形裁剪
// {
//     var sourceWidth = cropped.PixelWidth;
//     ...
//     return squareBitmap;
// }

// 替换为:
cropped.Freeze();
return cropped;
```

完整保留：地图视觉就绪检查、DOM 查询、CapturePreviewAsync、scale 换算、边界保护、OnLoad。

---

## 步 2：OverlayWindow.xaml

```xml
<Window x:Class="Anhei4Map.App.OverlayWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        WindowStyle="None"
        ResizeMode="NoResize"
        ShowInTaskbar="False"
        Topmost="True"
        Width="400"
        Height="250"
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

不变属性：所有 Stage 03 已冻结的 Overlay 属性。初始 400×250 为最大尺寸，实际由截图比例缩小。

---

## 步 3：OverlayWindow.xaml.cs — 动态尺寸

```csharp
public partial class OverlayWindow : Window
{
    private const double MaxOverlayWidth = 400;
    private const double MaxOverlayHeight = 250;

    public void UpdateMapImage(BitmapSource bitmap)
    {
        if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0) return;

        var scale = Math.Min(
            MaxOverlayWidth / bitmap.PixelWidth,
            MaxOverlayHeight / bitmap.PixelHeight);

        if (!double.IsFinite(scale) || scale <= 0) return;

        Width = bitmap.PixelWidth * scale;
        Height = bitmap.PixelHeight * scale;

        MapImage.Source = bitmap;
    }
}
```

**冻结公式：**
```
scale = Min(400 / bitmap.PixelWidth, 250 / bitmap.PixelHeight)
displayWidth  = bitmap.PixelWidth  × scale  (≤ 400)
displayHeight = bitmap.PixelHeight × scale  (≤ 250)
```

**规则：**
- scale 为正有限数
- Width ≤ 400, Height ≤ 250
- Stretch = Uniform（不变形）
- 不使用 Fill、UniformToFill、ScaleTransform
- 无二次裁剪
- 保持 Opacity=0.7

---

## 步 4：预期效果

- 完整显示 HellTides 地图区域（地形 + 标记）
- 保持原始宽高比
- 最大 400×250
- 左上角 (20, 20)
- 不覆盖游戏右上角小地图

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
fix: remove square crop, display full map proportionally in 400x250 overlay

Remove the centered square crop introduced in ACCEPTANCE-FIX-01. The
full DOM map region is now returned by CaptureAndCropMapAsync and scaled
proportionally (max 400x250) in the OverlayWindow via Uniform stretch.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-03-ACCEPTANCE-FIX-03
Previous: ACCEPTANCE-FIX-01 (reverted), ACCEPTANCE-FIX-02 (kept)
Type: FIX
Status: DONE
Commit: <hash>
Files Modified:
  - src/Anhei4Map.App/RendererWindow.xaml.cs
  - src/Anhei4Map.App/OverlayWindow.xaml
  - src/Anhei4Map.App/OverlayWindow.xaml.cs
Capture: Full DOM map crop, no secondary square crop
Overlay: Proportional max 400x250, Uniform
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
```
