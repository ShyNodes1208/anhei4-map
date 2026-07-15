# 当前任务

## 阶段
STAGE-06

## 任务编号
STAGE-06-TASK-01-HOURLY-REFRESH-FIX-01

## 类型
FIX

## 状态
READY

## 组件
HH:01 刷新增加页面 Reload

## 下一执行者
Cursor

---

Base: 9634010d5f4b10e246f9d2209469fa4d57d0f722

## Allowed Paths
- src/Anhei4Map.App/App.xaml.cs
- src/Anhei4Map.App/RendererWindow.xaml.cs

## Forbidden
OverlayWindow, Win32Native, MapViewportDiagnostics, 其他 src/**, tests/**, *.csproj. New Files: NONE. New Dependencies: NONE.

---

## 实现

### RendererWindow.xaml.cs — 新增最小方法

```csharp
public async Task<bool> ReloadPageAsync()
{
    if (_isClosed || webView.CoreWebView2 == null || !_navigationCompletedSuccessfully)
        return false;

    var tcs = new TaskCompletionSource<bool>();
    void OnNavCompleted(object? s, CoreWebView2NavigationCompletedEventArgs e)
    {
        webView.CoreWebView2.NavigationCompleted -= OnNavCompleted;
        tcs.TrySetResult(e.IsSuccess);
    }

    webView.CoreWebView2.NavigationCompleted += OnNavCompleted;
    webView.CoreWebView2.Reload();

    var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30)));
    if (completed != tcs.Task)
    {
        webView.CoreWebView2.NavigationCompleted -= OnNavCompleted;
        return false;
    }

    return await tcs.Task;
}
```

### App.xaml.cs — 修改 OnHourlyRefreshTick

```csharp
private async void OnHourlyRefreshTick(object? sender, EventArgs e)
{
    _hourlyRefreshTimer!.Stop();
    try
    {
        if (_rendererWindow == null || _overlayWindow == null) return;

        var reloadOk = await _rendererWindow.ReloadPageAsync();
        if (!reloadOk) return;

        var bitmap = await _rendererWindow.CaptureAndCropMapAsync();
        if (bitmap != null) _overlayWindow.UpdateMapImage(bitmap);
    }
    catch { }
    finally { ScheduleNextHourlyRefresh(); }
}
```

---

## 规则

- Reload 失败 → 保留旧 Overlay，不作更新
- 截图失败 → 保留旧 Overlay
- Reload 最多等待 30s，超时返回 false
- finally 始终重新预约下一小时
- 不创建第二个 WebView2 / 不轮询 / 不新增状态机

---

## 验证
```powershell
dotnet build -c Release && dotnet test -c Release --no-build && git diff --check
```

## 提交
```
fix: reload page before hourly map refresh
```
