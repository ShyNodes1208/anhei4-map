# 当前任务

## 阶段
STAGE-03-CROPPED-MAP-OVERLAY

## 任务编号
STAGE-03-ACCEPTANCE-FIX-04

## 类型
FIX

## 状态
READY

## 组件
RendererWindow 1280×800 + 地图容器全视口布局

## 父任务
ACCEPTANCE-FIX-03 (90071df — 未 push)

## 下一执行者
Cursor

---

## 问题

DOM 地图截图源的宽高比约为 3.17:1 (400×126)，地图纵向视野过短。网站地图容器当前保持其默认的宽扁布局。单纯增大 Overlay MaxHeight 无效——截图源本身比例决定了最终显示。

## 修复策略

后台 RendererWindow 扩高到 1280×800，并使用 JavaScript 强制地图容器占满整个 WebView2 视口，触发 resize/invalidateSize 重排。

---

## 允许修改的精确路径

- `src/Anhei4Map.App/RendererWindow.xaml`
- `src/Anhei4Map.App/RendererWindow.xaml.cs`

## 禁止修改范围

- OverlayWindow.xaml / OverlayWindow.xaml.cs
- App.xaml.cs
- `src/Anhei4Map.Core/**`、`src/Anhei4Map.Infrastructure/**`
- `tests/**`、`docs/design/**`、`*.csproj`
- 不修改截图/裁剪/Overlay 缩放逻辑
- 不实现定时刷新、穿透、热键、人物同步

---

## 步 1：RendererWindow.xaml

```xml
<Window ... Width="1280" Height="800" ...>
```

将 `Height` 从 `720` 改为 `800`。其余属性不变。

---

## 步 2：PrepareMapViewportAsync 方法

在 `InitializeWebViewAsync` 中，`NavigationCompleted.IsSuccess == true` 之后、触发 `NavigationReady` 之前调用：

```csharp
private async Task<bool> PrepareMapViewportAsync()
{
    const string script = @"
(function() {
  try {
    document.documentElement.style.margin = '0';
    document.documentElement.style.padding = '0';
    document.documentElement.style.overflow = 'hidden';
    document.body.style.margin = '0';
    document.body.style.padding = '0';
    document.body.style.overflow = 'hidden';

    const selectors = ['#map', '.leaflet-container', '[class*=""map""]'];
    let best = null, bestArea = 0;
    for (const sel of selectors) {
      const el = document.querySelector(sel);
      if (!el) continue;
      const r = el.getBoundingClientRect();
      if (r.width<=0||r.height<=0) continue;
      const area = r.width*r.height;
      if (area>bestArea) { best=el; bestArea=area; }
    }
    if (!best) return false;

    best.style.position = 'fixed';
    best.style.left = '0';
    best.style.top = '0';
    best.style.width = '100vw';
    best.style.height = '100vh';
    best.style.maxWidth = 'none';
    best.style.maxHeight = 'none';
    best.style.margin = '0';
    best.style.padding = '0';
    best.style.zIndex = '9999';

    window.dispatchEvent(new Event('resize'));

    if (typeof window.L !== 'undefined' && window.L.map) {
      try { window.L.map.invalidateSize(); } catch(e) {}
    }

    return true;
  } catch(e) { return false; }
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

**冻结规则：**

| 事项 | 规则 |
|------|------|
| 选择器 | 复用现有 3 个 |
| 目标容器 | 面积最大的可见地图容器 |
| CSS 定位 | `fixed; left:0; top:0; width:100vw; height:100vh` |
| resize 事件 | `window.dispatchEvent(new Event('resize'))` |
| Leaflet | 仅当 `L` 和 `L.map` 存在且 `invalidateSize` 可调用时调用 |
| Leaflet 失败 | 不影响函数返回值 |
| 整体失败 | 返回 false；不抛异常；不退出程序 |
| 执行线程 | WebView2 UI 线程 |

---

## 步 3：初始化流程整合

在 `InitializeWebViewAsync` 中：

```
NavigationCompleted.IsSuccess == true
  → await PrepareMapViewportAsync()
  → await Task.Delay(1500)  // 等待地图重新布局和瓦片加载
  → NavigationReady?.Invoke()
```

保留现有 3000ms 预热（在 App.xaml.cs 的 StartInitialCaptureAsync 中已存在），加上此处的 1500ms 重排等待，总共约 4500ms。

**不得**引入永久刷新或循环重试。

---

## 步 4：预期效果

- 截图源比例 ≈ 1280:800 = 1.6:1
- Overlay 接近 400×250
- 地图纵向上完整显示更多区域
- 所有现有截图/裁剪/Overlay 逻辑不变

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
fix: expand RendererWindow to 1280x800 and force map to full viewport

Resize offscreen renderer to 1280x800 and inject CSS to position the
map container at fixed 0,0 spanning 100vw×100vh. Dispatches resize
event and optionally calls Leaflet invalidateSize. Prepares the map
to render at a ~1.6:1 ratio matching the target 400x250 overlay.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-03-ACCEPTANCE-FIX-04
Type: FIX
Status: DONE
Commit: <hash>
Files Modified:
  - src/Anhei4Map.App/RendererWindow.xaml
  - src/Anhei4Map.App/RendererWindow.xaml.cs
Renderer Viewport: 1280x800
Map Container: fixed 100vw×100vh, resize event + Leaflet fallback
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
```
