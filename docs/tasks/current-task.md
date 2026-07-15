# 当前任务

## 阶段
STAGE-05

## 任务编号
STAGE-05-TASK-01-MOUSE-CLICK-THROUGH

## 类型
IMPLEMENTATION

## 状态
READY

## 组件
OverlayWindow 鼠标穿透 (WS_EX_TRANSPARENT + WS_EX_NOACTIVATE)

## 下一执行者
Cursor

---

## Allowed Paths (2 files)
- src/Anhei4Map.App/OverlayWindow.xaml.cs
- src/Anhei4Map.App/Win32Native.cs

## Forbidden
App.xaml.cs, RendererWindow, MapViewportDiagnostics, MapRegion, DomainPolicy, 其他 src/**, tests/**, *.csproj. New Files: NONE. New Dependencies: NONE.

---

## 实现

### Win32Native.cs — 添加 1 行常量

```csharp
public const uint WS_EX_NOACTIVATE = 0x08000000;
```

### OverlayWindow.xaml.cs — 添加 SourceInitialized

```csharp
using System.Windows.Interop;

public OverlayWindow()
{
    InitializeComponent();
    SourceInitialized += OnSourceInitialized;
}

private void OnSourceInitialized(object? sender, EventArgs e)
{
    var hwnd = new WindowInteropHelper(this).Handle;
    var win32 = new Win32Interop();

    var exStyle = win32.GetWindowLongPtr(hwnd, Win32Native.GWL_EXSTYLE);
    var newExStyle = new IntPtr(
        exStyle.ToInt64() | Win32Native.WS_EX_TRANSPARENT | Win32Native.WS_EX_NOACTIVATE);
    win32.SetWindowLongPtr(hwnd, Win32Native.GWL_EXSTYLE, newExStyle);

    win32.SetWindowPos(hwnd, Win32Native.HWND_TOPMOST, 0,0,0,0,
        Win32Native.SWP_NOACTIVATE | Win32Native.SWP_NOMOVE | Win32Native.SWP_NOSIZE | Win32Native.SWP_SHOWWINDOW);
}
```

WS_EX_TRANSPARENT (0x20) 已存在于 Win32Native。WPF AllowsTransparency=True 已设置 WS_EX_LAYERED，添加 WS_EX_TRANSPARENT 后鼠标消息穿透到底层窗口。

## 保持
Topmost, 位置, 600×375, 等比例, 透明度, 地图/WebView2/diagnostics

## 禁止
开关, 热键, 设置, 托盘, 拖动, 缩放, 配置, 样式服务, 轮询, 后台线程

## 验证
dotnet build -c Release && dotnet test -c Release --no-build && git diff --check

## 提交
feat: enable mouse click-through on overlay window
