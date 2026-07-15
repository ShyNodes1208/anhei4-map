# 当前任务

## 阶段
STAGE-04

## 任务编号
STAGE-04-TASK-04-REVISION

## 类型
FIX

## 状态
READY

## 组件
撤销焦点裁剪 + 放大 Overlay 至 600×375

## 下一执行者
Cursor

---

## Allowed Paths (2 files)
- src/Anhei4Map.App/RendererWindow.xaml.cs
- src/Anhei4Map.App/OverlayWindow.xaml.cs

## Forbidden
App.xaml.cs, MapViewportDiagnostics, MapRegion 模型, DomainPolicy, MainWindow, 其他 src/**, tests/**, *.csproj, *.sln. New Files: NONE. New Dependencies: NONE.

---

## 步 1: 撤销焦点裁剪 (RendererWindow.xaml.cs)

删除以下内容：

1. 四个常量 (约第 142–145 行):
```csharp
private const double FocusLeftRatio = 0.2540;
private const double FocusTopRatio = 0.0000;
private const double FocusRightRatio = 0.6917;
private const double FocusBottomRatio = 0.8528;
```

2. `TryApplyFocusCrop` 方法（约 30 行）

3. 恢复首次裁剪后的逻辑为：
```csharp
cropped.Freeze();

// (diagnostic branch unchanged, uses cropped)

return cropped;
```

**保留:** scrollIntoView, IsMapVisualReadyAsync, MapRegion 重查询, 导航检查, CapturePreview, 单次 CroppedBitmap (975×720), diagnostics 三件套.

---

## 步 2: 放大 Overlay (OverlayWindow.xaml.cs)

修改第 8–9 行：

```csharp
// 改前:
private const double MaxOverlayWidth = 400;
private const double MaxOverlayHeight = 250;

// 改后:
private const double MaxOverlayWidth = 600;
private const double MaxOverlayHeight = 375;
```

`UpdateMapImage` 的等比例缩放逻辑不变。975×720 → scale=min(600/975,375/720)=min(0.615,0.521)=0.521 → ~508×375.

---

## 验证
```powershell
dotnet build -c Release
dotnet test -c Release --no-build
git diff --check
```

## 提交
```
fix: remove focus crop and enlarge overlay to 600x375
```
