# 当前任务

## 阶段
STAGE-03-CROPPED-MAP-OVERLAY

## 任务编号
STAGE-03-ACCEPTANCE-FIX-02

## 类型
FIX

## 状态
READY

## 组件
地图底层瓦片/Canvas 视觉就绪检查 + 延迟初始截图

## 父任务
STAGE-03-TASK-02 (481c30b) + ACCEPTANCE-FIX-01 (875f659)

## 下一执行者
Cursor

---

## 问题

NavigationCompleted 后标记层（圆圈、Marker）先行渲染完毕，但底层地图瓦片或 Canvas 仍在异步加载。首张非 null 截图被过早接受，导致 Overlay 只显示标记圆圈而没有地形底图。

## 修复策略

在初始截图流程中增加两步守卫：
1. 3000ms 预热延迟
2. 每轮重试前先验证地图视觉已就绪

---

## 允许修改的精确路径

- `src/Anhei4Map.App/RendererWindow.xaml.cs`
- `src/Anhei4Map.App/App.xaml.cs`（如需修改初始截图重试流程）

## 禁止修改范围

- OverlayWindow.xaml / OverlayWindow.xaml.cs
- RendererWindow.xaml
- `src/Anhei4Map.Core/**`、`src/Anhei4Map.Infrastructure/**`
- `tests/**`、`docs/design/**`、`*.csproj`
- 不修改 DOM 裁剪、scale 公式、正方形裁剪、Overlay 属性
- 不实现定时刷新、鼠标穿透、热键、人物同步

---

## 步 1：IsMapVisualReadyAsync

在 `RendererWindow.xaml.cs` 中添加：

```csharp
private async Task<bool> IsMapVisualReadyAsync()
{
    const string script = @"
(function() {
  const selectors = ['#map', '.leaflet-container', '[class*=""map""]'];
  let best = null, bestArea = 0;
  for (const sel of selectors) {
    const el = document.querySelector(sel);
    if (!el) continue;
    const style = getComputedStyle(el);
    if (style.display==='none'||style.visibility==='hidden'||style.opacity==='0') continue;
    const r = el.getBoundingClientRect();
    if (r.width<=0||r.height<=0) continue;
    const area = r.width*r.height;
    if (area>bestArea) { best=el; bestArea=area; }
  }
  if (!best) return false;

  // 1) img tiles
  const imgs = best.querySelectorAll('img');
  for (const img of imgs) {
    if (img.complete && img.naturalWidth>0 && img.naturalHeight>0) {
      const s = getComputedStyle(img);
      if (s.display!=='none'&&s.visibility!=='hidden') return true;
    }
  }

  // 2) canvas
  const canvases = best.querySelectorAll('canvas');
  for (const c of canvases) {
    if (c.width>0 && c.height>0) {
      const cr = c.getBoundingClientRect();
      if (cr.width>0 && cr.height>0) {
        const s = getComputedStyle(c);
        if (s.display!=='none'&&s.visibility!=='hidden') return true;
      }
    }
  }

  // 3) CSS background-image
  const bg = getComputedStyle(best).backgroundImage;
  if (bg && bg!=='none') return true;
  for (const el of best.querySelectorAll('*')) {
    const bgi = getComputedStyle(el).backgroundImage;
    if (bgi && bgi!=='none') return true;
  }

  return false;
})()";

    if (!_webViewInitialized || webView.CoreWebView2 == null || _isClosed) return false;

    try
    {
        var result = await webView.CoreWebView2.ExecuteScriptAsync(script);
        return string.Equals(result, "true", StringComparison.OrdinalIgnoreCase);
    }
    catch
    {
        return false;
    }
}
```

**规则：**
- 先找到面积最大的可见地图容器（复用选择器）
- 容器未找到 → 返回 false
- 检查优先级：img tiles → canvas → CSS background-image
- img: `complete==true && naturalWidth>0 && naturalHeight>0` + 可见
- canvas: `width>0 && height>0 && rect.width>0 && rect.height>0` + 可见
- 单独的 SVG 标记/圆圈/Marker 不算"底层地图已就绪"
- JS 异常 → 返回 false
- 不抛异常、不退出程序

---

## 步 2：修正初始截图重试流程

在 `App.xaml.cs` 的 `StartInitialCaptureAsync` 中修改：

```csharp
private async Task StartInitialCaptureAsync()
{
    const int maxAttempts = 10;
    const int warmupDelayMs = 3000;
    const int retryDelayMs = 1000;

    // 预热延迟——等待动态地图初始渲染
    await Task.Delay(warmupDelayMs);

    for (var attempt = 0; attempt < maxAttempts; attempt++)
    {
        if (attempt > 0) await Task.Delay(retryDelayMs);

        // 检查地图视觉是否就绪
        var isReady = await _rendererWindow!.Dispatcher.InvokeAsync(
            () => _rendererWindow.IsMapVisualReadyAsync());

        if (!isReady) continue;

        // 视觉就绪 → 执行截图
        var bitmap = await _rendererWindow!.Dispatcher.InvokeAsync(
            () => _rendererWindow.CaptureAndCropMapAsync());

        if (bitmap != null)
        {
            _overlayWindow!.Dispatcher.Invoke(() =>
            {
                _overlayWindow.UpdateMapImage(bitmap);
                if (!_overlayWindow.IsVisible) _overlayWindow.Show();
            });
            return;
        }
    }
    // 10 次全部失败 → 静默停止
}
```

---

## 步 3：冻结规则表

| 规则 | 值 |
|------|-----|
| 预热延迟 | 3000ms（NavigationReady 后等待渲染） |
| 最大尝试次数 | 10 |
| 重试间隔 | 1000ms |
| 视觉检查 | IsMapVisualReadyAsync 返回 true |
| 截图条件 | 视觉就绪 **且** CaptureAndCropMapAsync 返回非 null |
| 首张成功截图 | UpdateMapImage + Show OverlayWindow + 停止重试 |
| 全部失败 | 静默停止，不弹窗，不退出 |
| Interlocked 防重复 | 保持现有逻辑 |
| Keep existing | DOM 裁剪、正方形裁剪、Overlay 属性 |

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
- 手动：Overlay 显示完整正方形地图（含地形底图 + 标记）

---

## Git 提交信息

```
fix: add map tile/canvas visual readiness check before initial capture

After NavigationCompleted, wait 3000ms then poll IsMapVisualReadyAsync
up to 10 times at 1000ms intervals. Only accept the first non-null
screenshot after confirming img tiles, canvas, or CSS background-image
have rendered inside the map container. Prevents capturing a partially
loaded page with only marker overlays visible.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-03-ACCEPTANCE-FIX-02
Parent: ACCEPTANCE-FIX-01 (875f659)
Type: FIX
Status: DONE
Commit: <hash>
Files Modified: <list>
Visual Check: img tiles / canvas / CSS background-image
Warm-up: 3000ms | Retry: 10 × 1000ms
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
```
