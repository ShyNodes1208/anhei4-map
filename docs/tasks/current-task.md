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

## 步 1：MapRegion 记录类型

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

DOM 查询成功返回 MapRegion。找不到地图元素返回 null。

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

冻结属性：WindowStyle=None, ResizeMode=NoResize, ShowInTaskbar=False, Topmost=False, Width=1280, Height=720, Background=White, WebView2 Name=webView。

---

## 步 3：RendererWindow.xaml.cs 初始化

```csharp
public partial class RendererWindow : Window
{
    private bool _webViewInitialized;
    private bool _isClosing;

    public RendererWindow()
    {
        InitializeComponent();
        Left = -10000;
        Top = -10000;
        Loaded += OnLoaded;
        Closing += OnClosing;
    }
}
```

Left=-10000, Top=-10000 在构造函数设置（XAML 不支持负坐标）。WebView2 初始化在 Loaded 事件中触发。

### WebView2 初始化（复用 Stage 02 规则）

1. CoreWebView2Environment.CreateAsync(userDataFolder=%LocalAppData%\Anhei4Map\WebView2)
2. webView.EnsureCoreWebView2Async(env)
3. 配置 8 项 settings（与 Stage 02 TASK-07 一致）
4. 注册 NavigationStarting（DomainPolicy）、NewWindowRequested（Handled）、DownloadStarting（Cancel）
5. webView.CoreWebView2.Navigate("https://helltides.com/")
6. NavigationCompleted 中设置 _webViewInitialized = true

简化规则（相对于 Stage 02）：不需要 WS_EX_TOOLWINDOW；初始化失败不弹 MessageBox（屏幕外窗口）；不需要 CancellationTokenSource。

---

## 步 4：DOM 查询方法

```csharp
public async Task<MapRegion?> QueryMapRegionAsync()
{
    if (!_webViewInitialized || webView.CoreWebView2 == null) return null;

    const string script = @"
(function() {
  const selectors = ['#map', '.leaflet-container', '[class*=""map""]'];
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
})()";

    string? json;
    try { json = await webView.CoreWebView2.ExecuteScriptAsync(script); }
    catch { return null; }

    if (string.IsNullOrWhiteSpace(json) || json == "null") return null;

    try { return JsonSerializer.Deserialize<MapRegion>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
    catch (JsonException) { return null; }
}
```

DOM 查询规则：选择器 #map, .leaflet-container, [class*="map"]；选面积最大且>0的可见元素；返回 left/top/width/height/dpr；无匹配返回 null；JS 异常返回 null；JSON 异常返回 null；WebView2 未就绪返回 null；所有失败路径不抛异常、不关闭程序。

---

## 步 5：App.xaml.cs 修改

删除 MainWindow 创建逻辑，改为创建 RendererWindow：

```csharp
var rendererWindow = new RendererWindow();
rendererWindow.Show();
```

MainWindow.xaml/.cs 保留在项目中但不使用。

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
