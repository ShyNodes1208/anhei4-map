# 当前任务

## 状态
READY

## 类型
FIX

## 修复编号
STAGE-01-REVIEW-FIX-04

## 对应 Codex 发现
S01-004 (MEDIUM)

## 阶段
STAGE-01-FOUNDATION (修复轮)

## 分支
feature/01-foundation

## Worktree
D:\AIProjects\anhei4-map-worktrees\stage-01-foundation

---

## 问题证据

[WindowBoundsNormalizer.cs:28,44-52](src/Anhei4Map.Core/Services/WindowBoundsNormalizer.cs#L28-L52):

```csharp
var primary = workAreas[0];
// ...
if (width > primary.Width) { width = primary.Width; }
if (height > primary.Height) { height = primary.Height; }
```

`IsSufficientlyVisible()` 正确遍历所有工作区检查可见性，但 max size 裁剪始终使用 `workAreas[0]`。当副屏大于主屏（如笔记本 1920×1080 外接 3840×2160）时，副屏上合法的宽窗口被错误按主屏尺寸裁剪。

现有测试两屏均为 1920×1080，未覆盖异构尺寸。

---

## 允许修改的精确路径

- `src/Anhei4Map.Core/Services/WindowBoundsNormalizer.cs`
- `tests/Anhei4Map.Tests/WindowBoundsNormalizerTests.cs`

## 禁止修改范围

- `src/Anhei4Map.App/**`
- `src/Anhei4Map.Infrastructure/**`
- `docs/design/**`
- `docs/plans/**`
- `*.csproj`
- `*.sln`
- 其他所有测试文件
- 不调用真实显示器 API（System.Windows.Forms.Screen 等）
- 不集成 DPI
- 不涉及 Win32 或 WPF

---

## 目标工作区选择规则

### 交集面积计算

对 `workAreas` 中每个 WorkArea，使用已有 `GetIntersectionArea()` 方法计算窗口与该工作区的交集面积。

### 选择规则

1. 计算窗口与每个工作区的交集面积。
2. **选择交集面积最大的工作区**作为目标工作区。
3. 如果多个工作区交集面积相同：**使用 `workAreas` 中索引较小的工作区**（保证确定性）。
4. 如果没有任何正交集（即所有交集面积均为 0）：**使用 `workAreas[0]`** 作为回退目标工作区。

### 负坐标工作区

负坐标工作区（如 `Left=-1920`）必须正常参与交集面积计算和选择。不应被排除。

### 空工作区列表

`workAreas.Length == 0` → 保持现有回退行为不变（返回 `(0, EdgePadding, DefaultWidth, DefaultHeight, saved.Opacity)`）。

---

## 尺寸限制规则

选择目标工作区后：

1. 宽度超过目标工作区宽度 → 限制到目标工作区宽度。
2. 高度超过目标工作区高度 → 限制到目标工作区高度。
3. 不应因为主屏较小而裁剪位于大副屏的合法窗口。
4. 合法位于大副屏、尺寸不超过副屏的窗口保持不变。
5. 小于最小尺寸（MinWidth=200, MinHeight=150）→ 保持现有默认值回退行为不变。
6. 完全离屏 → 保持现有重置到主屏右上角行为不变。
7. 20% 可见阈值 → 保持不变，不做任何修改。
8. 宽度和高度独立限制：只超宽时不错误修改高度；只超高时不错误修改宽度。

---

## RED 阶段 — 先写失败测试

### 测试 1: `LargerSecondaryScreen_DoesNotClampToSmallerPrimary`

```csharp
// 主屏 1920×1080, 副屏 2560×1440 at Left=1920
// 窗口位于副屏, 宽 2500 (大于主屏但小于副屏)
// 期望: 窗口保持不变, 不按主屏 1920 裁剪
var workAreas = new[] {
    new WorkArea(0, 0, 1920, 1080),
    new WorkArea(1920, 0, 2560, 1440)
};
var saved = new WindowPlacement(2000, 100, 2500, 800, 0.9);
var result = WindowBoundsNormalizer.Normalize(saved, workAreas);
Assert.Equal(2500, result.Width);
Assert.Equal(800, result.Height);
```

### 测试 2: `OversizedWindowOnSecondary_ClampsToSecondaryWorkArea`

窗口在副屏但尺寸超过副屏 → 按副屏尺寸限制，不按主屏。

### 测试 3: `NegativeCoordinateSecondary_IsSelectedByIntersection`

副屏位于主屏左侧（负数坐标），窗口合法位于副屏 → 窗口保持不变。

### 测试 4: `WindowSpanningTwoScreens_SelectsLargestIntersection`

窗口跨越两个屏幕 → 选择交集面积最大的工作区作为限制目标。

### 测试 5: `EqualIntersection_UsesStableTieBreak`

交集面积相同时使用较小的 workAreas 索引。

### 测试 6: `FullyOffscreen_UsesPrimaryFallback`

没有任何正交集 → 使用 workAreas[0] 的回退规则（现有行为不变）。

### 测试 7: `EmptyWorkAreas_UsesExistingSafeFallback`

空列表 → 现有回退行为不变。

### 测试 8: `ExistingValidPrimaryWindow_RemainsUnchanged`

主屏正常窗口不回归。

### 测试 9: `ExistingPartialVisibilityThresholdTests_RemainPassing`

20% 阈值测试继续通过。

### 测试 10: `WidthAndHeightAreClampedIndependently`

只超宽时高度不变；只超高时宽度不变。

### RED 预期

```
dotnet test --filter "FullyQualifiedName~WindowBoundsNormalizerTests" -c Release
```

预期新增测试 FAIL — 当前实现固定使用 `workAreas[0]` 进行尺寸裁剪，大副屏合法窗口被错误裁剪。

---

## GREEN 阶段 — 最小实现

### 1. 添加 `FindBestWorkArea` 辅助方法

```csharp
private static WorkArea FindBestWorkArea(
    int left, int top, int width, int height, WorkArea[] workAreas)
{
    var bestIndex = 0;
    long bestArea = 0;

    for (var i = 0; i < workAreas.Length; i++)
    {
        var intersection = GetIntersectionArea(left, top, width, height, workAreas[i]);
        if (intersection > bestArea)
        {
            bestArea = intersection;
            bestIndex = i;
        }
    }

    return workAreas[bestIndex];
}
```

### 2. 修改 `Normalize` 方法

将第 28 行的 `var primary = workAreas[0]` 保持不变（用于 fallback reset）。

将第 44-52 行的 max size 裁剪改为使用目标工作区：

```csharp
var targetArea = FindBestWorkArea(left, top, width, height, workAreas);

if (width > targetArea.Width)
{
    width = targetArea.Width;
}

if (height > targetArea.Height)
{
    height = targetArea.Height;
}
```

**关键：** `IsSufficientlyVisible` 和 fallback reset 的 `primary` 保持不变。只有 max size 裁剪改用 `targetArea`。

### 最小修改原则

- 不改动 `IsSufficientlyVisible`、`GetIntersectionArea` 的实现
- 不改动 fallback reset 逻辑（第 59-64 行）
- 不改动 min size clamp（第 34-42 行）
- 不新增第三方依赖
- 不调用真实显示器 API

---

## 定向测试命令

```powershell
dotnet test tests/Anhei4Map.Tests/ --filter "FullyQualifiedName~WindowBoundsNormalizerTests" -c Release
```

## 完整测试命令

```powershell
dotnet test tests/Anhei4Map.Tests/ -c Release --no-build
```

## Release Build 命令

```powershell
dotnet build -c Release
```

---

## 完成标准

- [ ] `FindBestWorkArea` 辅助方法实现
- [ ] `Normalize` 中 max size 裁剪使用 `targetArea`
- [ ] 10 个新测试写入 `WindowBoundsNormalizerTests.cs`
- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] 定向测试 FAIL (RED)
- [ ] 定向测试 PASS (GREEN: 24/24: 14原有 + 10新增)
- [ ] 完整测试 `dotnet test -c Release --no-build` 全部通过 (113: 103原有 + 10新增)
- [ ] `git diff --check` clean
- [ ] 只修改了 `WindowBoundsNormalizer.cs` 和 `WindowBoundsNormalizerTests.cs`
- [ ] 原有 14 个测试全部继续通过（无回归）

---

## Git 提交信息

```
fix: clamp window bounds to selected work area instead of primary

S01-004: WindowBoundsNormalizer now selects the target work area based
on largest intersection area, then clamps max width/height to that area.
Previously max size was always clamped to workAreas[0], incorrectly
resizing valid windows on larger secondary monitors.

Added 10 tests: larger secondary, negative coordinate, spanning screens,
equal intersection tie-break, independent width/height clamping, and
regression guards for existing behavior.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-01-REVIEW-FIX-04
Finding: S01-004
Status: DONE
Commit: <hash>
Tests Added: 10
Tests Total: 113
Tests Passed: 113
Tests Failed: 0
Build: Release 0 errors 0 warnings
Files Modified:
  - src/Anhei4Map.Core/Services/WindowBoundsNormalizer.cs
  - tests/Anhei4Map.Tests/WindowBoundsNormalizerTests.cs
Files NOT Modified (verified): <列出禁止路径确认未触及>
Limitations: <如有>
```
