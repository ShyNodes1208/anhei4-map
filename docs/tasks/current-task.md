# 当前任务

## 阶段
STAGE-03-CROPPED-MAP-OVERLAY

## 任务编号
STAGE-03-TASK-01

## 类型
IMPLEMENTATION

## 状态
READY

## 组件
RendererWindow + DOM map-region detection

## 下一执行者
Cursor

---

## 允许修改的精确路径

- `src/Anhei4Map.App/RendererWindow.xaml`（新建）
- `src/Anhei4Map.App/RendererWindow.xaml.cs`（新建）
- `src/Anhei4Map.App/App.xaml.cs`（修改——创建 RendererWindow 替代 MainWindow）
- `src/Anhei4Map.Core/Models/MapRegion.cs`（新建——DOM 查询结果类型）

## 禁止修改范围

- `src/Anhei4Map.Infrastructure/**`
- `src/Anhei4Map.Core/Services/**`
- `src/Anhei4Map.Core/State/**`
- `src/Anhei4Map.Core/Interop/**`
- `MainWindow.xaml` / `MainWindow.xaml.cs`
- `tests/**`
- `docs/design/**`
- `*.csproj`
- 不实现截图
- 不实现裁剪
- 不实现 OverlayWindow
- 不实现定时器
- 不实现 TASK-02+

---

## 步 1：MapRegion 记录类型（冻结）

`src/Anhei4Map.Core/Models/MapRegion.cs`：

```csharp
namespace Anhei4Map.Core.Models;

public sealed record MapRegion(
    double Left,
    double Top,
    double Width,
    double Height,
    double DevicePixelRatio);
```

- 所有字段为 `double`（`getBoundingClientRect()` 返回小数）
- 不在 DOM 查询阶段提前取整——取整由 TASK-02 裁剪时处理

---

## 步 2：RendererWindow.xaml

```xml
<Window x:Class="Anhei4Map.App.RendererWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:wpf="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
        WindowStyle="None"
        ResizeMode="NoResize"
        ShowInTaskbar="False"
        Topmost="False"
        Width="1280"
        Height="720"
        Background="White">
    <Grid>
        <wpf:WebView2 x:Name="webView" />
    </Grid>
</Window>
```

冻结属性表不变。

---

## 步 3：RendererWindow.xaml.cs — 字段与状态

```csharp
public partial class RendererWindow : Window
{
    private bool _webViewInitialized;           // EnsureCoreWebView2Async 完成
    private bool _navigationCompletedSuccessfully; // 最近一次 NavigationCompleted.IsSuccess
    private bool _isClosed;

    public RendererWindow()
    {
        InitializeComponent();
        Left = -10000;
        Top = -10000;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
    }
}
```

- `_navigationCompletedSuccessfully`：在 `NavigationCompleted` 中 `e.IsSuccess == true` 时设为 `true`
- `NavigationStarting` 中设为 `false`（新导航开始时重置）
- `_isClosed`：窗口 `Closed` 事件中设为 `true`，阻止关闭后访问 WebView2

### WebView2 初始化

与 Stage 02 一致（8 settings、DomainPolicy、popups/downloads blocked），简化为无 WS_EX_TOOLWINDOW、无 CancellationTokenSource。

---

## 步 4：RendererWindow 对外 API（冻结）

```csharp
public async Task<MapRegion?> TryGetMapRegionAsync()
```

**返回值：** `MapRegion`（成功）或 `null`（任何失败）。

**就绪检查——仅当以下全部满足时才执行脚本：**
1. `_webViewInitialized == true`
2. `_navigationCompletedSuccessfully == true`
3. `webView.CoreWebView2 != null`
4. `_isClosed == false`

任一不满足 → 返回 `null`，不抛异常。

---

## 步 5：DOM 可见候选规则（冻结 JavaScript）

```javascript
(function() {
  const selectors = ['#map', '.leaflet-container', '[class*="map"]'];
  let best = null, bestArea = 0;
  for (const sel of selectors) {
    const el = document.querySelector(sel);
    if (!el) continue;
    const style = getComputedStyle(el);
    if (style.display === 'none' || style.visibility === 'hidden' || style.opacity === '0') continue;
    const r = el.getBoundingClientRect();
    if (r.width <= 0 || r.height <= 0) continue;
    const area = r.width * r.height;
    if (area > bestArea) { best = el; bestArea = area; }
  }
  if (!best) return null;
  const r = best.getBoundingClientRect();
  return JSON.stringify({ left: r.left, top: r.top, width: r.width, height: r.height, dpr: window.devicePixelRatio || 1 });
})()
```

**候选规则：**
- `getComputedStyle` 检查：display != "none"、visibility != "hidden"、opacity != "0"
- `getBoundingClientRect`：width > 0、height > 0
- 面积最大者胜出

**JSON 规则：**
- `ExecuteScriptAsync` 返回 JSON 编码字符串
- C# 使用 `System.Text.Json.JsonSerializer.Deserialize<MapRegion>()` + `PropertyNameCaseInsensitive = true`
- JavaScript 返回 `null` → C# 收到字符串 `"null"` → 返回 `null`
- JSON 解析失败（`JsonException`）→ 返回 `null`
- 反序列化后任一字段为 `NaN`、`Infinity`、负值 → 返回 `null`
- 反序列化后 Width 或 Height 为 0 → 返回 `null`

**所有失败路径返回 `null`，不抛异常，不关闭程序。**

---

## 步 6：App.xaml.cs 修改（冻结）

```csharp
// 删除旧 MainWindow 创建逻辑，替换为:
var rendererWindow = new RendererWindow();
rendererWindow.Show();
```

- RendererWindow 实例必须保存为 `App` 的字段（如 `private RendererWindow? _rendererWindow`），不能仅使用局部变量
- 不创建 MainWindow
- 不创建 OverlayWindow
- 不执行截图
- MainWindow.xaml/.cs 保留在项目中但不使用

---

## 验证命令

```powershell
dotnet build -c Release
dotnet test -c Release --no-build
git diff --check
```

## 验证标准

- dotnet build -c Release 0 errors 0 warnings
- dotnet test -c Release --no-build 160/160 PASS
- git diff --check clean

---

## Git 提交信息

```
feat: create offscreen RendererWindow with DOM map region detection

STAGE-03-TASK-01: New RendererWindow positioned offscreen at (-10000,-10000)
with embedded WebView2 loading helltides.com. QueryMapRegionAsync() uses
ExecuteScriptAsync with multi-selector fallback (#map, .leaflet-container,
[class*="map"]) and returns the largest visible candidate. App.xaml.cs
now creates RendererWindow instead of MainWindow.
```

---

## Cursor 最终报告格式

```
TASK_COMPLETE

Task: STAGE-03-TASK-01
Component: RendererWindow + DOM map-region detection
Type: IMPLEMENTATION
Status: DONE
Commit: <hash>
Files Created:
  - src/Anhei4Map.App/RendererWindow.xaml
  - src/Anhei4Map.App/RendererWindow.xaml.cs
  - src/Anhei4Map.Core/Models/MapRegion.cs
Files Modified:
  - src/Anhei4Map.App/App.xaml.cs
WebView2 Init: Per Stage 02 rules (8 settings, DomainPolicy)
DOM Selectors: #map, .leaflet-container, [class*="map"]
DOM Result: MapRegion record
Failure: All failures return null, no throw, no shutdown
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
```
