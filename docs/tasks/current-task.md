# 当前任务

## 阶段
STAGE-02-APP-SHELL

## 任务编号
STAGE-02-TASK-01

## 类型
TDD

## 状态
READY

## 组件
WindowStateMachine

## 下一执行者
Cursor

---

## 设计依据

冻结设计文档中关于状态机的全部规定：

- [01-product-design.md:148-166](docs/design/01-product-design.md): 三状态定义、合法转换、各状态语义
- [02-architecture.md:87-123](docs/design/02-architecture.md): 状态模型、转换表、WS_EX 生命周期
- [03-test-strategy.md:24-42](docs/design/03-test-strategy.md): T1.1–T1.12 测试用例清单

以下规格取自上述文档，Cursor 不得自行发挥。

---

## 状态类型

`Anhei4Map.Core.Models.WindowState` 已存在：

```csharp
public enum WindowState
{
    Hidden,
    Locked,
    Edit
}
```

直接复用，不创建新类型。

---

## 事件类型

创建 `src/Anhei4Map.Core/State/Trigger.cs`：

```csharp
namespace Anhei4Map.Core.State;

public enum Trigger
{
    ToggleHide,
    ToggleLock
}
```

---

## 状态机 API

创建 `src/Anhei4Map.Core/State/WindowStateMachine.cs`：

```csharp
namespace Anhei4Map.Core.State;

public class WindowStateMachine
{
    // 初始状态 = Locked
    public WindowState CurrentState { get; }

    // 尝试执行转换。返回 true = 转换成功，false = 非法转换（状态不变）。
    public bool TryTransition(Trigger trigger);

    // 只读属性——不改变状态
    public bool CanEnterEdit { get; }         // true only from Locked
    public bool ShouldApplyTransparent { get; } // true for Locked+Hidden, false for Edit
    public bool ShouldRenderWebView { get; }    // false for Hidden, true for Locked+Edit
    public bool ShouldShowEditOverlay { get; }  // true only for Edit
    public bool ShouldRunTopmostTimer { get; }  // true only for Locked
}
```

---

## 转换逻辑

`TryTransition(Trigger)` 必须按以下规则执行：

| CurrentState | Trigger    | NewState | Return |
|-------------|------------|----------|--------|
| Hidden      | ToggleHide | Locked   | true   |
| Locked      | ToggleHide | Hidden   | true   |
| Locked      | ToggleLock | Edit     | true   |
| Edit        | ToggleLock | Locked   | true   |
| Hidden      | ToggleLock | Hidden   | false  |
| Edit        | ToggleHide | Edit     | false  |

---

## 幂等规则

非法转换（返回 false）时：状态不变、不抛异常、不记录错误。

不存在从同一状态出发的同一事件同时合法又非法的歧义。上表覆盖全部 6 种组合。

---

## 属性真值表

| State  | CanEnterEdit | ShouldApplyTransparent | ShouldRenderWebView | ShouldShowEditOverlay | ShouldRunTopmostTimer |
|--------|-------------|------------------------|---------------------|-----------------------|-----------------------|
| Hidden | false       | true                   | false               | false                 | false                 |
| Locked | true        | true                   | true                | false                 | true                  |
| Edit   | false       | false                  | true                | true                  | false                 |

---

## 技术边界

- 纯逻辑类，无 WPF / Win32 / WebView2 引用
- 不读写文件或配置
- 不写日志
- 不注册热键
- 不直接控制窗口
- 不保存历史状态
- 由于单实例、单 UI 线程假设，本阶段不要求线程安全
- 构造函数不接受参数，不接受 null

---

## 允许修改的精确路径

- `src/Anhei4Map.Core/State/Trigger.cs`（新建）
- `src/Anhei4Map.Core/State/WindowStateMachine.cs`（新建）
- `tests/Anhei4Map.Tests/WindowStateMachineTests.cs`（新建）

## 禁止修改范围

- `src/Anhei4Map.Core/Models/**`（含 WindowState 枚举——复用，不修改）
- `src/Anhei4Map.Core/Services/**`
- `src/Anhei4Map.Core/Logging/**`
- `src/Anhei4Map.App/**`
- `src/Anhei4Map.Infrastructure/**`
- `tests/Anhei4Map.Tests/*`（除新建文件外）
- `docs/design/**`
- `*.csproj`
- `*.sln`

---

## RED 阶段 — 先写失败测试

创建 `tests/Anhei4Map.Tests/WindowStateMachineTests.cs`。

### 测试 1: `InitialState_IsLocked`

```csharp
[Fact]
public void InitialState_IsLocked()
{
    var sm = new WindowStateMachine();
    Assert.Equal(WindowState.Locked, sm.CurrentState);
}
```

### 测试 2: `Locked_ToggleHide_TransitionsToHidden`

### 测试 3: `Hidden_ToggleHide_TransitionsToLocked`

### 测试 4: `Locked_ToggleLock_TransitionsToEdit`

### 测试 5: `Edit_ToggleLock_TransitionsToLocked`

### 测试 6: `Hidden_ToggleLock_StaysHidden`

非法转换 → 返回 false，状态保持 Hidden。

### 测试 7: `Edit_ToggleHide_StaysEdit`

非法转换 → 返回 false，状态保持 Edit。

### 测试 8–12: 属性验证

| # | 测试名 | 验证内容 |
|---|--------|----------|
| T1.8 | `Locked_CanEnterEdit` | Locked 状态下 CanEnterEdit = true, Hidden 和 Edit 下 = false |
| T1.9 | `ShouldApplyTransparent_ByState` | Hidden/Locked = true, Edit = false |
| T1.10 | `ShouldRenderWebView_ByState` | Locked/Edit = true, Hidden = false |
| T1.11 | `ShouldShowEditOverlay_ByState` | Edit = true, Hidden/Locked = false |
| T1.12 | `ShouldRunTopmostTimer_ByState` | Locked = true, Hidden/Edit = false |

属性测试可使用 `[Theory]` + `[InlineData]` 参数化以减少重复。

### 测试 13: `MultipleTransitions_ProduceDeterministicResults`

连续执行 Locked → ToggleLock → Edit → ToggleLock → Locked → ToggleHide → Hidden → ToggleHide → Locked，验证每步状态和返回值。

### 测试 14: `QueryProperties_DoNotChangeState`

多次调用 CurrentState 和属性查询不改变状态。

### RED 预期

```
dotnet test --filter "FullyQualifiedName~WindowStateMachineTests" -c Release
```

预期 14 个测试全部 FAIL — `WindowStateMachine` 类尚不存在 (CS0246)。

---

## GREEN 阶段 — 最小实现

1. 创建 `src/Anhei4Map.Core/State/Trigger.cs`
2. 创建 `src/Anhei4Map.Core/State/WindowStateMachine.cs`
3. 实现仅满足上述转换表和属性真值表
4. 不实现 TASK-02 (HotkeyDispatcher)
5. 不实现窗口操作、热键注册、WebView2
6. 不新增第三方依赖

---

## 定向测试命令

```powershell
dotnet test tests/Anhei4Map.Tests/ --filter "FullyQualifiedName~WindowStateMachineTests" -c Release
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

- [ ] `Trigger.cs` 和 `WindowStateMachine.cs` 已创建
- [ ] 14 个测试已创建
- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] 定向测试 FAIL (RED — CS0246)
- [ ] 最小实现后定向测试 PASS (GREEN — 14/14)
- [ ] 完整测试全部通过 (136: 122原有 + 14新增)
- [ ] `git diff --check` clean
- [ ] 只修改了允许的 3 个文件
- [ ] Cursor 只提交，不 push

---

## Git 提交信息

```
feat: implement window state machine

Add WindowStateMachine with three states (Hidden/Locked/Edit) and two
triggers (ToggleHide/ToggleLock). Four legal transitions, two illegal
transitions that return false without throwing. Five query properties
for downstream WPF integration. 14 xUnit tests covering all state-event
combinations and property truth table.
```

---

## Cursor 最终报告格式

```
TASK_COMPLETE

Task: STAGE-02-TASK-01
Component: WindowStateMachine
Type: TDD
Status: DONE
Commit: <hash>
Tests Added: 14
Tests Total: 136
Tests Passed: 136
Tests Failed: 0
Build: Release 0 errors 0 warnings
Files Created:
  - src/Anhei4Map.Core/State/Trigger.cs
  - src/Anhei4Map.Core/State/WindowStateMachine.cs
  - tests/Anhei4Map.Tests/WindowStateMachineTests.cs
Files NOT Modified (verified): <列出禁止路径>
Limitations: <如有>
```
