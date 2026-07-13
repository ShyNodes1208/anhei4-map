# 当前任务

## 阶段
STAGE-02-APP-SHELL

## 任务编号
STAGE-02-TASK-05

## 类型
SCAFFOLD

## 状态
READY

## 组件
App Single Instance Mutex + WebView2 NuGet + Runtime Detection

## 下一执行者
Cursor

---

## 设计依据

- [02-architecture.md:290-316](docs/design/02-architecture.md): Single Instance via Mutex
- [02-architecture.md:182-188](docs/design/02-architecture.md): WebView2 Runtime detection
- Stage 02 plan: TASK-05 正式负责 NuGet 引用（已从 TASK-06 前移）

---

## 允许修改的精确路径

- `src/Anhei4Map.App/Anhei4Map.App.csproj`
- `src/Anhei4Map.App/App.xaml.cs`

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.Infrastructure/**`
- `MainWindow.xaml` / `MainWindow.xaml.cs`
- `tests/**`
- `docs/design/**`
- `anhei4-map.sln`
- 不创建 MainWindow
- 不创建 WebView2 控件
- 不执行导航
- 不实现 TASK-06+

---

## 步 1：NuGet 依赖

在 `Anhei4Map.App.csproj` 中添加：

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2903.40" />
</ItemGroup>
```

版本固定为 `1.0.2903.40`，不使用浮动版本（如 `1.0.*`）。

---

## 步 2：单实例 Mutex

### Mutex 名称

```
Global\Anhei4Map_SingleInstance
```

### 字段声明

```csharp
private static Mutex? _mutex;
```

必须保存为 `static` 字段（非局部变量），防止 GC 回收。

### 创建逻辑

```csharp
_mutex = new Mutex(true, @"Global\Anhei4Map_SingleInstance", out bool createdNew);
```

### createdNew == false（已有实例）

```csharp
if (!createdNew)
{
    _mutex.Dispose();
    _mutex = null;
    Shutdown();
    return; // 不继续执行
}
```

- 立即 Dispose 当前 Mutex 对象
- 不调用 ReleaseMutex（当前实例不拥有该 Mutex）
- Shutdown() 后 return，不继续 Runtime 检测

### createdNew == true（首个实例）

- Mutex 对象保持到应用退出
- `OnExit` 中清理

### Mutex 构造异常

```csharp
try
{
    _mutex = new Mutex(true, @"Global\Anhei4Map_SingleInstance", out bool createdNew);
}
catch (Exception ex) when (ex is UnauthorizedAccessException or WaitHandleCannotBeOpenedException or IOException)
{
    MessageBox.Show(
        $"无法创建应用程序互斥锁：{ex.Message}",
        "启动失败",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
    Shutdown();
    return;
}
```

**冻结规则：**
- UnauthorizedAccessException、WaitHandleCannotBeOpenedException、IOException → MessageBox + Shutdown
- 不传播异常（App 为最外层，异常会导致未处理异常对话框）

### OnExit 清理

```csharp
protected override void OnExit(ExitEventArgs e)
{
    try
    {
        _mutex?.ReleaseMutex();
    }
    catch
    {
        // 忽略——进程退出前释放尽力而为
    }

    _mutex?.Dispose();
    _mutex = null;
}
```

- ReleaseMutex 可能失败（已持有、已释放等），try/catch 防止 OnExit 中断
- Dispose 始终调用

---

## 步 3：WebView2 Runtime 检测

### 前置条件

Mutex 检查通过（`createdNew == true`）后立即执行。

### 检测调用

```csharp
string? version;
try
{
    version = CoreWebView2Environment.GetAvailableBrowserVersionString();
}
catch (WebView2RuntimeNotFoundException)
{
    // 处理见下方
}
```

### 返回值处理

`version` 可能为 null 或空——视为 Runtime 未正确安装：

```csharp
if (string.IsNullOrWhiteSpace(version))
{
    ShowRuntimeMissingDialog();
    Shutdown();
    return;
}
```

### Runtime 缺失对话框

```csharp
private static void ShowRuntimeMissingDialog()
{
    MessageBox.Show(
        "Microsoft Edge WebView2 Runtime 未安装。\n\n" +
        "请从以下链接下载 Evergreen Bootstrapper 后重试：\n\n" +
        "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
        "缺少必需组件 — Anhei4Map",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
}
```

- 标题：`"缺少必需组件 — Anhei4Map"`
- 正文：含下载链接
- Button：OK
- Icon：Error

### 其他异常

`WebView2RuntimeNotFoundException` 之外的所有异常（如 DllNotFoundException）不吞：

```csharp
catch (Exception ex) when (ex is not WebView2RuntimeNotFoundException)
{
    MessageBox.Show(
        $"WebView2 运行时检测失败：{ex.Message}",
        "启动失败",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
    Shutdown();
    return;
}
```

---

## 完整 OnStartup 顺序（冻结）

```
OnStartup:
  try { new Mutex(...); }
    catch → MessageBox → Shutdown → return
  if (!createdNew) → Dispose → Shutdown → return

  try { GetAvailableBrowserVersionString(); }
    catch WebView2RuntimeNotFoundException → MessageBox → Shutdown → return
    catch other → MessageBox → Shutdown → return
  if (string.IsNullOrWhiteSpace(version)) → MessageBox → Shutdown → return

  // TASK-06 continues from here: Create MainWindow
```

**TASK-05 到此停止。不创建 MainWindow。不创建 WebView2 控件。**

---

## 验证命令

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
git diff --check
```

## 验证标准

- `dotnet restore` 成功（下载 WebView2 NuGet）
- `dotnet build -c Release` 0 errors, 0 warnings
- `dotnet test -c Release --no-build` 全部通过 (160/160)
- SCAFFOLD 类型无需新增测试
- `git diff --check` clean

---

## Git 提交信息

```
feat: add single instance mutex and WebView2 runtime check

SCAFFOLD: App startup now enforces single instance via global Mutex
and verifies WebView2 Runtime availability. Adds fixed-version
Microsoft.Web.WebView2 NuGet reference. Mutex failure, runtime
missing, and unexpected exceptions all show MessageBox and clean
Shutdown. Does not create MainWindow or WebView2 control.
```

---

## Cursor 最终报告格式

```
TASK_COMPLETE

Task: STAGE-02-TASK-05
Component: App Single Instance + WebView2 NuGet + Runtime Check
Type: SCAFFOLD
Status: DONE
Commit: <hash>
Files Modified:
  - src/Anhei4Map.App/Anhei4Map.App.csproj
  - src/Anhei4Map.App/App.xaml.cs
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
NuGet Added: Microsoft.Web.WebView2 1.0.2903.40
Mutex: Global\Anhei4Map_SingleInstance
```
