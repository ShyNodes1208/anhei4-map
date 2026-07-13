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

## TASK-07 职责

TASK-06 创建了 MainWindow 和 WebView2 控件外壳。TASK-07 负责：
1. CoreWebView2Environment 初始化和 WebView2 控件就绪
2. WebView2 安全配置（9 项设置）
3. 导航到 https://helltides.com/
4. 导航安全事件（白名单、弹窗拦截、下载拦截、导航完成处理）

---

## 允许修改的精确路径

- `src/Anhei4Map.App/MainWindow.xaml.cs`（添加初始化逻辑）
- `src/Anhei4Map.App/App.xaml.cs`（可能需要调用初始化）

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.Infrastructure/**`
- `MainWindow.xaml`
- `App.xaml`
- `tests/**`
- `docs/design/**`
- `*.csproj`
- `*.sln`
- 不实现 RegisterHotKey
- 不实现鼠标穿透/WS_EX_TRANSPARENT
- 不实现 EditOverlay
- 不实现状态机调用
- 不实现 Stage 03 功能

---

## 步 1：WebView2 初始化

在 MainWindow 构造函数中，`SourceInitialized` 之后添加异步初始化：

```csharp
public MainWindow()
{
    InitializeComponent();
    SourceInitialized += OnSourceInitialized;
    _ = InitializeWebViewAsync();
}

private async Task InitializeWebViewAsync()
{
    var userDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Anhei4Map",
        "WebView2");

    var env = await CoreWebView2Environment.CreateAsync(
        browserExecutableFolder: null,
        userDataFolder: userDataFolder,
        options: null);

    await webView.EnsureCoreWebView2Async(env);
    ConfigureWebViewSettings();
    webView.CoreWebView2.Navigate("https://helltides.com/");
    RegisterWebViewEvents();
}
```

**规则：**
- `_ = InitializeWebViewAsync()` — fire-and-forget，不阻塞构造函数
- userDataFolder：`%LocalAppData%\Anhei4Map\WebView2`
- 先创建 Environment，再 EnsureCoreWebView2Async
- 初始化完成后立即导航
- 导航失败不由 TASK-07 处理重试（Stage 03 实现 RetryPolicy 集成）

---

## 步 2：WebView2 安全配置

```csharp
private void ConfigureWebViewSettings()
{
    var settings = webView.CoreWebView2.Settings;
    settings.IsScriptEnabled = true;                // helltides.com 需要 JS
    settings.IsWebMessageEnabled = false;            // 无 JS↔C# 通信
    settings.AreDefaultScriptDialogsEnabled = false; // 禁止 alert/confirm/prompt
    settings.IsStatusBarEnabled = false;             // 无状态栏
    settings.AreDevToolsEnabled = false;              // 禁止 F12
    settings.IsPasswordAutosaveEnabled = false;       // 禁止保存凭证
    settings.IsGeneralAutofillEnabled = false;        // 禁止表单自动填充
    settings.IsPinchZoomEnabled = false;              // 缩放由应用控制
}
```

9 项设置来自 [02-architecture.md:199-207](docs/design/02-architecture.md)，逐一精确设置。

---

## 步 3：导航安全事件

```csharp
private void RegisterWebViewEvents()
{
    // NavigationStarting — 使用 DomainPolicy 校验
    webView.CoreWebView2.NavigationStarting += (sender, args) =>
    {
        if (!DomainPolicy.IsAllowed(args.Uri))
        {
            args.Cancel = true;
        }
    };

    // NewWindowRequested — 阻止所有弹窗
    webView.CoreWebView2.NewWindowRequested += (sender, args) =>
    {
        args.Handled = true;
    };

    // DownloadStarting — 阻止所有下载
    webView.CoreWebView2.DownloadStarting += (sender, args) =>
    {
        args.Cancel = true;
    };

    // NavigationCompleted — 区分成功与失败
    webView.CoreWebView2.NavigationCompleted += (sender, args) =>
    {
        if (!args.IsSuccess)
        {
            // Stage 03: 触发 RetryPolicy + 显示错误 UI
            // 当前阶段: 不处理
        }
    };
}
```

**规则：**
- `NavigationStarting`：调用 `DomainPolicy.IsAllowed(args.Uri)`；不合法 → `args.Cancel = true`
- 允许 `about:blank`——DomainPolicy 会返回 false（非 https），因此 `about:blank` 被阻止 ✓
- `NewWindowRequested`：所有弹窗 `args.Handled = true`
- `DownloadStarting`：所有下载 `args.Cancel = true`
- `NavigationCompleted`：`IsSuccess == false` 留待 Stage 03 处理，当前阶段仅略过
- 不在本任务中使用 RetryPolicy
- 不显示错误 UI/横幅

---

## 步 4：导航失败规则

- `EnsureCoreWebView2Async` 失败（异常）→ 当前阶段不吞异常，异常会由 fire-and-forget Task 传播为 `TaskScheduler.UnobservedTaskException`
- `Navigate` 失败 → 由 `NavigationCompleted` 的 `IsSuccess == false` 捕获，Stage 03 处理重试
- 初始化失败不阻止窗口显示——窗口已在 `Show()` 时可见，WebView2 初始化异步进行

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
- WebView2 功能需要手动验证（启动 app 确认 helltides.com 加载）

---

## Git 提交信息

```
feat: initialize WebView2 and navigate to helltides.com

IMPLEMENTATION: Configure CoreWebView2Environment with isolated user
data folder. Apply 9 security settings (JS enabled, devtools/popups/
downloads blocked). Navigate to https://helltides.com/. Register
NavigationStarting whitelist via DomainPolicy, NewWindowRequested
and DownloadStarting blockers. Navigation failure handling deferred
to Stage 03.
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
WebView2 Settings: 9 settings configured per architecture doc
Navigation: https://helltides.com/
DomainPolicy: Integrated in NavigationStarting
Popups/Downloads: Blocked
Build: Release 0 errors 0 warnings
Tests: 160/160 PASS
```
