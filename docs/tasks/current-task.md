# 当前任务

## 任务编号
STAGE-01-TASK-05

## 任务名称
窗口边界规范化纯逻辑

## 任务类型
BEHAVIOR

## 状态
READY

## 所属阶段
STAGE-01-FOUNDATION

## 前置条件
STAGE-01-TASK-04 完成——`JsonSettingsStore` 支持原子保存和损坏恢复，26 个测试通过。`AppSettings` 中 `WindowPlacement` 和默认值（640×360）已定义。

## 任务目标
实现与 WPF、Win32、真实显示器 API 解耦的窗口边界规范化纯逻辑。输入为保存的 `WindowPlacement` 和可用显示器工作区列表，输出为修正后的 `WindowPlacement`。

## 允许修改
- Create: `src/Anhei4Map.Core/Services/WindowBoundsNormalizer.cs`
- Create: `tests/Anhei4Map.Tests/WindowBoundsNormalizerTests.cs`

## 禁止修改
- 不得修改 `src/Anhei4Map.App/`
- 不得修改 `src/Anhei4Map.Infrastructure/`
- 不得修改 `docs/design/`
- 不得引用 WPF、Win32、`System.Windows.Forms`、`System.Drawing`
- 不得修改 `JsonSettingsStore` 或 `AppSettings`

## 设计常量（以架构文档为准）

| 常量 | 值 | 来源 |
|------|----|------|
| 默认宽度 | 640 | `AppSettings.CreateDefaults()` |
| 默认高度 | 360 | `AppSettings.CreateDefaults()` |
| 最小宽度 | 200 | `AppSettings.Validate()` |
| 最小高度 | 150 | `AppSettings.Validate()` |
| 可见阈值 | 20% | `02-architecture.md` |
| 边距 | 16px | `02-architecture.md` |

## RED 测试清单

```csharp
// tests/Anhei4Map.Tests/WindowBoundsNormalizerTests.cs

[Fact] public void ValidBounds_Unchanged() { }
[Fact] public void FullyOffscreenRight_Resets() { }
[Fact] public void FullyOffscreenLeft_Resets() { }
[Fact] public void FullyOffscreenAbove_Resets() { }
[Fact] public void FullyOffscreenBelow_Resets() { }
[Fact] public void PartiallyVisibleAboveThreshold_Unchanged() { }
[Fact] public void BelowVisibilityThreshold_Resets() { }
[Fact] public void ValidOnNegativeOriginMonitor_Unchanged() { }
[Fact] public void WidthBelowMin_ClampedToDefault() { }
[Fact] public void HeightBelowMin_ClampedToDefault() { }
[Fact] public void WidthExceedsWorkArea_Clamped() { }
[Fact] public void HeightExceedsWorkArea_Clamped() { }
[Fact] public void EmptyWorkAreaList_Fallback() { }
[Fact] public void ResetPositionsIncludeEdgePadding() { }
```

共 14 个测试。

## 有效 RED 失败标准
`dotnet test --filter "FullyQualifiedName~WindowBoundsNormalizerTests"` 编译失败——`WindowBoundsNormalizer` 类型不存在。

## GREEN 最小实现

`WindowBoundsNormalizer.Normalize(WindowPlacement saved, WorkArea[] workAreas)` 返回 `WindowPlacement`：

1. Clamp Width/Height 到 [min, max(workArea size)]
2. 遍历 workAreas，检查交集面积 >= 20% 窗口面积
3. 如无足够交集 → 重置到 `workAreas[0]` 右上角（Right - 640 - 16, Top + 16）
4. 空 workAreas → (0, 16, 640, 360)
5. 纯静态方法，无副作用

`WorkArea` record: `(int Left, int Top, int Width, int Height)` — 定义在 `WindowBoundsNormalizer.cs` 同文件中。

## 定向测试命令
```powershell
dotnet test anhei4-map.sln -c Release --filter "FullyQualifiedName~WindowBoundsNormalizerTests"
```

## 完整测试命令
```powershell
dotnet test anhei4-map.sln -c Release --no-build
```

## 完成标准
1. 14 个定向测试全部通过
2. 加已有 26 个 = 40 个测试全部通过
3. `dotnet build -c Release` 零错误
4. Core 层零 WPF/Win32 引用

## Git 提交信息
```
feat: add window bounds normalization (14 tests)
```

## 完成后报告格式
```
STAGE-01-TASK-05 完成报告
- 状态: DONE
- 创建文件: src/Anhei4Map.Core/Services/WindowBoundsNormalizer.cs, tests/Anhei4Map.Tests/WindowBoundsNormalizerTests.cs
- 定向测试: [通过数]/14
- 完整测试: [通过数]/40
- dotnet build -c Release: [通过/失败]
- Git commit: [hash]
```
