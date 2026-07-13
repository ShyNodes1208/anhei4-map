# 当前任务

## 阶段
STAGE-02-APP-SHELL

## 任务编号
STAGE-02-TASK-03

## 类型
SCAFFOLD

## 状态
READY

## 组件
IWin32Interop Interface + ScreenInfo/DpiInfo Record Types

## 下一执行者
Cursor

---

## 设计依据

[02-architecture.md:152-176](docs/design/02-architecture.md): IWin32Interop — Thin Adapter 完整接口定义。

以下规格直接取自冻结架构文档。

---

## 创建文件

### 1. `src/Anhei4Map.Core/Interop/IWin32Interop.cs`

```csharp
using Anhei4Map.Core.Models;

namespace Anhei4Map.Core.Interop;

public interface IWin32Interop
{
    // Hotkey
    int RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    bool UnregisterHotKey(IntPtr hwnd, int id);

    // Window styles
    IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newStyle);
    IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
        int x, int y, int cx, int cy, uint flags);

    // Display
    ScreenInfo[] GetMonitorWorkingAreas();
    DpiInfo GetDpiForWindow(IntPtr hwnd);
}
```

### 2. `src/Anhei4Map.Core/Interop/ScreenInfo.cs`

```csharp
namespace Anhei4Map.Core.Interop;

public record ScreenInfo(int Left, int Top, int Width, int Height);
```

### 3. `src/Anhei4Map.Core/Interop/DpiInfo.cs`

```csharp
namespace Anhei4Map.Core.Interop;

public record DpiInfo(float ScaleX, float ScaleY);
```

---

## 技术边界

- 仅定义接口和记录类型，不实现
- 不写 P/Invoke（由 TASK-04 Win32Interop 实现）
- 不引用 WPF、Win32、WebView2
- IntPtr 使用 `System.IntPtr`
- 不引入第三方依赖
- 不修改 csproj 或 Solution
- 接口中的热键方法留待 Stage 03 使用
- SCAFFOLD 类型不需要 RED 测试

---

## 允许修改的精确路径

- `src/Anhei4Map.Core/Interop/IWin32Interop.cs`（新建）
- `src/Anhei4Map.Core/Interop/ScreenInfo.cs`（新建）
- `src/Anhei4Map.Core/Interop/DpiInfo.cs`（新建）

## 禁止修改范围

- `src/Anhei4Map.App/**`
- `src/Anhei4Map.Infrastructure/**`
- `src/Anhei4Map.Core/State/**`
- `src/Anhei4Map.Core/Services/**`
- `src/Anhei4Map.Core/Models/**`
- `src/Anhei4Map.Core/Logging/**`
- `tests/**`
- `docs/design/**`
- `*.csproj`
- `*.sln`
- 不实现 Win32Interop 适配器
- 不实现 P/Invoke
- 不修改 HotkeyDispatcher 或 WindowStateMachine

---

## 验证命令

```powershell
dotnet build -c Release
dotnet test -c Release --no-build
git diff --check
```

## 验证标准

- `dotnet build -c Release` 0 errors, 0 warnings
- `dotnet test -c Release --no-build` 全部通过 (160/160, 无新增测试)
- `git diff --check` clean
- 只创建了 3 个新文件

---

## Git 提交信息

```
feat: define IWin32Interop interface with display and hotkey records

SCAFFOLD: Interface-only — Win32 P/Invoke adapter contract for
downstream WPF implementation. Includes ScreenInfo and DpiInfo
record types per frozen architecture design.
```

---

## Cursor 最终报告格式

```
TASK_COMPLETE

Task: STAGE-02-TASK-03
Component: IWin32Interop Interface
Type: SCAFFOLD
Status: DONE
Commit: <hash>
Files Created:
  - src/Anhei4Map.Core/Interop/IWin32Interop.cs
  - src/Anhei4Map.Core/Interop/ScreenInfo.cs
  - src/Anhei4Map.Core/Interop/DpiInfo.cs
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
Files NOT Modified (verified): <列出>
```
