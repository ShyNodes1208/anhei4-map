# Stage 03: Cropped Map Overlay

## Stage Info

- Stage: STAGE-03
- Name: Cropped HellTides Map Overlay
- Slug: cropped-map-overlay
- Branch: feature/03-cropped-map-overlay
- Base: 2bada05884a21f8fbe8179171daadfd178539349

## Goal

HellTides.com 在屏幕外 RendererWindow 中静默渲染。DOM 查询定位地图容器 → CapturePreviewAsync 截图 → 按实际渲染比例裁剪 → 前台 OverlayWindow 显示纯地图图片。每 3 秒 DispatcherTimer 刷新。

## Architecture

```
RendererWindow (offscreen -10000,-10000)    OverlayWindow (visible, Image only)
  │ WebView2 (renders helltides.com)          │ Image control (cropped bitmap)
  │ CapturePreviewAsync()                      │
  │ ExecuteScriptAsync() → DOM coords         │
  └──────────┬────────────────────────────────┘
             │ MapCaptureService
             │ 1. Query DOM rect + best candidate
             │ 2. CapturePreviewAsync
             │ 3. Scale: bitmapPixels / webView.ActualSize
             │ 4. Crop bitmap by scaled DOM rect
             │ 5. Update overlay Image
```

## Renderer Strategy

- 创建独立 `RendererWindow`（非原 MainWindow）
- `ShowInTaskbar="False"`、非零尺寸（如 1280×720）
- 移动到屏幕外：`Left=-10000, Top=-10000`
- 窗口保持 `Show()` 状态——WebView2 需要窗口可见才能正常渲染
- RendererWindow 加载 WebView2 并导航 helltides.com（复用 Stage 02 的初始化逻辑）
- 原 MainWindow 弃用或改为 RendererWindow

## DOM Detection Strategy

多选择器候选，选择面积最大的可见元素：

```javascript
(function() {
  const selectors = ['#map', '.leaflet-container', '[class*="map"]'];
  let best = null, bestArea = 0;
  for (const sel of selectors) {
    const el = document.querySelector(sel);
    if (!el) continue;
    const r = el.getBoundingClientRect();
    const area = r.width * r.height;
    if (area > 0 && area > bestArea) { best = el; bestArea = area; }
  }
  if (!best) return null;
  const r = best.getBoundingClientRect();
  return JSON.stringify({ left: r.left, top: r.top, width: r.width, height: r.height, dpr: window.devicePixelRatio || 1 });
})()
```

**降级规则：**
- 没有候选元素 → 返回 null → 跳过本轮刷新，不关闭程序
- width/height 必须 > 0
- 选择器集中在方法内定义，方便网站改版后调整

## Crop Scaling Strategy

不使用 devicePixelRatio 作为唯一缩放依据。根据截图实际像素与 WebView2 控件尺寸计算比例：

```
scaleX = bitmapPixelWidth / webView.ActualWidth
scaleY = bitmapPixelHeight / webView.ActualHeight

cropLeft   = (int)(domLeft   * scaleX)
cropTop    = (int)(domTop    * scaleY)
cropWidth  = (int)(domWidth  * scaleX)
cropHeight = (int)(domHeight * scaleY)
```

- devicePixelRatio 仅作为诊断日志，不参与坐标换算
- `ActualWidth/ActualHeight` 来自 WPF 布局（非 CSS 像素）
- 边界保护：crop 坐标不得超出截图范围

## Refresh Strategy

- `DispatcherTimer`（UI 线程）每 3 秒触发
- `SemaphoreSlim(1,1)` 防重入——上一次截图未完成则跳过本次
- 截图在 UI 线程异步执行（`async void` + `await`）
- `NavigationCompleted` 之后才开始定时器
- 定时器 tick 时检查 `webView.CoreWebView2 != null`

## Default Positioning

OverlayWindow：`Left=20, Top=20, Width=300, Height=300, Opacity=0.7`，`Topmost=True`，`ShowInTaskbar=False`。

## Scope

- RendererWindow（屏幕外 WebView2 渲染）
- DOM 多选择器查询 + 最大面积候选
- CapturePreviewAsync + 按实际渲染比例裁剪
- OverlayWindow 显示裁剪图片
- DispatcherTimer + SemaphoreSlim 防重入
- 默认位置/尺寸/透明度

## Non-Goals

- 鼠标穿透 — Stage 04
- 全局热键 — Stage 04
- 游戏人物位置同步 — 红线
- EditOverlay — Stage 04
- 读取游戏内存/进程注入 — 红线

## Risks

| Risk | Mitigation |
|------|-----------|
| DOM 变化 | 多选择器 + 最大面积候选 + 集中管理选择器 |
| 坐标漂移 | 每次刷新重新查询 DOM |
| WebView2 未加载 | NavigationCompleted 之后启动定时器 |
| 截图重入 | SemaphoreSlim 跳过本轮 |

## Test Strategy

- 单元测试：裁剪坐标换算、候选选择逻辑（纯逻辑部分）
- SCAFFOLD：RendererWindow + OverlayWindow
- 手动验收：启动确认左上角显示地图截图

## Acceptance

- Overlay 位于 (20,20)，300×300，Opacity 0.7
- 显示裁剪地图图片（非完整网页）
- 每 3 秒刷新
- RendererWindow 不可见（屏幕外）
- Release build 0e0w，测试全通过
