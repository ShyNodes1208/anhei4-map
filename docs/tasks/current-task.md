# 当前任务

## 阶段
STAGE-02-APP-SHELL

## 任务编号
STAGE-02-TASK-04

## 类型
SCAFFOLD

## 状态
READY

## 组件
Win32Interop Adapter — IWin32Interop 的 P/Invoke 实现

## 下一执行者
Cursor

---

## 设计依据

[02-architecture.md:29-33](docs/design/02-architecture.md): Win32Interop.cs — IWin32Interop impl (thin adapter)

---

## 创建文件

### 1. `src/Anhei4Map.App/Win32Interop.cs`

实现 `IWin32Interop`，包含所有 5 个方法的 P/Invoke 调用。

```csharp
namespace Anhei4Map.App;

public sealed class Win32Interop : IWin32Interop
{
    // --- Window Styles ---

    public IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) { ... }
    public IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) { ... }
    public bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int width, int height, uint flags) { ... }

    // --- Display ---

    public ScreenInfo[] GetMonitorWorkingAreas() { ... }
    public DpiInfo GetDpiForWindow(IntPtr hWnd) { ... }
}
```

### 2. `src/Anhei4Map.App/Win32Native.cs` (可选)

若不使用单独文件，可将 P/Invoke 签名和常量放在 Win32Interop.cs 的内部静态类中。

---

## 32/64 位兼容策略

公共方法通过 `IntPtr.Size` 在运行时选择正确的 Native API：

```csharp
// IntPtr.Size == 8 → 64 位进程 → SetWindowLongPtrW / GetWindowLongPtrW
// IntPtr.Size == 4 → 32 位进程 → SetWindowLongW   / GetWindowLongW
```

### SetWindowLongPtr（公共方法）

```csharp
public IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
{
    if (IntPtr.Size == 8)
        return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
    else
        return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
}

// 64-bit: returns IntPtr directly
[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

// 32-bit: returns int, dwNewLong is int (truncated from IntPtr via ToInt32)
[DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
```

### GetWindowLongPtr（公共方法）

```csharp
public IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
{
    if (IntPtr.Size == 8)
        return GetWindowLongPtr64(hWnd, nIndex);
    else
        return new IntPtr(GetWindowLong32(hWnd, nIndex));
}

// 64-bit: returns IntPtr directly
[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

// 32-bit: returns int → wrapped as new IntPtr(result)
[DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);
```

**不允许**仅声明一个 `SetWindowLongPtrW` 而忽略 32 位进程兼容。

### SetWindowPos（64/32 通用）

```csharp
[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool SetWindowPos(
    IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
```

SetWindowPos 签名在 32 位和 64 位下完全一致，无需兼容分支。

### 显示器枚举

```csharp
private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

[DllImport("user32.dll")]
private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

[DllImport("user32.dll")]
private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);
```

### GetMonitorWorkingAreas 失败规则

```csharp
public ScreenInfo[] GetMonitorWorkingAreas()
{
    var areas = new List<ScreenInfo>();
    EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hMonitor, hdc, ref rect, dwData) =>
    {
        var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
        if (GetMonitorInfo(hMonitor, ref mi))
        {
            areas.Add(new ScreenInfo(
                mi.rcWork.Left, mi.rcWork.Top,
                mi.rcWork.Right - mi.rcWork.Left,
                mi.rcWork.Bottom - mi.rcWork.Top));
        }
        // GetMonitorInfo 失败 → 跳过该显示器，继续枚举
        return true; // 继续枚举下一个显示器
    }, IntPtr.Zero);
    // EnumDisplayMonitors 返回 false → 回调未触发 → areas 为空列表
    return areas.ToArray();
}
```

**冻结规则：**
- `GetMonitorInfo` 返回 false → **跳过**该显示器，继续枚举下一个。
- `EnumDisplayMonitors` 返回 false → 回调未被调用 → 返回空数组 `new ScreenInfo[0]`。
- 不抛异常。

### GetDpiForWindow

```csharp
[DllImport("user32.dll")]
private static extern uint GetDpiForWindow(IntPtr hWnd);
```

**DPI 转换规则（冻结）：**
```csharp
public DpiInfo GetDpiForWindow(IntPtr hWnd)
{
    var dpi = GetDpiForWindowNative(hWnd);
    var scale = dpi / 96.0f;
    return new DpiInfo(scale, scale);
}
```

- `dpi`：原生 uint（如 96、120、144、192）
- `scale`：`dpi / 96.0f`，float 除法
- ScaleX 和 ScaleY 始终相等：都等于 `dpi / 96.0f`
- `dpi == 0`（API 失败）→ scale = 0.0f，不抛异常，不替换为 1.0

---

## 结构体定义

### RECT

```csharp
[StructLayout(LayoutKind.Sequential)]
private struct RECT
{
    public int Left, Top, Right, Bottom;
}
```

### MONITORINFOEX

```csharp
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
private struct MONITORINFOEX
{
    public int cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string szDevice;
}
```

使用 `cbSize = Marshal.SizeOf<MONITORINFOEX>()`。

---

## Win32 常量

需要定义（可在内部静态类）：

```csharp
// SetWindowLongPtr
public const int GWL_EXSTYLE = -20;

// Extended window styles
public const uint WS_EX_TOPMOST = 0x00000008;
public const uint WS_EX_TRANSPARENT = 0x00000020;
public const uint WS_EX_TOOLWINDOW = 0x00000080;

// SetWindowPos
public static readonly IntPtr HWND_TOPMOST = new(-1);
public const uint SWP_NOACTIVATE = 0x0010;
public const uint SWP_NOMOVE = 0x0002;
public const uint SWP_NOSIZE = 0x0001;
public const uint SWP_SHOWWINDOW = 0x0040;
```

若使用 `nint` / `nuint`（.NET 8 原生支持），可用 `nint` 替代 `IntPtr`，但接口定义使用 `IntPtr`，保持一致。

---

## 异常传播规则

- P/Invoke 不抛异常（API 失败由返回值指示）
- `SetLastError = true` + `Marshal.GetLastWin32Error()` 供上层调用方使用
- 本实现层**不抛异常**——返回 API 原始值
- 若 API 返回 false、IntPtr.Zero 或 0，调用方可自行 `Marshal.GetLastWin32Error()`
- `GetDpiForWindow` 返回 0 → ScaleX=ScaleY=0.0f，不抛异常，不替换为 1.0
- `GetMonitorInfo` 返回 false → 跳过该显示器
- `EnumDisplayMonitors` 返回 false → 返回空数组

---

## 允许修改的精确路径

- `src/Anhei4Map.App/Win32Interop.cs`（新建）
- `src/Anhei4Map.App/Win32Native.cs`（可选，新建——若使用单独文件放 P/Invoke 和常量）

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.Infrastructure/**`
- `tests/**`
- `docs/design/**`
- `*.csproj`
- `*.sln`
- 不引入 RegisterHotKey / UnregisterHotKey
- 不实现 WebView2
- 不实现 MainWindow 逻辑
- 不实现状态机调用
- 不实现 TASK-05+

---

## 验证命令

```powershell
dotnet build -c Release
dotnet test -c Release --no-build
git diff --check
```

## 验证标准

- `dotnet build -c Release` 0 errors, 0 warnings
- `dotnet test -c Release --no-build` 全部通过 (160/160)
- SCAFFOLD 类型无需新增单元测试
- `git diff --check` clean

---

## Git 提交信息

```
feat: implement Win32Interop P/Invoke adapter

SCAFFOLD: Thin P/Invoke wrapper implementing IWin32Interop. Covers
SetWindowLongPtr, GetWindowLongPtr, SetWindowPos, monitor enumeration
via EnumDisplayMonitors/GetMonitorInfo, and GetDpiForWindow. Includes
RECT, MONITORINFOEX structs and standard Win32 constants. No hotkey
registration — that is Stage 03 scope.
```

---

## Cursor 最终报告格式

```
TASK_COMPLETE

Task: STAGE-02-TASK-04
Component: Win32Interop Adapter
Type: SCAFFOLD
Status: DONE
Commit: <hash>
Files Created:
  - src/Anhei4Map.App/Win32Interop.cs
  - src/Anhei4Map.App/Win32Native.cs (if separate)
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
P/Invoke APIs Used:
  - SetWindowLongPtrW, GetWindowLongPtrW, SetWindowPos
  - EnumDisplayMonitors, GetMonitorInfo
  - GetDpiForWindow
Excluded: RegisterHotKey, UnregisterHotKey
```
