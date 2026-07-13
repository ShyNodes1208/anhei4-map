# Stage 03: Cropped Map Overlay

## Stage Info

- Stage: STAGE-03
- Name: Cropped HellTides Map Overlay
- Slug: cropped-map-overlay
- Branch: feature/03-cropped-map-overlay
- Base: 2bada05884a21f8fbe8179171daadfd178539349 (v0.2.0-webview-shell-preview)

## Goal

HellTides.com 在后台 WebView2 中静默加载。通过 DOM 查询定位地图容器，使用 CapturePreviewAsync 截取页面并裁剪为仅地图区域。前台 WPF Image 控件显示裁剪后的地图图片，每 3 秒刷新。窗口固定在屏幕左上角 (20,20)，默认 300×300，不透明度 0.7。

## User Value

用户看到的是干净的地图图片叠加层（非完整网页），固定在屏幕左上角，自动刷新。不遮挡游戏右上角原生小地图。

## Technical Approach

### Architecture

```
MainWindow (hidden, full page)          OverlayWindow (visible, Image only)
  │ WebView2 (renders helltides.com)       │ Image control (cropped bitmap)
  │ CapturePreviewAsync()                   │
  │ ExecuteScriptAsync() → DOM coords      │
  └──────────┬─────────────────────────────┘
             │ MapCaptureService
             │ 1. Query DOM rect
             │ 2. CapturePreviewAsync
             │ 3. Crop bitmap
             │ 4. Update overlay Image
```

### DOM Query Strategy

HellTides.com 地图位于 `#map` 或 `canvas` 容器内。使用 `ExecuteScriptAsync` 查询：

```javascript
(function() {
  const map = document.querySelector('#map') || document.querySelector('canvas.leaflet-container');
  if (!map) return null;
  const rect = map.getBoundingClientRect();
  return JSON.stringify({
    left: rect.left,
    top: rect.top,
    width: rect.width,
    height: rect.height,
    devicePixelRatio: window.devicePixelRatio || 1
  });
})()
```

### Screenshot & Crop

`CapturePreviewAsync` 截取整个 WebView2 可见区域 → `BitmapSource` → 按 DOM 坐标裁剪。

DPI 换算：`actualPixelX = cssX * devicePixelRatio`。CapturePreviewAsync 返回物理像素，需与 CSS 像素坐标对齐。

### Refresh Timer

`System.Timers.Timer` 每 3 秒触发。使用 `SemaphoreSlim(1,1)` 防并发（上一次截图未完成则跳过本次）。

### Default Positioning

Overlay 窗口固定在屏幕左上角：Left=20, Top=20, Width=300, Height=300, Opacity=0.7。

## Scope

- 后台 WebView2 渲染（隐藏 MainWindow）
- DOM 查询地图容器坐标
- CapturePreviewAsync 截图
- 裁剪为地图区域
- OverlayWindow 显示裁剪图片
- 每 3 秒刷新
- 默认位置/尺寸/透明度

## Non-Goals

- 鼠标穿透 (WS_EX_TRANSPARENT) — Stage 04
- 全局热键 — Stage 04
- 游戏人物位置同步 — Stage 05
- EditOverlay 控制面板 — Stage 04
- 读取游戏内存/进程注入 — 安全红线

## Risks

| Risk | Mitigation |
|------|-----------|
| DOM 结构变化 | 多选择器回退；DOM 查询失败→跳过刷新 |
| 截图性能 | 使用小 viewport；CapturePreviewAsync 异步 |
| 坐标漂移 | 每次刷新重新查询 DOM |
| WebView2 未加载完成 | 等待 NavigationCompleted 后再启动定时器 |

## Test Strategy

- 单元测试：MapCaptureService 裁剪逻辑、坐标换算（纯逻辑部分）
- SCAFFOLD：WPF Overlay 窗口
- 手动验收：启动应用确认地图截图显示在左上角

## Acceptance

- 应用启动后 Overlay 窗口位于屏幕左上角 (20, 20)
- 显示裁剪后地图图片（非完整网页）
- 每 3 秒刷新
- MainWindow 不可见
- Release build 0 errors 0 warnings
- 所有单元测试通过
