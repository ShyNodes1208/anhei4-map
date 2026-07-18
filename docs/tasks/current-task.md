# 当前任务

## 阶段
STAGE-07

## 任务编号
STAGE-07-TASK-02-BUNDLED-WEBVIEW2-RUNTIME

## 类型
BEHAVIOR

## 状态
READY

## 组件
打包 WebView2 Fixed Version Runtime，实现真正无依赖便携版

## 下一执行者
Cursor

---

## Allowed Paths
- src/Anhei4Map.App/App.xaml.cs
- src/Anhei4Map.App/RendererWindow.xaml.cs
- .gitignore
- scripts/publish-portable.ps1（新增）

## Forbidden
RendererWindow.xaml, OverlayWindow.xaml, OverlayWindow.xaml.cs, ControlWindow.xaml, ControlWindow.xaml.cs, MainWindow.xaml, MainWindow.xaml.cs, Win32Native.cs, Win32Interop.cs, MapViewportDiagnostics.cs, Anhei4Map.App.csproj, 其他 src/**, tests/**, artifacts/**, *.csproj. New Dependencies: NONE.

---

## 分析摘要

当前 WebView2 初始化流程（两处使用系统 Evergreen Runtime）：

1. **App.xaml.cs:50** — `CoreWebView2Environment.GetAvailableBrowserVersionString()` 检查系统 Evergreen，目标电脑无此 Runtime 会失败
2. **RendererWindow.xaml.cs:207** — `CoreWebView2Environment.CreateAsync(browserExecutableFolder: null, ...)` 使用系统 Evergreen
3. **RendererWindow.xaml** — 无 `Source` 属性，全部程序化导航，无需修改 XAML

目标：上述两处均改为使用随包携带的 Fixed Version Runtime。

---

## 实现

### 一、修改 App.xaml.cs — 启动时 Runtime 检查

将第 47-74 行的 Evergreen 版本检查替换为 Fixed Runtime 检查：

```csharp
// === 删除以下代码块（第 47-74 行）===
// string? version;
// try { version = CoreWebView2Environment.GetAvailableBrowserVersionString(); }
// catch (WebView2RuntimeNotFoundException) { ... }
// ...

// === 替换为 ===
var fixedRuntimePath = Path.Combine(
    AppContext.BaseDirectory,
    "WebView2Runtime");

var browserExePath = Path.Combine(fixedRuntimePath, "msedgewebview2.exe");
if (!File.Exists(browserExePath))
{
    ShowRuntimeMissingDialog();
    Shutdown();
    return;
}

string? version;
try
{
    version = CoreWebView2Environment.GetAvailableBrowserVersionString(fixedRuntimePath);
}
catch (Exception ex)
{
    MessageBox.Show(
        $"WebView2 Fixed Runtime 验证失败：{ex.Message}",
        "启动失败",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
    Shutdown();
    return;
}

if (string.IsNullOrWhiteSpace(version))
{
    ShowRuntimeMissingDialog();
    Shutdown();
    return;
}
```

保留 `ShowRuntimeMissingDialog` 方法不变（对话框文案仍适用）。

### 二、修改 RendererWindow.xaml.cs — 使用 Fixed Runtime + Win10 ACL

#### 2.1 新增 using 语句

在现有 using 块末尾添加：

```csharp
using System.Security.AccessControl;
using System.Security.Principal;
```

#### 2.2 新增两个方法

在类中任意位置添加（建议在 `OnClosed` 之后、`InitializeWebViewAsync` 之前）：

```csharp
private static string GetFixedRuntimePath()
{
    return Path.Combine(AppContext.BaseDirectory, "WebView2Runtime");
}

private static void EnsureWebView2RuntimeAcl(string runtimePath)
{
    // 仅 Windows 10（build < 22000）需要此 ACL 修复
    // Windows 11（build >= 22000）自动具备所需权限
    if (Environment.OSVersion.Version.Major != 10 ||
        Environment.OSVersion.Version.Build >= 22000)
    {
        return;
    }

    try
    {
        var directoryInfo = new DirectoryInfo(runtimePath);
        var security = directoryInfo.GetAccessControl();

        // ALL APPLICATION PACKAGES 和 ALL RESTRICTED APPLICATION PACKAGES
        var sids = new[] { "S-1-15-2-1", "S-1-15-2-2" };

        foreach (var sid in sids)
        {
            var rule = new FileSystemAccessRule(
                new SecurityIdentifier(sid),
                FileSystemRights.ReadAndExecute,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow);

            security.AddAccessRule(rule);
        }

        directoryInfo.SetAccessControl(security);
    }
    catch
    {
        // ACL 设置是尽力而为的；失败时不阻止启动
    }
}
```

#### 2.3 修改 InitializeWebViewAsync 方法

将 `InitializeWebViewAsync` 方法体（第 191-242 行）替换为：

```csharp
private async Task InitializeWebViewAsync()
{
    try
    {
        if (_isClosed)
        {
            return;
        }

        var fixedRuntimePath = GetFixedRuntimePath();
        var browserExePath = Path.Combine(fixedRuntimePath, "msedgewebview2.exe");

        if (!File.Exists(browserExePath))
        {
            MessageBox.Show(
                "WebView2 Fixed Runtime 缺失，便携版不完整。\n\n" +
                "请确保 WebView2Runtime 目录与 Anhei4Map.App.exe 位于同一文件夹。",
                "启动失败 — Anhei4Map",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }

        EnsureWebView2RuntimeAcl(fixedRuntimePath);

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Anhei4Map",
            "WebView2");

        Directory.CreateDirectory(userDataFolder);

        var env = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: fixedRuntimePath,
            userDataFolder: userDataFolder,
            options: null);

        if (_isClosed)
        {
            return;
        }

        await webView.EnsureCoreWebView2Async(env);

        if (_isClosed)
        {
            return;
        }

        ConfigureWebViewSettings();
        RegisterWebViewEvents();
        webView.CoreWebView2.Navigate("https://helltides.com/");

        _webViewInitialized = true;
    }
    catch (Exception ex)
    {
        if (!_isClosed)
        {
            MessageBox.Show(
                $"WebView2 初始化失败，应用无法继续运行。\n\n错误：{ex.Message}",
                "初始化失败 — Anhei4Map",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }
}
```

关键变更：
- `browserExecutableFolder: null` → `browserExecutableFolder: fixedRuntimePath`
- 新增 Runtime exe 存在性检查
- 新增 Win10 ACL 调用
- 其余逻辑（userDataFolder、settings、events、navigate）完全不变

### 三、修改 .gitignore

在文件末尾追加：

```
.runtime-cache/
```

### 四、新增 scripts/publish-portable.ps1

```powershell
<#
.SYNOPSIS
    Build and package the portable Anhei4Map application with bundled WebView2 Fixed Runtime.

.DESCRIPTION
    Creates a self-contained win-x64 publish and bundles the WebView2 Fixed Version Runtime
    so target machines require no pre-installed .NET or WebView2.

.PARAMETER RuntimeSource
    Path to a local WebView2 Fixed Version Runtime directory (must contain msedgewebview2.exe).
    Default: D:\AIProjects\anhei4-map\.runtime-cache\webview2-fixed-x64

.PARAMETER OutputDir
    Publish output directory.
    Default: D:\AIProjects\anhei4-map\artifacts\Anhei4Map-win-x64-portable

.PARAMETER Configuration
    Build configuration.
    Default: Release

.PARAMETER SkipBuild
    Skip dotnet publish (use when publish directory already populated).

.EXAMPLE
    .\scripts\publish-portable.ps1
#>

param(
    [string]$RuntimeSource = "D:\AIProjects\anhei4-map\.runtime-cache\webview2-fixed-x64",
    [string]$OutputDir = "D:\AIProjects\anhei4-map\artifacts\Anhei4Map-win-x64-portable",
    [string]$Configuration = "Release",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

# ---------- Validate Runtime ----------
$runtimeExe = Join-Path $RuntimeSource "msedgewebview2.exe"
if (-not (Test-Path $runtimeExe)) {
    throw @"
WebView2 Fixed Runtime not found at:
  $runtimeExe

Download the Fixed Version from:
  https://developer.microsoft.com/microsoft-edge/webview2/

Extract to:
  $RuntimeSource
"@
}

Write-Host "[1/4] Runtime validated: $runtimeExe" -ForegroundColor Green

# ---------- Publish ----------
if (-not $SkipBuild) {
    Write-Host "[2/4] Publishing win-x64 self-contained..." -ForegroundColor Cyan

    Remove-Item $OutputDir -Recurse -Force -ErrorAction SilentlyContinue

    dotnet publish `
        "$repoRoot\src\Anhei4Map.App\Anhei4Map.App.csproj" `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -o $OutputDir

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}
else {
    Write-Host "[2/4] Skipped (publish directory already exists)" -ForegroundColor Yellow
}

# ---------- Verify publish ----------
$appExe = Join-Path $OutputDir "Anhei4Map.App.exe"
if (-not (Test-Path $appExe)) {
    throw "Publish output missing: $appExe"
}

Write-Host "[3/4] Copying WebView2 Fixed Runtime..." -ForegroundColor Cyan

$targetRuntimeDir = Join-Path $OutputDir "WebView2Runtime"
Remove-Item $targetRuntimeDir -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -Path $RuntimeSource -Destination $targetRuntimeDir -Recurse

$targetExe = Join-Path $targetRuntimeDir "msedgewebview2.exe"
if (-not (Test-Path $targetExe)) {
    throw "Runtime copy failed: msedgewebview2.exe not found at $targetExe"
}

# ---------- Create ZIP ----------
Write-Host "[4/4] Creating ZIP..." -ForegroundColor Cyan

$zipPath = Join-Path (Split-Path -Parent $OutputDir) "Anhei4Map-v0.7.0-win-x64-portable.zip"
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

Compress-Archive -Path "$OutputDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

$zip = Get-Item $zipPath

Write-Host ""
Write-Host "=== PUBLISH COMPLETE ===" -ForegroundColor Green
Write-Host "Portable Directory: $OutputDir" -ForegroundColor Green
Write-Host "Portable ZIP:       $zipPath" -ForegroundColor Green
Write-Host "ZIP Size:           $([math]::Round($zip.Length / 1MB, 1)) MB" -ForegroundColor Green
Write-Host "Fixed Runtime:      $targetRuntimeDir" -ForegroundColor Green
```

---

## 保持

- 地图 Overlay 最大范围 945×591
- 等比例缩放算法
- 浮点容差处理
- 地图裁剪逻辑
- Topmost + WS_EX_TRANSPARENT + WS_EX_NOACTIVATE
- 鼠标左键、右键、滚轮穿透
- HH:01 自动刷新（带 10s 预热 + 10 次重试）
- 手动刷新（任务栏控制窗口）
- 退出功能（Application.Current.Shutdown）
- RendererWindow 离屏渲染
- 所有现有测试

## ACL 策略说明

| 项目 | 决策 |
|------|------|
| 方案 | .NET `System.Security.AccessControl.DirectorySecurity` |
| 目标 SID | S-1-15-2-1, S-1-15-2-2 |
| 权限 | ReadAndExecute + ContainerInherit + ObjectInherit |
| 范围 | 仅 Windows 10（build < 22000） |
| 失败处理 | 捕获异常，不阻止启动 |
| 外部进程 | 不使用 icacls |
| 管理员权限 | 不需要 |

`System.Security.AccessControl` 是 .NET 8 内置命名空间，无需额外 NuGet 包。

## 目标便携版目录结构

```
Anhei4Map-win-x64-portable\
├─ Anhei4Map.App.exe
├─ Anhei4Map.App.dll
├─ Microsoft.Web.WebView2.Core.dll
├─ Microsoft.Web.WebView2.Wpf.dll
├─ WebView2Runtime\
│  ├─ msedgewebview2.exe
│  └─ ... (Fixed Version 全部文件)
├─ *.dll (其他 .NET 运行时文件)
└─ runtimes\
```

## 验证

```powershell
dotnet build .\anhei4-map.sln -c Release
dotnet test .\anhei4-map.sln -c Release --no-build
git diff --check
```

要求：
- Build 0 errors
- Build 0 warnings
- Tests 160/160
- Diff check PASS
- 不 push
- 不生成便携版（发布阶段由 Claude 执行）

额外验证（代码审查）：
- `grep -n "browserExecutableFolder: null" src/` 必须无结果
- `grep -n "GetAvailableBrowserVersionString()" src/` 必须无结果（无参数版本）
- `.gitignore` 包含 `.runtime-cache/`

## 提交

```
feat: bundle WebView2 fixed runtime for true portability
```

## Overengineering Check
PASS — 仅修改 Runtime 路径来源（null → fixedRuntimePath）、添加 Win10 ACL 兼容（一个方法）、替换启动检查、新增最小发布脚本。不引入新架构层、服务或状态机。
