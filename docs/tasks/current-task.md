# 当前任务

## 阶段
STAGE-07

## 任务编号
STAGE-07-TASK-01-ENLARGE-OVERLAY-50-PERCENT

## 类型
BEHAVIOR

## 状态
READY

## 组件
将地图 Overlay 最大显示尺寸增加 50%（630×394 → 945×591）

## 下一执行者
Cursor

---

## Allowed Paths
- src/Anhei4Map.App/OverlayWindow.xaml.cs

## Forbidden
App.xaml, App.xaml.cs, ControlWindow.xaml, ControlWindow.xaml.cs, RendererWindow.xaml, RendererWindow.xaml.cs, MainWindow.xaml, MainWindow.xaml.cs, Win32Native.cs, Win32Interop.cs, MapViewportDiagnostics.cs, 其他 src/**, tests/**, *.csproj, artifacts/**. New Files: NONE. New Dependencies: NONE.

---

## 实现

### 修改两行常量

文件：`src/Anhei4Map.App/OverlayWindow.xaml.cs`

```csharp
// 改前（第 9-10 行）:
private const double MaxOverlayWidth = 630;
private const double MaxOverlayHeight = 394;

// 改后:
private const double MaxOverlayWidth = 945;
private const double MaxOverlayHeight = 591;
```

计算：
- 630 × 1.5 = 945
- 394 × 1.5 = 591

### 不修改

- `UpdateMapImage` 方法逻辑（等比例缩放、浮点容差、边界检查全部不变）
- `OnSourceInitialized` 方法（鼠标穿透、Topmost 不变）
- `OverlayWindow` 构造函数
- 其他所有文件

### 缩放行为

常量修改后，`UpdateMapImage` 自动适配新上限：

```
scale = Math.Min(945 / pixelWidth, 591 / pixelHeight)
```

以当前 975×720 地图为例：
- scale = min(945/975, 591/720) = min(0.969, 0.821) = 0.821
- 显示尺寸 ≈ 800×591（原 533×394）

地图保持等比例，由高度限制主导。

## 保持

- 等比例缩放算法
- 浮点容差处理（SizeTolerance = 0.001）
- 地图裁剪逻辑
- 地图位置
- Topmost
- 鼠标左键、右键、滚轮穿透（WS_EX_TRANSPARENT + WS_EX_NOACTIVATE）
- 自动 HH:01 刷新
- 手动刷新
- 任务栏控制窗口
- 退出功能
- RendererWindow 全部行为
- 便携版 artifacts

## 验证

```powershell
dotnet build .\anhei4-map.sln -c Release
dotnet test .\anhei4-map.sln -c Release --no-build
git diff --check
```

要求：
- Build 0 errors
- Build 0 warnings
- Tests 160/160
- Diff check PASS
- 不 push
- 不生成便携版
- 不创建 Release

## 提交

```
feat: enlarge overlay display by 50% to 945x591
```

## Overengineering Check
PASS — 仅改动两个常量值，不涉及任何逻辑变更
