# 当前任务

## 阶段
STAGE-06

## 任务编号
STAGE-06-TASK-02-ENLARGE-OVERLAY

## 类型
IMPLEMENTATION

## 状态
READY

## 组件
Overlay 显示尺寸放大至 900×563

## 下一执行者
Cursor

---

## Allowed Path
- src/Anhei4Map.App/OverlayWindow.xaml.cs

## Forbidden
App.xaml.cs, RendererWindow, Win32Native, MapViewportDiagnostics, 其他 src/**, tests/**, *.csproj. New Files: NONE. New Dependencies: NONE.

---

## 实现

修改 OverlayWindow.xaml.cs 的两行常量：

```csharp
// 改前:
private const double MaxOverlayWidth = 600;
private const double MaxOverlayHeight = 375;

// 改后:
private const double MaxOverlayWidth = 900;
private const double MaxOverlayHeight = 563;
```

UpdateMapImage 的等比例缩放逻辑不变。975×720 地图 → scale=min(900/975,563/720)=min(0.923,0.782)=0.782 → ~762×563.

## 保持
整张地图、等比例、裁剪、鼠标穿透、HH:01 刷新、底部遮挡、diagnostics

## 验证
```powershell
dotnet build -c Release && dotnet test -c Release --no-build && git diff --check
```

## 提交
```
feat: enlarge overlay to 900x563
```

## Overengineering Check
PASS
