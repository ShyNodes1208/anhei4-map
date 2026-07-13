# 当前任务

## 阶段
STAGE-02-APP-SHELL

## 任务编号
STAGE-02-TASK-02

## 类型
TDD

## 状态
READY

## 组件
HotkeyDispatcher

## 下一执行者
Cursor

---

## 设计依据

冻结设计文档：

- [01-product-design.md:191-200](docs/design/01-product-design.md): 8 个默认快捷键 → 8 个命令
- [02-architecture.md:252-270](docs/design/02-architecture.md): 热键注册流程、冲突处理
- [03-test-strategy.md:44-57](docs/design/03-test-strategy.md): T2.1–T2.6 测试用例

---

## 已有类型

`AppSettings.cs` 中已定义：

```csharp
public enum HotkeyCommand
{
    Unknown,
    ToggleLock,
    ToggleHide,
    ZoomIn,
    ZoomOut,
    OpacityUp,
    OpacityDown,
    ResetPosition,
    Refresh
}

public record HotkeyBinding(int Id, uint Modifiers, uint Key, HotkeyCommand Command);
```

---

## 组件职责

`HotkeyDispatcher` 是**纯映射服务**：给定绑定列表和热键 ID，返回对应的 `HotkeyCommand`。

- 不调用 `WindowStateMachine`
- 不调用 `RegisterHotKey` 或任何 Win32 API
- 不引用 WPF、WebView2
- 不读写配置文件
- 不记录日志
- 不保存窗口状态

与 `WindowStateMachine` 的集成属于后续 WPF 层任务（TASK-05+），不在本任务范围内。

---

## API

创建 `src/Anhei4Map.Core/Services/HotkeyDispatcher.cs`：

```csharp
namespace Anhei4Map.Core.Services;

public class HotkeyDispatcher
{
    // 绑定列表为 null 时视为空列表
    public HotkeyDispatcher(IEnumerable<HotkeyBinding>? bindings);

    // 根据热键 ID 查找命令。找不到 → HotkeyCommand.Unknown
    // id 允许任意 int 值
    public HotkeyCommand Dispatch(int id);
}
```

---

## 行为规则

### 1. 已知 ID → 对应命令

- 绑定列表中包含该 ID → 返回对应的 `HotkeyCommand`
- 如 ID=1 绑定 `ToggleLock` → `Dispatch(1)` 返回 `HotkeyCommand.ToggleLock`

### 2. 未知 ID → Unknown

- 绑定列表中不包含该 ID → 返回 `HotkeyCommand.Unknown`
- 不抛异常

### 3. 空绑定列表 → 全部 Unknown

- `bindings` 为 `null` 或空 `IEnumerable` → 所有 `Dispatch(id)` 返回 `Unknown`

### 4. 重复 ID → 最后绑定生效

- 绑定列表中有多个相同 ID → 列表中**最后**出现的绑定生效
- 例如：`[Binding(1, ..., ToggleHide), Binding(1, ..., ToggleLock)]` → `Dispatch(1)` 返回 `ToggleLock`

### 5. 不验证 modifiers/key

- 不检查 `Modifiers` 或 `Key` 字段
- 仅使用 `Id` 和 `Command`

### 6. 确定性和纯逻辑

- 相同绑定列表和相同 ID → 始终返回相同结果
- 无随机数、无时间依赖、无状态保存

---

## 允许修改的精确路径

- `src/Anhei4Map.Core/Services/HotkeyDispatcher.cs`（新建）
- `tests/Anhei4Map.Tests/HotkeyDispatcherTests.cs`（新建）

## 禁止修改范围

- `src/Anhei4Map.Core/State/**`
- `src/Anhei4Map.Core/Models/**`
- `src/Anhei4Map.Core/Logging/**`
- `src/Anhei4Map.App/**`
- `src/Anhei4Map.Infrastructure/**`
- `tests/Anhei4Map.Tests/*`（除新建文件外）
- `docs/design/**`
- `*.csproj`
- `*.sln`

---

## RED 阶段 — 先写失败测试

创建 `tests/Anhei4Map.Tests/HotkeyDispatcherTests.cs`。

### 测试覆盖

| # | 测试名 | 场景 | 断言 |
|---|--------|------|------|
| T2.1 | `Dispatch_KnownId_ReturnsCorrectCommand` | ID=1→ToggleLock, ID=2→ToggleHide 等 | 对应命令 |
| T2.2 | `Dispatch_UnknownId_ReturnsUnknown` | 99 不在任何绑定中 | Unknown |
| T2.3 | `EmptyBindings_ReturnsUnknown` | 空列表 + 任意 ID | Unknown |
| T2.4 | `NullBindings_ReturnsUnknown` | null 绑定 + 任意 ID | Unknown |
| T2.5 | `DuplicateIds_LastWins` | 两个绑定 ID=1，命令不同 | 最后一个的 Command |
| T2.6 | `Dispatch_IsDeterministic` | 相同输入两次调用 | 相同输出 |
| T2.7 | `Dispatch_AllEightDefaultCommands` | 8 个 ID → 8 个不同命令 | 全部正确 |

T2.1 使用 `[Theory]` + `[InlineData]` 覆盖全部 8 个默认绑定。

### RED 预期

```
dotnet test --filter "FullyQualifiedName~HotkeyDispatcherTests" -c Release
```

预期全部 FAIL — `HotkeyDispatcher` 类尚不存在 (CS0246)。

---

## GREEN 阶段 — 最小实现

1. 创建 `src/Anhei4Map.Core/Services/HotkeyDispatcher.cs`
2. 构造函数存储绑定列表（防御性拷贝，null → 空列表）
3. `Dispatch` 从后往前查找（或使用字典/Lookup 保留 last-wins）
4. 仅满足上述行为规则
5. 不实现 Win32/WPF/WebView2/TASK-03

---

## 定向测试命令

```powershell
dotnet test tests/Anhei4Map.Tests/ --filter "FullyQualifiedName~HotkeyDispatcherTests" -c Release
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

- [ ] `HotkeyDispatcher.cs` 已创建
- [ ] 7 个测试已创建
- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] 定向测试 FAIL (RED — CS0246)
- [ ] 定向测试 PASS (GREEN — 包含 Theory 展开)
- [ ] 完整测试全部通过 (prev + new)
- [ ] `git diff --check` clean
- [ ] Cursor 只提交，不 push

---

## Git 提交信息

```
feat: implement hotkey dispatcher

Add HotkeyDispatcher that resolves hotkey IDs to HotkeyCommand using
a binding list. Unknown IDs return Unknown. Duplicate IDs use last-wins
semantics. Null bindings treated as empty. Pure logic — no Win32 or WPF.
```

---

## Cursor 最终报告格式

```
TASK_COMPLETE

Task: STAGE-02-TASK-02
Component: HotkeyDispatcher
Type: TDD
Status: DONE
Commit: <hash>
Tests Added: <count>
Tests Total: <count>
Tests Passed: <count>
Tests Failed: 0
Build: Release 0 errors 0 warnings
Files Created:
  - src/Anhei4Map.Core/Services/HotkeyDispatcher.cs
  - tests/Anhei4Map.Tests/HotkeyDispatcherTests.cs
Files NOT Modified (verified): <列出>
Limitations: <如有>
```
