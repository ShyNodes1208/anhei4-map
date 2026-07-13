# 当前任务

## 阶段
STAGE-02-APP-SHELL

## 任务编号
STAGE-02-TASK-07

## 类型
IMPLEMENTATION

## 状态
READY

## 组件
WebView2 Setup + Safe Navigation

## 下一执行者
Cursor

---

## 聚合规则

本任务集合 7 个子规则，Cursor 必须逐项满足，不可省略或自行发挥。

---

## 规则 1: 允许修改的精确路径

- `src/Anhei4Map.App/MainWindow.xaml.cs`

只有 MainWindow.xaml.cs 需要修改。WebView2 初始化完全在 MainWindow 内完成。

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.Infrastructure/**`
- `src/Anhei4Map.App/App.xaml.cs`
- `src/Anhei4Map.App/App.xaml`
- `src/Anhei4Map.App/MainWindow.xaml`
- `tests/**`
- `docs/design/**`
- `*.csproj`
- `*.sln`
- 不改 DomainPolicy
- 不改 WebView2 NuGet 版本

---

## 规则 2: 初始化生命周期

### 触发时机

`MainWindow.Loaded` 事件——窗口完全加载且 HWND 可用后开始 WebView2 初始化。不在构造函数中启动初始化。

### 防重复

```csharp
private bool _webViewInitialized;

private async void OnLoaded(object sender, RoutedEventArgs e)
{
    if (_webViewInitialized) return;
    _webViewInitialized = true;
    await InitializeWebViewAsync();
}
```

### 关闭取消

```csharp
private CancellationTokenSource? _initCts;

public MainWindow()
{
    InitializeComponent();
    SourceInitialized += OnSourceInitialized;
    Loaded += OnLoaded;
    Closing += OnClosing;
}

private void OnClosing(object? sender, CancelEventArgs e)
{
    _initCts?.Cancel();
    _initCts?.Dispose();
}
```

- `InitializeWebViewAsync` 内部异步操作传递 `_initCts.Token`
- 窗口关闭时取消进行中的初始化
- 初始化完成后，`NavigationStarting` 等事件中的代码不依赖 `_initCts`

---

## 规则 3: 初始化顺序（冻结）

```
1. _initCts = new CancellationTokenSource()
2. Directory.CreateDirectory(userDataFolder)
3. env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, null, _initCts.Token)
4. await webView.EnsureCoreWebView2Async(env, _initCts.Token)
5. ConfigureWebViewSettings()  // 设置 8 项属性
6. RegisterWebViewEvents()     // 注册 3 类事件
7. webView.CoreWebView2.Navigate("https://helltides.com/")
```

任一步失败（异常或取消）→ 跳过所有后续步骤，进入失败处理。

---

## 规则 4: 8 项 WebView2 设置

来自 [02-architecture.md:199-207](docs/design/02-architecture.md)：

| # | 属性 | 值 | 原因 |
|---|------|-----|------|
| 1 | IsScriptEnabled | true | helltides.com 需要 JS |
| 2 | IsWebMessageEnabled | false | 无 JS↔C# 通信 |
| 3 | AreDefaultScriptDialogsEnabled | false | 禁止 alert/confirm/prompt |
| 4 | IsStatusBarEnabled | false | 无状态栏 |
| 5 | AreDevToolsEnabled | false | 禁止 F12 |
| 6 | IsPasswordAutosaveEnabled | false | 不保存密码 |
| 7 | IsGeneralAutofillEnabled | false | 不保存表单 |
| 8 | IsPinchZoomEnabled | false | 缩放由应用控制 |

**不在冻结设计中的属性不设置，使用 WebView2 默认值。** 包括但不限于：`AreDefaultContextMenusEnabled`、`IsZoomControlEnabled`、`IsBuiltInErrorPageEnabled`、`AreBrowserAcceleratorKeysEnabled`。

---

## 规则 5: 导航安全事件

### 注册顺序（在 Navigate 之前）

```csharp
private void RegisterWebViewEvents()
{
    webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
    webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
    webView.CoreWebView2.DownloadStarting += OnDownloadStarting;
}
```

### NavigationStarting

```csharp
private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
{
    // Uri 为空或无法解析 → 阻止
    if (string.IsNullOrEmpty(e.Uri))
    {
        e.Cancel = true;
        return;
    }

    // 使用 DomainPolicy 校验 —— 非允许域名一律 Cancel
    if (!DomainPolicy.IsAllowed(e.Uri))
    {
        e.Cancel = true;
    }
}
```

**about:blank 判定（冻结）：**

`DomainPolicy.IsAllowed("about:blank")` → Uri 无法解析为绝对 URI → `UriFormatException` → `IsAllowed` 返回 **false** → `e.Cancel = true` → **about:blank 被阻止。**

不单独为 about:blank 添加白名单。

**不允许的 scheme（由 DomainPolicy 阻止）：**
- http、file、data、javascript、自定义 scheme ——全部被 `IsAllowed` 拒绝

### NewWindowRequested

```csharp
private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
{
    e.Handled = true; // 阻止所有弹窗
}
```

不调用外部浏览器。

### DownloadStarting

```csharp
private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
{
    e.Cancel = true; // 阻止所有下载
}
```

---

## 规则 6: 初始化失败处理（冻结）

```csharp
private async Task InitializeWebViewAsync()
{
    try
    {
        _initCts = new CancellationTokenSource();

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Anhei4Map",
            "WebView2");

        Directory.CreateDirectory(userDataFolder);

        var env = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder,
            options: null).WithCancellation(_initCts.Token);

        await webView.EnsureCoreWebView2Async(env).WithCancellation(_initCts.Token);

        ConfigureWebViewSettings();
        RegisterWebViewEvents();

        webView.CoreWebView2.Navigate("https://helltides.com/");
    }
    catch (OperationCanceledException)
    {
        // 窗口关闭——静默停止
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            $"WebView2 初始化失败，应用无法继续运行。\n\n错误：{ex.Message}",
            "初始化失败 — Anhei4Map",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Application.Current.Shutdown();
    }
}
```

### 失败规则表

| 异常 | 行为 |
|------|------|
| OperationCanceledException | 静默停止——窗口已关闭 |
| 其他任何 Exception | MessageBox + Shutdown |
| 不记录日志 | 推迟到 Stage 03 |
| Navigate 调用后导航失败 | 由 NavigationCompleted IsSuccess==false 处理——Stage 03 处理重试 |

### WithCancellation 扩展

如果 `WithCancellation` 不可用，使用 `Task.WhenAny`：

```csharp
var task = CoreWebView2Environment.CreateAsync(...);
var completed = await Task.WhenAny(task, Task.Delay(-1, _initCts.Token));
if (completed != task)
    throw new OperationCanceledException(_initCts.Token);
var env = await task;
```

---

## 规则 7: Task 完成后的生命周期

初始化成功后：
- 事件保持注册直到窗口关闭
- WebView2 控件由 WPF 自动回收
- `_initCts` 不再需要（可在初始化完成后 Dispose）
- 不保存 CancellationTokenSource 到后续事件中使用

---

## 验证命令

```powershell
dotnet build -c Release
dotnet test -c Release --no-build
git diff --check
```

## 验证标准

- `dotnet build -c Release` 0 errors, 0 warnings
- `dotnet test -c Release --no-build` 160/160 PASS
- `git diff --check` clean
- 手动验证：启动应用确认 helltides.com 加载

---

## Git 提交信息

```
feat: initialize WebView2 with safe navigation to helltides.com

IMPLEMENTATION: WebView2 initialization on MainWindow.Loaded with
cancellation support on window close. 8 security settings applied
per architecture design. Navigation whitelist via DomainPolicy;
about:blank blocked implicitly by DomainPolicy rejecting non-HTTPS.
Popups and downloads blocked. Initialization failure shows error
dialog and shuts down. Retry on navigation failure deferred to Stage 03.
```

---

## Cursor 最终报告格式

```
TASK_COMPLETE

Task: STAGE-02-TASK-07
Component: WebView2 Setup + Safe Navigation
Type: IMPLEMENTATION
Status: DONE
Commit: <hash>
Files Modified:
  - src/Anhei4Map.App/MainWindow.xaml.cs
Init Event: MainWindow.Loaded
Init Sequence: CreateDirectory → CoreWebView2Environment.CreateAsync → EnsureCoreWebView2Async → 8 settings → 3 events → Navigate
WebView2 Settings: 8 per architecture doc (listed above)
about:blank: Blocked by DomainPolicy (non-HTTPS → UriFormatException → false)
Cancellation: CancellationTokenSource cancelled on window Closing
Failure: MessageBox → Shutdown (OperationCanceledException silent)
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
```
