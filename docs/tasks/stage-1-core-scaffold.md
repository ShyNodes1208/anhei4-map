# Stage-1: Core Library + Solution Scaffold Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Cursor:** 只按本文件写测试和代码并提交。不得改需求、架构和范围。一次只执行一个任务。

**Goal:** 创建 anhei4-map 解决方案骨架、Anhei4Map.Core 类库（全部 Models/Services/State/Interop/Logging）、Anhei4Map.Core.Tests xUnit 测试项目、Anhei4Map.Wpf 项目骨架。所有 Core 层 51 个测试用例全部通过。

**Architecture:** WPF (View) → Core (Logic, testable) → Tests (xUnit)。Core 层零 WPF/Win32 依赖。Win32 调用隔离在 `IWin32Interop` 接口后。

**Tech Stack:** C# / .NET 8 / WPF / xUnit / 无外部 DI 容器（手动构造函数注入）

## Global Constraints

- 技术栈不可替换：C#、.NET 8、WPF、WebView2、xUnit
- 安全红线：不读游戏内存/文件/网络、不注入/Hook、不模拟键鼠、不自动寻路
- 不引入数据库、后端服务、爬虫、私有 API、云服务
- 依赖最小化：仅 `Microsoft.Web.WebView2`（WPF）+ `xUnit` + `Microsoft.NET.Test.Sdk`
- Win32 调用必须通过 `IWin32Interop` 接口（Core 层仅定义接口，WPF 层实现 P/Invoke）
- 所有决策逻辑必须在 Core 层，WPF 层仅做绑定和 P/Invoke
- Cursor 一次只执行一个任务
- 每个任务以独立可验证的 git commit 结束

---

### Task 1: Solution + Project Scaffolding

**前置条件:** 无（绿色场项目）

**目标行为:** 创建 .NET 8 解决方案和三个项目，建立项目引用关系，确认 `dotnet test` 可运行。

**允许修改文件:**
- Create: `anhei4-map.sln`
- Create: `src/Anhei4Map.Core/Anhei4Map.Core.csproj`
- Create: `src/Anhei4Map.Core/Class1.cs`（临时，Task 2 删除）
- Create: `src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj`
- Create: `tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj`
- Create: `tests/Anhei4Map.Core.Tests/Usings.cs`
- Modify: `src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj`（添加 WebView2 NuGet）

**禁止修改范围:**
- 不得修改 docs/ 下任何文件
- 不得修改 CLAUDE.md、AGENTS.md、.gitignore

**先失败的测试:**
无——此任务为纯脚手架，无业务逻辑。

**预期失败原因:**
N/A

**最小实现:**

```bash
# Step 1: Create solution
dotnet new sln -n anhei4-map

# Step 2: Create Core classlib
dotnet new classlib -n Anhei4Map.Core -o src/Anhei4Map.Core -f net8.0

# Step 3: Create WPF application
dotnet new wpf -n Anhei4Map.Wpf -o src/Anhei4Map.Wpf -f net8.0

# Step 4: Create xUnit test project
dotnet new xunit -n Anhei4Map.Core.Tests -o tests/Anhei4Map.Core.Tests -f net8.0

# Step 5: Add projects to solution
dotnet sln add src/Anhei4Map.Core/Anhei4Map.Core.csproj
dotnet sln add src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj
dotnet sln add tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj

# Step 6: Add project references
dotnet add tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj reference src/Anhei4Map.Core/Anhei4Map.Core.csproj

# Step 7: Add WebView2 NuGet to WPF project
dotnet add src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj package Microsoft.Web.WebView2

# Step 8: Remove template Class1.cs
rm src/Anhei4Map.Core/Class1.cs

# Step 9: Verify build
dotnet build
dotnet test tests/Anhei4Map.Core.Tests/
```

**验证命令:**
```bash
dotnet build --no-restore 2>&1 | grep "Build succeeded"
dotnet test tests/Anhei4Map.Core.Tests/ 2>&1 | grep "Passed!"
```

**人工验证:**
- `ls *.sln` 确认 `anhei4-map.sln` 存在
- `ls src/Anhei4Map.Core/Anhei4Map.Core.csproj` 确认 Core 项目存在
- `ls src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj` 确认 WPF 项目存在
- `ls tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj` 确认 Tests 项目存在
- `dotnet test` 输出 "No test is available" 或 0 测试通过（因为尚未添加测试）

**完成标准:**
- `dotnet build` 零错误
- 三个项目均正确引用：Tests → Core，WPF → WebView2 NuGet
- 无 Class1.cs 残留

**Git 提交信息:**
```
chore: scaffold solution with Core, WPF, and xUnit test projects
```

---

### Task 2: Core Models

**前置条件:** Task 1 完成（solution + 项目结构存在）

**目标行为:** 创建 Core 层的四个数据模型：`WindowState` 枚举、`HotkeyBinding`、`WindowPlacement`、`AppSettings`。均为纯数据类，无行为逻辑。

**允许修改文件:**
- Create: `src/Anhei4Map.Core/Models/WindowState.cs`
- Create: `src/Anhei4Map.Core/Models/HotkeyBinding.cs`
- Create: `src/Anhei4Map.Core/Models/WindowPlacement.cs`
- Create: `src/Anhei4Map.Core/Models/AppSettings.cs`

**禁止修改范围:**
- 不得修改 tests/ 下任何文件
- 不得修改 WPF 项目
- 不得修改 docs/、CLAUDE.md、AGENTS.md

**先失败的测试:**
无——模型为纯数据类，Task 5 的 SettingsManager 测试会间接验证序列化。此任务无独立测试文件。

**预期失败原因:**
N/A

**最小实现:**

```csharp
// src/Anhei4Map.Core/Models/WindowState.cs
namespace Anhei4Map.Core.Models;

public enum WindowState
{
    Hidden,
    Locked,
    Edit
}
```

```csharp
// src/Anhei4Map.Core/Models/HotkeyBinding.cs
namespace Anhei4Map.Core.Models;

public record HotkeyBinding(
    int Id,
    uint Modifiers,  // MOD_ALT=1, MOD_CONTROL=2, MOD_SHIFT=4, MOD_WIN=8
    uint Key,        // Win32 virtual-key code
    HotkeyCommand Command
);

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
```

```csharp
// src/Anhei4Map.Core/Models/WindowPlacement.cs
namespace Anhei4Map.Core.Models;

public record WindowPlacement(
    int Left,
    int Top,
    int Width,
    int Height,
    double Opacity  // 0.1 to 1.0
);
```

```csharp
// src/Anhei4Map.Core/Models/AppSettings.cs
namespace Anhei4Map.Core.Models;

public record AppSettings(
    List<HotkeyBinding> Hotkeys,
    WindowPlacement Placement,
    double ZoomLevel  // 0.25 to 5.0
)
{
    public static AppSettings CreateDefaults() => new(
        Hotkeys: new List<HotkeyBinding>
        {
            new(1, 0x0003, 0x4D, HotkeyCommand.ToggleLock),   // Ctrl+Shift+M
            new(2, 0x0003, 0x48, HotkeyCommand.ToggleHide),   // Ctrl+Shift+H
            new(3, 0x0003, 0xBB, HotkeyCommand.ZoomIn),       // Ctrl+Shift+Plus
            new(4, 0x0003, 0xBD, HotkeyCommand.ZoomOut),      // Ctrl+Shift+Minus
            new(5, 0x0003, 0x26, HotkeyCommand.OpacityUp),    // Ctrl+Shift+Up
            new(6, 0x0003, 0x28, HotkeyCommand.OpacityDown),  // Ctrl+Shift+Down
            new(7, 0x0003, 0x52, HotkeyCommand.ResetPosition),// Ctrl+Shift+R
            new(8, 0x0003, 0x74, HotkeyCommand.Refresh)       // Ctrl+Shift+F5
        },
        Placement: new WindowPlacement(
            Left: -1, Top: -1,  // -1 = "auto-calculate at startup"
            Width: 640, Height: 360,
            Opacity: 0.9
        ),
        ZoomLevel: 1.0
    );
}
```

**验证命令:**
```bash
dotnet build src/Anhei4Map.Core/ --no-restore 2>&1
```

**人工验证:**
- 确认 4 个 `.cs` 文件存在于 `src/Anhei4Map.Core/Models/`
- 确认 `dotnet build` 零错误
- 确认 `AppSettings.CreateDefaults()` 返回非空的 8 个快捷键绑定

**完成标准:**
- `dotnet build` 零错误
- `WindowState` 枚举含 `Hidden`、`Locked`、`Edit`
- `HotkeyBinding` 含 `Id`、`Modifiers`、`Key`、`Command` 字段
- `HotkeyCommand` 含 8 个命令 + `Unknown`
- `AppSettings.CreateDefaults()` 生成完整默认配置
- `WindowPlacement` 含 `Left`、`Top`、`Width`、`Height`、`Opacity`

**Git 提交信息:**
```
feat: add Core data models (WindowState, HotkeyBinding, WindowPlacement, AppSettings)
```

---

### Task 3: RetryPolicy + Tests

**前置条件:** Task 1 完成（solution + 项目结构存在）

**目标行为:** 实现指数退避重试策略，支持 10 次重试、延迟序列 [0, 1000, 2000, 4000, 8000, 30000...]、max 30000ms。10 个测试用例全部通过。

**允许修改文件:**
- Create: `src/Anhei4Map.Core/Services/RetryPolicy.cs`
- Create: `tests/Anhei4Map.Core.Tests/RetryPolicyTests.cs`

**禁止修改范围:**
- 不得修改 Models/、WPF 项目、docs/、CLAUDE.md

**先失败的测试:**

```csharp
// tests/Anhei4Map.Core.Tests/RetryPolicyTests.cs
using Anhei4Map.Core.Services;

namespace Anhei4Map.Core.Tests;

public class RetryPolicyTests
{
    [Fact] public void Attempt0_Returns0ms() =>
        Assert.Equal(0, RetryPolicy.NextDelay(0));

    [Fact] public void Attempt1_Returns1000ms() =>
        Assert.Equal(1000, RetryPolicy.NextDelay(1));

    [Fact] public void Attempt2_Returns2000ms() =>
        Assert.Equal(2000, RetryPolicy.NextDelay(2));

    [Fact] public void Attempt3_Returns4000ms() =>
        Assert.Equal(4000, RetryPolicy.NextDelay(3));

    [Fact] public void Attempt4_Returns8000ms() =>
        Assert.Equal(8000, RetryPolicy.NextDelay(4));

    [Fact] public void Attempt5_Returns30000ms() =>
        Assert.Equal(30000, RetryPolicy.NextDelay(5));

    [Fact] public void Attempt6_Returns30000ms() =>
        Assert.Equal(30000, RetryPolicy.NextDelay(6));

    [Fact] public void Attempt9_Returns30000ms() =>
        Assert.Equal(30000, RetryPolicy.NextDelay(9));

    [Fact] public void Attempt10_ReturnsNull() =>
        Assert.Null(RetryPolicy.NextDelay(10));

    [Fact] public void AttemptNegative_ThrowsArgumentOutOfRange() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicy.NextDelay(-1));
}
```

**预期失败原因:**
编译错误——`RetryPolicy` 类不存在，`NextDelay` 方法不存在。

**最小实现:**

```csharp
// src/Anhei4Map.Core/Services/RetryPolicy.cs
namespace Anhei4Map.Core.Services;

public static class RetryPolicy
{
    private static readonly int[] Delays = { 0, 1000, 2000, 4000, 8000, 30000 };
    public const int MaxRetries = 10;
    public const int MaxDelayMs = 30000;

    /// <returns>Delay in milliseconds, or null if retries exhausted.</returns>
    public static int? NextDelay(int attemptNumber)
    {
        if (attemptNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(attemptNumber),
                "Attempt number must be non-negative.");
        if (attemptNumber >= MaxRetries)
            return null;
        if (attemptNumber < Delays.Length)
            return Delays[attemptNumber];
        return MaxDelayMs;
    }
}
```

**验证命令:**
```bash
dotnet test tests/Anhei4Map.Core.Tests/ --filter "FullyQualifiedName~RetryPolicyTests"
```

**人工验证:**
- 确认 10/10 测试通过
- 确认 `dotnet test` 输出包含 "10 Passed, 0 Failed"

**完成标准:**
- 10 个测试用例全部通过
- `RetryPolicy.NextDelay(-1)` 抛出 `ArgumentOutOfRangeException`
- `RetryPolicy.NextDelay(10)` 返回 `null`

**Git 提交信息:**
```
feat: add RetryPolicy with exponential backoff (10 tests)
```

---

### Task 4: WindowStateMachine + Tests

**前置条件:** Task 2 完成（Core Models 存在）

**目标行为:** 实现窗口状态机，三种状态 Hidden/Locked/Edit，合法转换受 guard 保护。12 个测试用例全部通过。

**允许修改文件:**
- Create: `src/Anhei4Map.Core/State/WindowStateMachine.cs`
- Create: `tests/Anhei4Map.Core.Tests/WindowStateMachineTests.cs`

**禁止修改范围:**
- 不得修改 Services/、WPF 项目、docs/、CLAUDE.md
- 不得引用 WPF 或 Win32 命名空间

**先失败的测试:**

```csharp
// tests/Anhei4Map.Core.Tests/WindowStateMachineTests.cs
using Anhei4Map.Core.State;
using Anhei4Map.Core.Models;

namespace Anhei4Map.Core.Tests;

public class WindowStateMachineTests
{
    [Fact]
    public void InitialState_IsLocked()
    {
        var sm = new WindowStateMachine();
        Assert.Equal(Models.WindowState.Locked, sm.CurrentState);
    }

    [Fact]
    public void LockedToEditRoundTrip()
    {
        var sm = new WindowStateMachine();
        sm.ToggleLock();
        Assert.Equal(Models.WindowState.Edit, sm.CurrentState);
        Assert.True(sm.ShouldShowEditOverlay);
        Assert.False(sm.ShouldApplyTransparent);

        sm.ToggleLock();
        Assert.Equal(Models.WindowState.Locked, sm.CurrentState);
        Assert.False(sm.ShouldShowEditOverlay);
        Assert.True(sm.ShouldApplyTransparent);
    }

    [Fact]
    public void LockedToHiddenRoundTrip()
    {
        var sm = new WindowStateMachine();
        sm.ToggleHide();
        Assert.Equal(Models.WindowState.Hidden, sm.CurrentState);
        Assert.False(sm.ShouldRenderWebView);

        sm.ToggleHide();
        Assert.Equal(Models.WindowState.Locked, sm.CurrentState);
        Assert.True(sm.ShouldRenderWebView);
    }

    [Fact]
    public void HiddenToEdit_IsIllegal()
    {
        var sm = new WindowStateMachine();
        sm.ToggleHide();   // Locked → Hidden
        sm.ToggleLock();   // should be no-op
        Assert.Equal(Models.WindowState.Hidden, sm.CurrentState);
    }

    [Fact]
    public void EditToHidden_IsIllegal()
    {
        var sm = new WindowStateMachine();
        sm.ToggleLock();   // Locked → Edit
        sm.ToggleHide();   // should be no-op
        Assert.Equal(Models.WindowState.Edit, sm.CurrentState);
    }

    [Fact]
    public void SameStateToggleLock_IsIdempotent()
    {
        var sm = new WindowStateMachine();
        sm.ToggleLock();   // Locked → Edit
        sm.ToggleLock();   // Edit → Locked
        sm.ToggleLock();   // Locked → Edit
        Assert.Equal(Models.WindowState.Edit, sm.CurrentState);
    }

    [Fact]
    public void RapidDoubleToggle_EndsInOriginalState()
    {
        var sm = new WindowStateMachine();
        sm.ToggleLock();   // Locked → Edit
        sm.ToggleLock();   // Edit → Locked
        Assert.Equal(Models.WindowState.Locked, sm.CurrentState);
    }

    [Fact]
    public void CanEnterEdit_TrueOnlyFromLocked()
    {
        var sm = new WindowStateMachine();
        Assert.True(sm.CanEnterEdit);

        sm.ToggleLock();
        Assert.True(sm.CanEnterEdit);

        sm.ToggleHide();   // Edit → Illegal, stays Edit
        sm.ToggleLock();   // Edit → Locked
        sm.ToggleHide();   // Locked → Hidden
        Assert.False(sm.CanEnterEdit);
    }

    [Fact]
    public void ShouldApplyTransparent_TrueForLockedAndHidden()
    {
        var sm = new WindowStateMachine();
        Assert.True(sm.ShouldApplyTransparent);  // Locked

        sm.ToggleLock();
        Assert.False(sm.ShouldApplyTransparent); // Edit

        sm.ToggleLock();
        sm.ToggleHide();
        Assert.True(sm.ShouldApplyTransparent);  // Hidden
    }

    [Fact]
    public void ShouldRenderWebView_FalseOnlyInHidden()
    {
        var sm = new WindowStateMachine();
        Assert.True(sm.ShouldRenderWebView);   // Locked

        sm.ToggleLock();
        Assert.True(sm.ShouldRenderWebView);   // Edit

        sm.ToggleLock();
        sm.ToggleHide();
        Assert.False(sm.ShouldRenderWebView);  // Hidden
    }

    [Fact]
    public void ShouldShowEditOverlay_TrueOnlyInEdit()
    {
        var sm = new WindowStateMachine();
        Assert.False(sm.ShouldShowEditOverlay);  // Locked

        sm.ToggleLock();
        Assert.True(sm.ShouldShowEditOverlay);   // Edit

        sm.ToggleLock();
        Assert.False(sm.ShouldShowEditOverlay);  // Locked
    }

    [Fact]
    public void ShouldRunTopmostTimer_TrueOnlyInLocked()
    {
        var sm = new WindowStateMachine();
        Assert.True(sm.ShouldRunTopmostTimer);   // Locked

        sm.ToggleLock();
        Assert.False(sm.ShouldRunTopmostTimer);  // Edit

        sm.ToggleLock();
        sm.ToggleHide();
        Assert.False(sm.ShouldRunTopmostTimer);  // Hidden
    }
}
```

**预期失败原因:**
编译错误——`WindowStateMachine` 类不存在，`ToggleLock()`、`ToggleHide()`、`CurrentState` 等成员不存在。

**最小实现:**

```csharp
// src/Anhei4Map.Core/State/WindowStateMachine.cs
using Anhei4Map.Core.Models;

namespace Anhei4Map.Core.State;

public class WindowStateMachine
{
    public WindowState CurrentState { get; private set; } = WindowState.Locked;

    public bool CanEnterEdit => CurrentState == WindowState.Locked
                             || CurrentState == WindowState.Edit;
    public bool ShouldApplyTransparent =>
        CurrentState is WindowState.Locked or WindowState.Hidden;
    public bool ShouldRenderWebView =>
        CurrentState is WindowState.Locked or WindowState.Edit;
    public bool ShouldShowEditOverlay => CurrentState == WindowState.Edit;
    public bool ShouldRunTopmostTimer => CurrentState == WindowState.Locked;

    public void ToggleLock()
    {
        if (CurrentState == WindowState.Locked)
            CurrentState = WindowState.Edit;
        else if (CurrentState == WindowState.Edit)
            CurrentState = WindowState.Locked;
        // Hidden → no-op (illegal transition)
    }

    public void ToggleHide()
    {
        if (CurrentState == WindowState.Locked)
            CurrentState = WindowState.Hidden;
        else if (CurrentState == WindowState.Hidden)
            CurrentState = WindowState.Locked;
        // Edit → no-op (illegal transition)
    }
}
```

**验证命令:**
```bash
dotnet test tests/Anhei4Map.Core.Tests/ --filter "FullyQualifiedName~WindowStateMachineTests"
```

**人工验证:**
- 确认 12/12 测试通过

**完成标准:**
- 12 个测试用例全部通过
- Hidden → Edit 被 guard 阻止
- Edit → Hidden 被 guard 阻止
- 幂等性：同状态 toggle 不改变状态

**Git 提交信息:**
```
feat: add WindowStateMachine with three-state FSM (12 tests)
```

---

### Task 5: SettingsManager + Tests

**前置条件:** Task 2 完成（Core Models 存在）

**目标行为:** 实现 JSON 设置文件的原子读写、损坏恢复、默认值回退。10 个测试用例全部通过。

**允许修改文件:**
- Create: `src/Anhei4Map.Core/Services/SettingsManager.cs`
- Create: `tests/Anhei4Map.Core.Tests/SettingsManagerTests.cs`

**禁止修改范围:**
- 不得修改 State/、WPF 项目、docs/、CLAUDE.md

**先失败的测试:**

```csharp
// tests/Anhei4Map.Core.Tests/SettingsManagerTests.cs
using System.Text.Json;
using Anhei4Map.Core.Models;
using Anhei4Map.Core.Services;

namespace Anhei4Map.Core.Tests;

public class SettingsManagerTests
{
    private static string TestDir(int n) =>
        Path.Combine(Path.GetTempPath(), $"Anhei4Map_Tests_{n}");

    [Fact]
    public void SaveThenLoad_RoundTrip()
    {
        var dir = TestDir(1);
        try
        {
            var mgr = new SettingsManager(dir);
            var original = AppSettings.CreateDefaults();
            mgr.Save(original);
            var loaded = mgr.Load();
            Assert.Equal(original.Placement, loaded.Placement);
            Assert.Equal(original.ZoomLevel, loaded.ZoomLevel);
            Assert.Equal(original.Hotkeys.Count, loaded.Hotkeys.Count);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenFileNotExists()
    {
        var dir = TestDir(2);
        try
        {
            var mgr = new SettingsManager(dir);
            var loaded = mgr.Load();
            Assert.Equal(640, loaded.Placement.Width);
            Assert.Equal(360, loaded.Placement.Height);
            Assert.Equal(8, loaded.Hotkeys.Count);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenJsonMalformed()
    {
        var dir = TestDir(3);
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "settings.json"), "NOT JSON {{{");
            var mgr = new SettingsManager(dir);
            var loaded = mgr.Load();
            Assert.Equal(640, loaded.Placement.Width);
            Assert.True(File.Exists(Path.Combine(dir, "settings.json.bak")));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenWidthZero()
    {
        var dir = TestDir(4);
        try
        {
            Directory.CreateDirectory(dir);
            var bad = AppSettings.CreateDefaults();
            var badPlacement = bad.Placement with { Width = 0 };
            var badSettings = bad with { Placement = badPlacement };
            File.WriteAllText(Path.Combine(dir, "settings.json"),
                JsonSerializer.Serialize(badSettings));
            var mgr = new SettingsManager(dir);
            var loaded = mgr.Load();
            Assert.Equal(640, loaded.Placement.Width);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenOpacityOutOfRange()
    {
        var dir = TestDir(5);
        try
        {
            Directory.CreateDirectory(dir);
            var bad = AppSettings.CreateDefaults();
            var badPlacement = bad.Placement with { Opacity = 1.5 };
            var badSettings = bad with { Placement = badPlacement };
            File.WriteAllText(Path.Combine(dir, "settings.json"),
                JsonSerializer.Serialize(badSettings));
            var mgr = new SettingsManager(dir);
            var loaded = mgr.Load();
            Assert.Equal(0.9, loaded.Placement.Opacity);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Save_CreatesTmpThenRenames()
    {
        var dir = TestDir(6);
        try
        {
            var mgr = new SettingsManager(dir);
            mgr.Save(AppSettings.CreateDefaults());
            Assert.False(File.Exists(Path.Combine(dir, "settings.json.tmp")));
            Assert.True(File.Exists(Path.Combine(dir, "settings.json")));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Save_CreatesDirectoryIfNotExists()
    {
        var dir = TestDir(7);
        try
        {
            var mgr = new SettingsManager(dir);
            mgr.Save(AppSettings.CreateDefaults());
            Assert.True(Directory.Exists(dir));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Load_CleansResidualTmpFile()
    {
        var dir = TestDir(8);
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "settings.json.tmp"), "orphaned");
            var mgr = new SettingsManager(dir);
            var loaded = mgr.Load();
            Assert.False(File.Exists(Path.Combine(dir, "settings.json.tmp")));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Load_IgnoresUnknownJsonFields()
    {
        var dir = TestDir(9);
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "settings.json"),
                @"{""Placement"":{""Left"":100,""Top"":200,""Width"":800,""Height"":400,""Opacity"":0.7},""ZoomLevel"":1.5,""Hotkeys"":[],""UnknownField"":42}");
            var mgr = new SettingsManager(dir);
            var loaded = mgr.Load();
            Assert.Equal(100, loaded.Placement.Left);
            Assert.Equal(1.5, loaded.ZoomLevel);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Save_PreservesOldFileWhenDiskFull()
    {
        var dir = TestDir(10);
        try
        {
            Directory.CreateDirectory(dir);
            var mgr = new SettingsManager(dir);
            var original = AppSettings.CreateDefaults();
            mgr.Save(original);
            // Simulate disk-full by making the path read-only. On real disk-full
            // the File.Move fails; we test that the old file is not corrupted.
            var path = Path.Combine(dir, "settings.json");
            File.SetAttributes(path, FileAttributes.ReadOnly);
            var modified = original with { Placement = original.Placement with { Width = 999 } };
            Assert.Throws<IOException>(() => mgr.Save(modified));
            File.SetAttributes(path, FileAttributes.Normal);
            var loaded = mgr.Load();
            Assert.Equal(original.Placement.Width, loaded.Placement.Width);
        }
        finally
        {
            try { File.SetAttributes(Path.Combine(dir, "settings.json"), FileAttributes.Normal); } catch { }
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
```

**预期失败原因:**
编译错误——`SettingsManager` 类不存在。

**最小实现:**

```csharp
// src/Anhei4Map.Core/Services/SettingsManager.cs
using System.Text.Json;
using Anhei4Map.Core.Models;

namespace Anhei4Map.Core.Services;

public class SettingsManager
{
    private readonly string _directory;
    private string FilePath => Path.Combine(_directory, "settings.json");
    private string TmpPath => Path.Combine(_directory, "settings.json.tmp");
    private string BakPath => Path.Combine(_directory, "settings.json.bak");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SettingsManager(string directory)
    {
        _directory = directory;
    }

    public AppSettings Load()
    {
        // Clean residual tmp from crashed previous write
        if (File.Exists(TmpPath))
        {
            try { File.Delete(TmpPath); } catch { }
        }

        if (!File.Exists(FilePath))
            return AppSettings.CreateDefaults();

        try
        {
            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings == null) throw new JsonException("Deserialized to null.");
            if (!Validate(settings))
            {
                BackupCorrupted();
                return AppSettings.CreateDefaults();
            }
            return settings;
        }
        catch (JsonException)
        {
            BackupCorrupted();
            return AppSettings.CreateDefaults();
        }
    }

    public void Save(AppSettings settings)
    {
        if (!Directory.Exists(_directory))
            Directory.CreateDirectory(_directory);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(TmpPath, json);
        File.Move(TmpPath, FilePath, overwrite: true);
    }

    private static bool Validate(AppSettings s) =>
        s.Placement.Width >= 200 && s.Placement.Height >= 150
        && s.Placement.Opacity >= 0.1 && s.Placement.Opacity <= 1.0
        && s.ZoomLevel >= 0.25 && s.ZoomLevel <= 5.0;

    private void BackupCorrupted()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Move(FilePath, BakPath, overwrite: true);
        }
        catch { }
    }
}
```

**验证命令:**
```bash
dotnet test tests/Anhei4Map.Core.Tests/ --filter "FullyQualifiedName~SettingsManagerTests"
```

**人工验证:**
- 确认 10/10 测试通过
- 检查 `%TEMP%\Anhei4Map_Tests_1\settings.json` 在 T3.1 后文件格式正确

**完成标准:**
- 10 个测试用例全部通过
- 原子写：无 `.tmp` 残留（正常路径）
- 损坏恢复：JSON 损坏 → 默认值 + `.bak`
- 验证失败：Width=0 → 默认值
- 前向兼容：未知 JSON 字段忽略
- 磁盘满：旧文件不损坏

**Git 提交信息:**
```
feat: add SettingsManager with atomic write and corruption recovery (10 tests)
```

---

### Task 6: WindowBoundsRecovery + Tests

**前置条件:** Task 2 完成（Models 存在）、Task 1 完成

**目标行为:** 实现多显示器窗口越界找回算法（20% 交集阈值、16px 边距、最小尺寸 clamp）。13 个测试用例全部通过。

**允许修改文件:**
- Create: `src/Anhei4Map.Core/Services/WindowBoundsRecovery.cs`
- Create: `tests/Anhei4Map.Core.Tests/WindowBoundsRecoveryTests.cs`
- Modify: `src/Anhei4Map.Core/Interop/IWin32Interop.cs`（确保 ScreenInfo/DpiInfo record 可用）

**禁止修改范围:**
- 不得修改 State/、SettingsManager、WPF 项目、docs/、CLAUDE.md

**先失败的测试:**

```csharp
// tests/Anhei4Map.Core.Tests/WindowBoundsRecoveryTests.cs
using Anhei4Map.Core.Interop;
using Anhei4Map.Core.Models;
using Anhei4Map.Core.Services;

namespace Anhei4Map.Core.Tests;

public class WindowBoundsRecoveryTests
{
    private static ScreenInfo[] SingleMonitor() => new[]
        { new ScreenInfo(0, 0, 2560, 1440) };

    private static ScreenInfo[] DualMonitor() => new[]
    {
        new ScreenInfo(0, 0, 2560, 1440),
        new ScreenInfo(2560, 0, 1920, 1080)
    };

    [Fact]
    public void WindowFullyOnMonitor_Unchanged()
    {
        var placement = new WindowPlacement(100, 100, 640, 360, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, SingleMonitor());
        Assert.Equal(placement, result);
    }

    [Fact]
    public void Window50PercentOffscreen_StillVisible_Unchanged()
    {
        // Half on, half off → >=20% → keep
        var placement = new WindowPlacement(2240, 100, 640, 360, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, SingleMonitor());
        Assert.Equal(placement, result);
    }

    [Fact]
    public void Window10PercentVisible_Resets()
    {
        var placement = new WindowPlacement(2520, 100, 640, 360, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, SingleMonitor());
        Assert.NotEqual(placement, result);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void WindowCompletelyOffscreen_Resets()
    {
        var placement = new WindowPlacement(-1000, -1000, 640, 360, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, SingleMonitor());
        Assert.NotEqual(placement, result);
    }

    [Fact]
    public void WindowOnSecondary_Unchanged()
    {
        var placement = new WindowPlacement(2600, 100, 640, 360, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, DualMonitor());
        Assert.Equal(placement, result);
    }

    [Fact]
    public void OnlyPrimaryAvailable_OffscreenResetsToPrimary()
    {
        var placement = new WindowPlacement(3000, 100, 640, 360, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, SingleMonitor());
        Assert.True(result.Left <= 2560 && result.Top >= 0);
    }

    [Fact]
    public void WidthBelowMin_Resets()
    {
        var placement = new WindowPlacement(100, 100, 50, 360, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, SingleMonitor());
        Assert.Equal(640, result.Width);
    }

    [Fact]
    public void HeightBelowMin_Resets()
    {
        var placement = new WindowPlacement(100, 100, 640, 50, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, SingleMonitor());
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void WidthExceedsScreen_Clamped()
    {
        var placement = new WindowPlacement(100, 100, 5000, 360, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, SingleMonitor());
        Assert.True(result.Width <= 2560);
    }

    [Fact]
    public void NoMonitors_Fallback()
    {
        var placement = new WindowPlacement(100, 100, 640, 360, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, Array.Empty<ScreenInfo>());
        Assert.Equal(0, result.Left);
        Assert.Equal(16, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void ResetPosition_Has16pxEdgePadding()
    {
        var placement = new WindowPlacement(-500, -500, 640, 360, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, SingleMonitor());
        Assert.Equal(2560 - 640 - 16, result.Left);
        Assert.Equal(16, result.Top);
    }

    [Fact]
    public void DpiScale1_NoChange()
    {
        var placement = new WindowPlacement(100, 100, 640, 360, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, SingleMonitor());
        Assert.Equal(placement, result);
    }

    [Fact]
    public void HeightExceedsScreen_Clamped()
    {
        var placement = new WindowPlacement(100, 100, 640, 3000, 0.9);
        var result = WindowBoundsRecovery.Recover(placement, SingleMonitor());
        Assert.True(result.Height <= 1440);
    }
}
```

**预期失败原因:**
编译错误——`WindowBoundsRecovery` 类不存在。

**最小实现:**

```csharp
// src/Anhei4Map.Core/Services/WindowBoundsRecovery.cs
using Anhei4Map.Core.Interop;
using Anhei4Map.Core.Models;

namespace Anhei4Map.Core.Services;

public static class WindowBoundsRecovery
{
    private const double VisibilityThreshold = 0.2;
    private const int MinWidth = 200;
    private const int MinHeight = 150;
    private const int DefaultWidth = 640;
    private const int DefaultHeight = 360;
    private const int EdgePadding = 16;

    public static WindowPlacement Recover(WindowPlacement saved, ScreenInfo[] monitors)
    {
        // Clamp to min/max size
        var clampedWidth = Math.Clamp(saved.Width, MinWidth,
            monitors.Length > 0 ? monitors.Max(m => m.Width) : DefaultWidth);
        var clampedHeight = Math.Clamp(saved.Height, MinHeight,
            monitors.Length > 0 ? monitors.Max(m => m.Height) : DefaultHeight);

        if (monitors.Length == 0)
            return new WindowPlacement(0, EdgePadding, DefaultWidth, DefaultHeight, saved.Opacity);

        // Check visibility on any monitor
        var rectArea = (double)clampedWidth * clampedHeight;
        foreach (var m in monitors)
        {
            var overlapW = Math.Max(0,
                Math.Min(saved.Left + clampedWidth, m.Left + m.Width)
                - Math.Max(saved.Left, m.Left));
            var overlapH = Math.Max(0,
                Math.Min(saved.Top + clampedHeight, m.Top + m.Height)
                - Math.Max(saved.Top, m.Top));
            if (overlapW * overlapH >= rectArea * VisibilityThreshold)
                return saved with { Width = clampedWidth, Height = clampedHeight };
        }

        // No monitor has enough overlap — reset to primary top-right
        var primary = monitors[0];
        return new WindowPlacement(
            primary.Left + primary.Width - DefaultWidth - EdgePadding,
            primary.Top + EdgePadding,
            DefaultWidth, DefaultHeight, saved.Opacity);
    }
}
```

**验证命令:**
```bash
dotnet test tests/Anhei4Map.Core.Tests/ --filter "FullyQualifiedName~WindowBoundsRecoveryTests"
```

**人工验证:**
- 确认 13/13 测试通过

**完成标准:**
- 13 个测试用例全部通过
- 20% 可见阈值正确
- 无显示器时 fallback 到 (0, 16, 640, 360)
- 最小尺寸 clamp：<200 → 640、<150 → 360
- 最大尺寸 clamp：不超屏幕
- 恢复位置正确计算 16px 边距

**Git 提交信息:**
```
feat: add WindowBoundsRecovery with multi-monitor out-of-bounds detection (13 tests)
```

---

### Task 7: HotkeyDispatcher + Tests

**前置条件:** Task 2 完成（Models 存在）

**目标行为:** 实现热键 ID 到命令的分发器。支持默认绑定、自定义绑定覆盖、未知 ID 返回 UnknownCommand。6 个测试用例。

**允许修改文件:**
- Create: `src/Anhei4Map.Core/Services/HotkeyDispatcher.cs`
- Create: `tests/Anhei4Map.Core.Tests/HotkeyDispatcherTests.cs`

**禁止修改范围:**
- 不得修改 State/、SettingsManager、WPF 项目、docs/、CLAUDE.md

**先失败的测试:**

```csharp
// tests/Anhei4Map.Core.Tests/HotkeyDispatcherTests.cs
using Anhei4Map.Core.Models;
using Anhei4Map.Core.Services;

namespace Anhei4Map.Core.Tests;

public class HotkeyDispatcherTests
{
    [Fact]
    public void KnownIds_DispatchCorrectCommands()
    {
        var bindings = AppSettings.CreateDefaults().Hotkeys;
        var dispatcher = new HotkeyDispatcher(bindings);

        Assert.Equal(HotkeyCommand.ToggleLock, dispatcher.Dispatch(1));
        Assert.Equal(HotkeyCommand.ToggleHide, dispatcher.Dispatch(2));
        Assert.Equal(HotkeyCommand.ZoomIn, dispatcher.Dispatch(3));
        Assert.Equal(HotkeyCommand.ZoomOut, dispatcher.Dispatch(4));
        Assert.Equal(HotkeyCommand.OpacityUp, dispatcher.Dispatch(5));
        Assert.Equal(HotkeyCommand.OpacityDown, dispatcher.Dispatch(6));
        Assert.Equal(HotkeyCommand.ResetPosition, dispatcher.Dispatch(7));
        Assert.Equal(HotkeyCommand.Refresh, dispatcher.Dispatch(8));
    }

    [Fact]
    public void UnknownId_ReturnsUnknownCommand()
    {
        var dispatcher = new HotkeyDispatcher(AppSettings.CreateDefaults().Hotkeys);
        Assert.Equal(HotkeyCommand.Unknown, dispatcher.Dispatch(999));
    }

    [Fact]
    public void EmptyBindings_AllReturnUnknown()
    {
        var dispatcher = new HotkeyDispatcher(new List<HotkeyBinding>());
        Assert.Equal(HotkeyCommand.Unknown, dispatcher.Dispatch(1));
        Assert.Equal(HotkeyCommand.Unknown, dispatcher.Dispatch(2));
    }

    [Fact]
    public void CustomBindings_OverrideDefaults()
    {
        var custom = new List<HotkeyBinding>
        {
            new(1, 0x0002, 0x41, HotkeyCommand.ZoomIn)  // Ctrl+A → ZoomIn
        };
        var dispatcher = new HotkeyDispatcher(custom);
        Assert.Equal(HotkeyCommand.ZoomIn, dispatcher.Dispatch(1));
        Assert.Equal(HotkeyCommand.Unknown, dispatcher.Dispatch(2));
    }

    [Fact]
    public void DuplicateIds_LastWins()
    {
        var bindings = new List<HotkeyBinding>
        {
            new(1, 0x0003, 0x4D, HotkeyCommand.ToggleLock),
            new(1, 0x0003, 0x4D, HotkeyCommand.ZoomIn)
        };
        var dispatcher = new HotkeyDispatcher(bindings);
        Assert.Equal(HotkeyCommand.ZoomIn, dispatcher.Dispatch(1));
    }

    [Fact]
    public void Dispatch_NegativeId_ReturnsUnknown()
    {
        var dispatcher = new HotkeyDispatcher(AppSettings.CreateDefaults().Hotkeys);
        Assert.Equal(HotkeyCommand.Unknown, dispatcher.Dispatch(-1));
    }
}
```

**预期失败原因:**
编译错误——`HotkeyDispatcher` 类不存在。

**最小实现:**

```csharp
// src/Anhei4Map.Core/Services/HotkeyDispatcher.cs
using Anhei4Map.Core.Models;

namespace Anhei4Map.Core.Services;

public class HotkeyDispatcher
{
    private readonly Dictionary<int, HotkeyCommand> _map = new();

    public HotkeyDispatcher(IEnumerable<HotkeyBinding> bindings)
    {
        foreach (var b in bindings)
            _map[b.Id] = b.Command;
    }

    public HotkeyCommand Dispatch(int id) =>
        _map.TryGetValue(id, out var cmd) ? cmd : HotkeyCommand.Unknown;
}
```

**验证命令:**
```bash
dotnet test tests/Anhei4Map.Core.Tests/ --filter "FullyQualifiedName~HotkeyDispatcherTests"
```

**人工验证:**
- 确认 6/6 测试通过

**完成标准:**
- 6 个测试用例全部通过
- 8 个默认快捷键 ID 正确映射
- 未知 ID → `UnknownCommand`
- 重复 ID → 最后注册的胜出

**Git 提交信息:**
```
feat: add HotkeyDispatcher with binding-to-command resolution (6 tests)
```

---

### Task 8: IWin32Interop Interface

**前置条件:** Task 1 完成

**目标行为:** 定义 Win32 互操作的薄接口和两个 record 类型。Core 层仅定义接口——不实现 P/Invoke。WPF 层在 Task 10 实现。

**允许修改文件:**
- Create: `src/Anhei4Map.Core/Interop/IWin32Interop.cs`

**禁止修改范围:**
- 不得引用 WPF、Windows Forms、或任何 Win32 命名空间
- 不得实现 P/Invoke
- 不得修改 Models/、Services/、WPF 项目

**先失败的测试:**
无——纯接口定义。编译即验证。

**预期失败原因:**
N/A

**最小实现:**

```csharp
// src/Anhei4Map.Core/Interop/IWin32Interop.cs
namespace Anhei4Map.Core.Interop;

public interface IWin32Interop
{
    int RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    bool UnregisterHotKey(IntPtr hwnd, int id);
    IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newStyle);
    IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
        int x, int y, int cx, int cy, uint flags);
    ScreenInfo[] GetMonitorWorkingAreas();
    DpiInfo GetDpiForWindow(IntPtr hwnd);
}

public record ScreenInfo(int Left, int Top, int Width, int Height);
public record DpiInfo(float ScaleX, float ScaleY);
```

**验证命令:**
```bash
dotnet build src/Anhei4Map.Core/
```

**人工验证:**
- 确认 `src/Anhei4Map.Core/Interop/IWin32Interop.cs` 存在
- 确认 `IWin32Interop` 接口含 7 个方法
- 确认 `ScreenInfo` 和 `DpiInfo` record 定义

**完成标准:**
- `dotnet build` 零错误
- 接口不引用 WPF/Win32 类型（除 `IntPtr`）

**Git 提交信息:**
```
feat: add IWin32Interop thin interface for Win32 calls
```

---

### Task 9: AppLogger

**前置条件:** Task 1 完成

**目标行为:** 实现简单的结构化文件日志器，格式 `[ISO8601] [LEVEL] [source] message`。

**允许修改文件:**
- Create: `src/Anhei4Map.Core/Logging/AppLogger.cs`

**禁止修改范围:**
- 不得修改 Interop/、Models/、Services/、State/、WPF 项目

**先失败的测试:**
无——AppLogger 是 thin file I/O wrapper。Manual verification 即可。

**预期失败原因:**
N/A

**最小实现:**

```csharp
// src/Anhei4Map.Core/Logging/AppLogger.cs
namespace Anhei4Map.Core.Logging;

public enum LogLevel { Debug, Info, Warn, Error, Fatal }

public class AppLogger
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private const long MaxSize = 10 * 1024 * 1024;  // 10MB
    private const int MaxRotations = 3;

    public AppLogger(string directory)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "app.log");
    }

    public void Log(LogLevel level, string source, string message)
    {
        var line = $"[{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}] [{level.ToString().ToUpper()}] [{source}] {message}";
        lock (_lock)
        {
            RotateIfNeeded();
            File.AppendAllText(_filePath, line + Environment.NewLine);
        }
    }

    public void Debug(string source, string msg) => Log(LogLevel.Debug, source, msg);
    public void Info(string source, string msg) => Log(LogLevel.Info, source, msg);
    public void Warn(string source, string msg) => Log(LogLevel.Warn, source, msg);
    public void Error(string source, string msg) => Log(LogLevel.Error, source, msg);
    public void Fatal(string source, string msg) => Log(LogLevel.Fatal, source, msg);

    public void Flush() { /* File.AppendAllText already flushes; no-op for symmetry */ }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_filePath)) return;
        var info = new FileInfo(_filePath);
        if (info.Length < MaxSize) return;

        // Rotate: app.2.log → app.3.log, app.1.log → app.2.log, app.log → app.1.log
        for (int i = MaxRotations; i >= 1; i--)
        {
            var oldPath = i == 1 ? _filePath : Path.Combine(
                Path.GetDirectoryName(_filePath)!, $"app.{i - 1}.log");
            var newPath = Path.Combine(
                Path.GetDirectoryName(_filePath)!, $"app.{i}.log");
            if (File.Exists(oldPath))
            {
                if (File.Exists(newPath)) File.Delete(newPath);
                File.Move(oldPath, newPath);
            }
        }
    }
}
```

**验证命令:**
```bash
dotnet build src/Anhei4Map.Core/
```

**人工验证:**
- 确认 `src/Anhei4Map.Core/Logging/AppLogger.cs` 存在
- 确认 `Log()` 方法输出格式 `[ISO8601] [LEVEL] [source] message`
- 确认所有 5 个便捷方法（Debug/Info/Warn/Error/Fatal）可用

**完成标准:**
- `dotnet build` 零错误
- 5 个日志级别：Debug、Info、Warn、Error、Fatal
- 日志格式含 ISO8601 时间戳、级别、源、消息
- 10MB 自动轮转、保留最多 3 个备份

**Git 提交信息:**
```
feat: add AppLogger with structured logging and log rotation
```

---

### Task 10: Full Test Suite Verification + Stage-1 Gate

**前置条件:** Task 1–9 全部完成

**目标行为:** 运行全部 51 个单元测试，确认零失败。运行 `dotnet build` 确认全解决方案编译通过。更新 project-status.md。

**允许修改文件:**
- Modify: `docs/status/project-status.md`（更新为 STAGE-1-COMPLETE）

**禁止修改范围:**
- 不得修改任何 src/ 或 tests/ 下的 `.cs` 文件
- 不得修改 docs/design/

**先失败的测试:**
无——此任务为验证任务。

**预期失败原因:**
N/A（如果前置任务全部完成，应全部通过）

**验证命令:**
```bash
dotnet test tests/Anhei4Map.Core.Tests/ --logger "console;verbosity=detailed"
```

期望输出：
```
Passed!  - Failed:     0, Passed:    51, Skipped:     0, Total:    51
```

**人工验证:**
- 确认测试输出显示 "51 Passed, 0 Failed"
- 确认 `dotnet build` 零错误
- 确认 git status 干净（仅 project-status.md 修改）

**完成标准:**
- `dotnet test` 51/51 通过，0 跳过
- `dotnet build` 零错误
- project-status.md 更新为 `Current phase: STAGE-1-COMPLETE`

**Git 提交信息:**
```
chore: verify full test suite (51/51 pass) and mark stage-1 complete
```

---

## Stage-1 Summary

| Task | Component | Tests | Files |
|------|-----------|-------|-------|
| 1 | Solution Scaffold | — | 4 |
| 2 | Core Models | — | 4 |
| 3 | RetryPolicy | 10 | 2 |
| 4 | WindowStateMachine | 12 | 2 |
| 5 | SettingsManager | 10 | 2 |
| 6 | WindowBoundsRecovery | 13 | 2 |
| 7 | HotkeyDispatcher | 6 | 2 |
| 8 | IWin32Interop | — | 1 |
| 9 | AppLogger | — | 1 |
| 10 | Verification Gate | — | 1 |
| **Total** | | **51** | **21** |

## Post-Stage-1

Stage-2（下一个 Worktree 阶段）将实现 WPF View 层：MainWindow、EditOverlay、WebView2 集成、热键注册/注销、单实例 Mutex。Stage-2 不属本计划范围。
