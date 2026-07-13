# 当前任务

## 阶段
STAGE-03-CROPPED-MAP-OVERLAY

## 任务编号
STAGE-03-ACCEPTANCE-FIX-01

## 类型
FIX

## 状态
READY

## 组件
居中正方形裁剪——解决宽屏地图 letterbox 问题

## 父任务
STAGE-03-TASK-02 (481c30b — 未 push)

## 下一执行者
Cursor

---

## 问题

HellTides 地图是宽屏比例（~3000×700）。DOM 裁剪后得到宽矩形，用 `Stretch=Uniform` 缩放到 300×300 正方形窗口后产生上下透明留白，实际可见地图区域仅为约 300×60 横条。用户看到的是非可读的细条。

## 修复策略

在 `CaptureAndCropMapCoreAsync` 中，DOM 地图区域裁剪**之后**、`Freeze()` **之前**，追加一次居中正方形裁剪。

---

## 允许修改的精确路径

- `src/Anhei4Map.App/RendererWindow.xaml.cs`

## 禁止修改范围

- OverlayWindow.xaml / OverlayWindow.xaml.cs
- App.xaml.cs
- RendererWindow.xaml
- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.Infrastructure/**`
- `tests/**`
- `docs/design/**`
- `*.csproj`
- 不修改 Stretch=Uniform
- 不实现定时器、热键、穿透、人物同步

---

## 精确修改

在 `CaptureAndCropMapCoreAsync` 中，DOM 裁剪成功后（`var cropped = new CroppedBitmap(...)` 之后、`cropped.Freeze()` 之前），插入：

```csharp
// 居中正方形裁剪
{
    var sourceWidth = cropped.PixelWidth;
    var sourceHeight = cropped.PixelHeight;
    var side = Math.Min(sourceWidth, sourceHeight);

    if (side <= 0) return null;

    var squareX = (sourceWidth - side) / 2;
    var squareY = (sourceHeight - side) / 2;

    var squareBitmap = new CroppedBitmap(cropped,
        new Int32Rect(squareX, squareY, side, side));

    squareBitmap.Freeze();
    return squareBitmap;
}
```

原 `cropped.Freeze(); return cropped;` 改为上述居中正方形裁剪。

---

## 正方形裁剪公式（冻结）

| 变量 | 公式 |
|------|------|
| sourceWidth | cropped.PixelWidth |
| sourceHeight | cropped.PixelHeight |
| side | Min(sourceWidth, sourceHeight) |
| squareX | (sourceWidth - side) / 2（整数除法） |
| squareY | (sourceHeight - side) / 2（整数除法） |
| 最终尺寸 | side × side |

---

## 规则

- 最终 CroppedBitmap 调用 `Freeze()`
- `side <= 0` → 返回 null
- 不修改 Stretch 属性（保持 Uniform）
- 不使用 UniformToFill 或 Fill（避免变形或隐式裁剪）
- 居中裁剪同时排除地图边缘的网页控制按钮
- DOM 裁剪 + 正方形裁剪都使用 `CroppedBitmap`（源为 `BitmapFrame`，轻量引用，不复制像素数据直到渲染）

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
- 手动：Overlay 显示居中正方形地图，无 letterbox 留白

---

## Git 提交信息

```
fix: center-crop map to square after DOM region extraction

After DOM-based map region cropping produces a wide rectangle, a second
centered square crop is applied to fill the 300×300 overlay window
without letterboxing. Uses CroppedBitmap for lightweight reference-based
cropping.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-03-ACCEPTANCE-FIX-01
Parent: STAGE-03-TASK-02 (481c30b)
Type: FIX
Status: DONE
Commit: <hash>
File Modified: src/Anhei4Map.App/RendererWindow.xaml.cs
Square Crop: side = Min(PixelWidth, PixelHeight), centered
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
```
