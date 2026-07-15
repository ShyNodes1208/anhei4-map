# 当前任务

## 阶段
STAGE-06

## 任务编号
STAGE-06-TASK-01-HOURLY-REFRESH

## 类型
IMPLEMENTATION

## 状态
READY

## 组件
每小时整点 01 分自动刷新地图

## 下一执行者
Cursor

---

## 背景

HellTides 每小时整点刷新内容。程序只需在每小时 HH:01 执行一次 CaptureAndCropMapAsync 并更新 Overlay，替代已废弃的倒计时轮询方案。

## Allowed Paths
- src/Anhei4Map.App/App.xaml.cs

## Forbidden
RendererWindow.xaml.cs, OverlayWindow, Win32Native, MapViewportDiagnostics, 其他 src/**, tests/**, *.csproj. New Files: NONE. New Dependencies: NONE.

## 禁止实现
- 读取网页倒计时
- 5 秒轮询
- 倒计时解析
- 跳变检测
- 多个 DispatcherTimer 并发

---

## 实现

在 App.xaml.cs 中添加：

```csharp
private DispatcherTimer? _hourlyRefreshTimer;

private void ScheduleNextHourlyRefresh()
{
    var now = DateTime.Now;
    var next = new DateTime(now.Year, now.Month, now.Day, now.Hour, 1, 0);
    if (next <= now) next = next.AddHours(1);
    var delay = next - now;

    if (_hourlyRefreshTimer != null)
        _hourlyRefreshTimer.Stop();
    else
    {
        _hourlyRefreshTimer = new DispatcherTimer();
        _hourlyRefreshTimer.Tick += OnHourlyRefreshTick;
    }
    _hourlyRefreshTimer.Interval = delay;
    _hourlyRefreshTimer.Start();
}

private async void OnHourlyRefreshTick(object? sender, EventArgs e)
{
    _hourlyRefreshTimer!.Stop();
    try
    {
        if (_rendererWindow == null || _overlayWindow == null) return;
        var bitmap = await _rendererWindow.CaptureAndCropMapAsync();
        if (bitmap != null) _overlayWindow.UpdateMapImage(bitmap);
    }
    catch { }
    finally { ScheduleNextHourlyRefresh(); }
}
```

启动：首次 Overlay 显示成功后调用 `ScheduleNextHourlyRefresh()`。

调度逻辑：计算距离下一个本地时间 HH:01:00 的时间差作为 Timer interval。Tick 时 Stop → 刷新 → finally 重新 Schedule。程序挂起后恢复时，如果当前时间已超过计划 HH:01，`new DateTime(...)` 构造出过去时间，`next <= now` 将其加 1 小时，下一次仍正常触发。

---

## 验证
```powershell
dotnet build -c Release && dotnet test -c Release --no-build && git diff --check
```

## 提交
```
feat: auto-refresh map at HH:01 every hour
```
