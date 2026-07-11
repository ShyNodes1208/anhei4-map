# 当前任务

## 任务编号
STAGE-01-TASK-02

## 任务名称
AppSettings 默认值和设置校验

## 任务类型
BEHAVIOR

## 状态
READY

## 所属阶段
STAGE-01-FOUNDATION

## 前置条件
STAGE-01-TASK-01 完成——solution 和四个项目已创建，`dotnet build` 和 `dotnet test` 通过。

## 任务目标
在 `Anhei4Map.Core` 中定义 `AppSettings` record 及其关联类型，包含合理默认值和输入校验逻辑。全部行为通过 xUnit 测试验证。

## 允许修改
- Create: `src/Anhei4Map.Core/Models/AppSettings.cs`
- Create: `tests/Anhei4Map.Tests/AppSettingsTests.cs`

## 禁止修改
- 不得修改 `docs/` 下任何文件
- 不得修改 `src/Anhei4Map.App/`
- 不得修改 `src/Anhei4Map.Infrastructure/`
- 不得引入 JSON 序列化（System.Text.Json）
- 不得实现文件读写
- 不得添加 NuGet 包
- 不得引用 WPF/Win32/WebView2

## 必须新增的测试

```csharp
// tests/Anhei4Map.Tests/AppSettingsTests.cs

[Fact] public void CreateDefaults_HasEightHotkeyBindings() { }
[Fact] public void CreateDefaults_PlacementIs640x360() { }
[Fact] public void CreateDefaults_OpacityIs0_9() { }
[Fact] public void CreateDefaults_ZoomLevelIs1_0() { }
[Fact] public void CreateDefaults_InitialStateIsLocked() { }
[Fact] public void Validate_RejectsWidthBelow200() { }
[Fact] public void Validate_RejectsHeightBelow150() { }
[Fact] public void Validate_RejectsOpacityBelow0_1() { }
[Fact] public void Validate_RejectsOpacityAbove1_0() { }
[Fact] public void Validate_RejectsZoomBelow0_25() { }
[Fact] public void Validate_RejectsZoomAbove5_0() { }
[Fact] public void Validate_AcceptsValidSettings() { }
```

共 12 个测试。每个测试必须验证具体行为。

## RED 预期
编译错误：`AppSettings`、`HotkeyBinding`、`WindowPlacement`、`HotkeyCommand` 类型不存在。

## GREEN 最小实现

需定义以下类型（全部在 `src/Anhei4Map.Core/Models/AppSettings.cs` 一个文件中）：

- `HotkeyCommand` enum：Unknown, ToggleLock, ToggleHide, ZoomIn, ZoomOut, OpacityUp, OpacityDown, ResetPosition, Refresh
- `HotkeyBinding` record：Id(int), Modifiers(uint), Key(uint), Command(HotkeyCommand)
- `WindowPlacement` record：Left(int), Top(int), Width(int), Height(int), Opacity(double)
- `AppSettings` record：Hotkeys(List<HotkeyBinding>), Placement(WindowPlacement), ZoomLevel(double)
- `AppSettings.CreateDefaults()`：8 个 Ctrl+Shift 快捷键、640×360 默认窗口、不透明度 0.9、缩放 1.0
- `AppSettings.Validate()`：Width≥200, Height≥150, Opacity [0.1,1.0], Zoom [0.25,5.0]，返回 bool

## 定向测试命令
```powershell
dotnet test anhei4-map.sln -c Release --no-build --filter "FullyQualifiedName~AppSettingsTests"
```

## 完整测试命令
```powershell
dotnet test anhei4-map.sln -c Release --no-build
```

## 完成标准
1. 12 个测试全部通过
2. `dotnet build -c Release` 零错误
3. Core 层不引用 WPF/Win32/JSON
4. `Validate()` 拒绝所有非法值
5. `CreateDefaults()` 返回合理默认配置
6. `WindowPlacement.Left = -1` 表示启动时自动计算

## Git 提交信息
```
feat: add AppSettings with defaults and validation (12 tests)
```

## 完成后报告格式
```
STAGE-01-TASK-02 完成报告
- 状态: DONE
- 创建文件: src/Anhei4Map.Core/Models/AppSettings.cs, tests/Anhei4Map.Tests/AppSettingsTests.cs
- 测试结果: [通过数]/12
- dotnet build -c Release: [通过/失败]
- Git commit: [hash]
- 已知限制: [如有]
```
