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
ACCEPTANCE-FIX-03 (90071df)

## 前一派发提交（无效）
0b51cbeb968f100a2571169b8d0c155ef21ee864 — L.map.invalidateSize() 规则无效，已废弃

## 下一执行者
Cursor

---

## 允许修改的精确路径

- `src/Anhei4Map.App/RendererWindow.xaml`（Height 720→800）
- `src/Anhei4Map.App/RendererWindow.xaml.cs`（PrepareMapViewportAsync + 整合）

## 禁止修改范围

- OverlayWindow.xaml / OverlayWindow.xaml.cs
- App.xaml.cs
- `src/Anhei4Map.Core/**`、`src/Anhei4Map.Infrastructure/**`
- `tests/**`、`docs/design/**`、`*.csproj`
- 不修改截图/裁剪/Overlay 缩放逻辑
- 不实现定时刷新、穿透、热键、人物同步

---

## 步 1：RendererWindow.xaml

`Height="800"`，其余不变。

---

## 步 2：PrepareMapViewportAsync（冻结）

```csharp
private async Task<bool> PrepareMapViewportAsync()
```

**规则表：**

| 事项 | 规则 |
|------|------|
| 前置条件 | _webViewInitialized && _navigationCompletedSuccessfully && CoreWebView2!=null && !_isClosed |
| 不满足条件 | 返回 false，不抛异常 |
| 地图选择器 | '#map'、'.leaflet-container'、'[class*="map"]'（与现有选择器一致） |
| 候选规则 | 面积最大、getComputedStyle 可见、width>0、height>0 |
| 未找到容器 | 返回 false |

**CSS 注入：**
```
document.documentElement.style.margin = '0'
document.documentElement.style.padding = '0'
document.documentElement.style.overflow = 'hidden'
document.body.style.margin = '0'
document.body.style.padding = '0'
document.body.style.overflow = 'hidden'
```

**地图容器样式：**
```
position = 'fixed'
left = '0'
top = '0'
width = '100vw'
height = '100vh'
maxWidth = 'none'
maxHeight = 'none'
margin = '0'
padding = '0'
zIndex = '2147483647'
```

**布局重排（冻结）：**
1. CSS 修改后调用两次 `window.requestAnimationFrame`（等待浏览器完成布局）
2. 然后调用 `window.dispatchEvent(new Event('resize'))`

**禁止访问 Leaflet 内部对象：**
- 不调用 `L.map.invalidateSize()`
- 不查找 Leaflet 地图实例
- 不猜测网站全局变量
- 不扫描私有字段
- 不反射第三方库内部对象

**布局验证：**
1500ms 后重新查询同一地图容器的 `getBoundingClientRect()`。必须满足：
- `rect.width > 0` 且 `rect.height > 0`
- `rect.width >= window.innerWidth * 0.9`
- `rect.height >= window.innerHeight * 0.9`

满足 → 返回 true。不满足、JS 异常、JSON 空/null → 返回 false。不抛异常。

---

## 步 3：初始化流程整合

`InitializeWebViewAsync` 中 `NavigationCompleted.IsSuccess == true` 之后：

```
1. bool ready = await PrepareMapViewportAsync()
2. if (!ready) return;  // 不触发 NavigationReady
3. NavigationReady?.Invoke(this, EventArgs.Empty)
```

NavigationReady 仅在 PrepareMapViewportAsync 返回 true 后才触发。

---

## 验证命令

```powershell
dotnet build -c Release
dotnet test -c Release --no-build
git diff --check
```

## Git 提交信息

```
fix: expand RendererWindow to 1280x800, force map to fill viewport

Resize offscreen window to 1280x800. PrepareMapViewportAsync injects
CSS to position the map container at fixed 0,0 spanning 100vw x 100vh.
Uses requestAnimationFrame x2 + resize event for layout reflow. After
1500ms verifies container covers >=90% of viewport before allowing
NavigationReady. No Leaflet internal access.
```

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-03-ACCEPTANCE-FIX-04
Type: FIX | Status: DONE
Commit: <hash>
PrepareMapViewportAsync: CSS injection → requestAnimationFrame ×2 → resize → 1500ms → verify ≥90% viewport coverage
Leaflet: No map instance access
NavigationReady: Only after PrepareMapViewportAsync returns true
Build: Release 0 errors 0 warnings | Tests: 160/160 PASS
```
