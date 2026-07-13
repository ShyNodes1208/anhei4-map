# 当前任务

## 阶段
STAGE-03-CROPPED-MAP-OVERLAY

## 任务编号
STAGE-03-ACCEPTANCE-FIX-05

## 类型
FIX

## 状态
READY

## 组件
视口准备降级 + 导航代次防护

## 父任务
ACCEPTANCE-FIX-04 (d4bdeba — 90% 验证阻断 NavigationReady 导致 Overlay 不显示)

## 下一执行者
Cursor

---

## 允许修改的精确路径

- `src/Anhei4Map.App/RendererWindow.xaml.cs`

## 禁止修改范围

- RendererWindow.xaml
- OverlayWindow.xaml / OverlayWindow.xaml.cs
- App.xaml.cs
- `src/Anhei4Map.Core/**`、`src/Anhei4Map.Infrastructure/**`
- `tests/**`、`docs/design/**`、`*.csproj`
- 不修改截图/裁剪/Overlay 逻辑
- 不实现定时刷新、穿透、热键

---

## 步 1：导航代次字段

```csharp
private int _navigationGeneration;
private bool _navigationReadyRaised;
```

---

## 步 2：NavigationStarting

```csharp
private void OnNavigationStarting(...)
{
    Interlocked.Increment(ref _navigationGeneration);
    _navigationCompletedSuccessfully = false;
    _navigationReadyRaised = false;
    // ... 现有 DomainPolicy 检查不变
}
```

---

## 步 3：NavigationCompleted

```csharp
private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
{
    if (!e.IsSuccess) return;
    _navigationCompletedSuccessfully = true;

    var generation = _navigationGeneration;

    // 尝试视口准备——最多 3 次，500ms 间隔
    for (var attempt = 0; attempt < 3; attempt++)
    {
        if (_isClosed || generation != _navigationGeneration) return;
        if (await PrepareMapViewportAsync()) break;
        if (attempt < 2) await Task.Delay(500);
    }

    // 无论视口准备成功与否，只要仍是同一次导航且未关闭且未触发过
    if (!_isClosed && generation == _navigationGeneration && !_navigationReadyRaised)
    {
        _navigationReadyRaised = true;
        if (!_isClosed) RaiseNavigationReady();
    }
}
```

---

## 步 4：规则冻结

| 规则 | 值 |
|------|-----|
| PrepareMapViewportAsync 最大尝试 | 3 次 |
| 间隔 | 500ms |
| 成功 | 立即停止重试 |
| 全部失败 | 不影响 NavigationReady 触发 |
| 导航代次变化 | 旧流程立即停止，不触发 NavigationReady |
| NavigationReady 触发条件 | `!_isClosed && generation==_navigationGeneration && !_navigationReadyRaised` |
| 防重复 | `_navigationReadyRaised` 保证同次导航只触发一次 |
| 旧流程防护 | 每次 await 后检查 `generation == _navigationGeneration` |

---

## 保留

- RendererWindow 1280×800
- PrepareMapViewportAsync 全部 CSS 注入 + requestAnimationFrame + resize + 90% 验证
- 3000ms 预热 + 10 次截图重试
- 地图视觉就绪检查
- DOM 裁剪 + 完整截图
- Overlay 最大 400×250 + 按比例缩放
- 截图成功后才 Show Overlay

---

## 验证命令

```powershell
dotnet build -c Release
dotnet test -c Release --no-build
git diff --check
```

## Git 提交信息

```
fix: make viewport preparation best-effort, guard against stale navigation

PrepareMapViewportAsync is attempted up to 3 times with 500ms intervals
but its failure no longer blocks NavigationReady. Navigation generation
tracking prevents stale async flows from firing readiness after a newer
navigation starts. This ensures Overlay displays even when viewport
restyling cannot meet the 90% coverage threshold.
```

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-03-ACCEPTANCE-FIX-05
Type: FIX | Status: DONE | Commit: <hash>
Viewport Preparation: Best-effort, 3 attempts × 500ms
Fallback: NavigationReady fires regardless of preparation result
Navigation Generation: Interlocked.Increment guards stale async flows
Build: Release 0e0w | Tests: 160/160 PASS
```
