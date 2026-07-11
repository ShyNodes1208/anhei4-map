# Stage-01: Foundation — Solution Skeleton + Core Logic

> **Cursor:** 只按本文件和 `docs/tasks/current-task.md` 写代码并提交。不得改需求、架构和范围。一次只执行一个任务。

**Goal:** 创建 anhei4-map .NET 8 解决方案骨架和全部 Core 层纯逻辑（设置管理、窗口边界、域名策略、退避重试、日志）。全部附带 xUnit 测试，零 WPF 依赖。

**Architecture:** Anhei4Map.App (WPF) → Anhei4Map.Core (纯逻辑, testable) ← Anhei4Map.Infrastructure (日志/文件IO) → Anhei4Map.Tests (xUnit)

**Tech Stack:** C# / .NET 8 / WPF / xUnit

## Global Constraints

- 技术栈不可替换
- Core 层不引用 WPF、Win32、WebView2
- 安全红线：不读游戏内存/文件/网络、不注入/Hook、不模拟键鼠
- 不引入数据库、后端服务、爬虫
- SCAFFOLD 任务不强制编造 RED 测试

## Task Map

| # | Component | Type | Tests |
|---|-----------|------|-------|
| 1 | Solution + Project Skeleton | SCAFFOLD | 0 |
| 2 | AppSettings defaults + validation | TDD | ~8 |
| 3 | JSON settings save/load | TDD | ~6 |
| 4 | Corrupted settings recovery | TDD | ~4 |
| 5 | Window bounds normalization | TDD | ~10 |
| 6 | Helltides domain allowlist policy | TDD | ~5 |
| 7 | Limited backoff retry strategy | TDD | ~10 |
| 8 | Log abstraction + file logging | TDD | ~5 |
| 9 | build.ps1, test.ps1, README | SCAFFOLD | 0 |

---

### STAGE-01-TASK-01: Solution + Project Skeleton

**Type:** SCAFFOLD

**Files:**
- Create: `anhei4-map.sln`
- Create: `src/Anhei4Map.App/Anhei4Map.App.csproj` + template files
- Create: `src/Anhei4Map.Core/Anhei4Map.Core.csproj`
- Create: `src/Anhei4Map.Infrastructure/Anhei4Map.Infrastructure.csproj`
- Create: `tests/Anhei4Map.Tests/Anhei4Map.Tests.csproj` + `Usings.cs`

**Implement:** `dotnet new` scaffold commands. Delete template Class1.cs. Establish project references. Verify `dotnet restore && dotnet build && dotnet test`.

**Dependencies:** None. No WebView2. No hotkey logic. No settings logic.

---

### STAGE-01-TASK-02: AppSettings Defaults + Validation

**Type:** TDD

**Files:**
- Create: `src/Anhei4Map.Core/Models/AppSettings.cs`
- Create: `tests/Anhei4Map.Tests/AppSettingsTests.cs`

**Implement:** `AppSettings` record with Placement, Hotkeys, ZoomLevel. `CreateDefaults()` returns 8 default hotkey bindings + 640x360 placement + 1.0 zoom. `Validate()` checks Width >= 200, Height >= 150, Opacity 0.1-1.0, Zoom 0.25-5.0.

---

### STAGE-01-TASK-03: JSON Settings Save/Load

**Type:** TDD

**Files:**
- Create: `src/Anhei4Map.Infrastructure/SettingsManager.cs`
- Create: `tests/Anhei4Map.Tests/SettingsManagerTests.cs`

**Implement:** Atomic write (tmp + rename). Load with defaults fallback when file missing. Forward-compatible JSON deserialization.

---

### STAGE-01-TASK-04: Corrupted Settings Recovery

**Type:** TDD

**Files:**
- Modify: `src/Anhei4Map.Infrastructure/SettingsManager.cs`
- Create: `tests/Anhei4Map.Tests/SettingsRecoveryTests.cs`

**Implement:** JSON parse error → rename to .bak + use defaults. Invalid values (Width=0) → use defaults. Residual .tmp cleanup on next start.

---

### STAGE-01-TASK-05: Window Bounds Normalization

**Type:** TDD

**Files:**
- Create: `src/Anhei4Map.Core/Services/WindowBoundsNormalizer.cs`
- Create: `tests/Anhei4Map.Tests/WindowBoundsNormalizerTests.cs`

**Implement:** 20% visibility threshold. Min size clamp (200x150). Multi-monitor awareness. Off-screen → reset to primary top-right with 16px padding.

---

### STAGE-01-TASK-06: Helltides Domain Allowlist Policy

**Type:** TDD

**Files:**
- Create: `src/Anhei4Map.Core/Services/DomainPolicy.cs`
- Create: `tests/Anhei4Map.Tests/DomainPolicyTests.cs`

**Implement:** `IsAllowed(uri)` returns true only for `https://helltides.com/*` and subdomains. Blocks all other domains, IP addresses, and non-HTTPS schemes. Case-insensitive host comparison.

---

### STAGE-01-TASK-07: Limited Backoff Retry Strategy

**Type:** TDD

**Files:**
- Create: `src/Anhei4Map.Core/Services/RetryPolicy.cs`
- Create: `tests/Anhei4Map.Tests/RetryPolicyTests.cs`

**Implement:** Static method `NextDelay(attempt)` returns delay ms or null. Delays: [0, 1000, 2000, 4000, 8000, 30000...]. Max 10 retries. Negative attempt → throws ArgumentOutOfRangeException.

---

### STAGE-01-TASK-08: Log Abstraction + File Logging

**Type:** TDD

**Files:**
- Create: `src/Anhei4Map.Infrastructure/Logging/IAppLogger.cs`
- Create: `src/Anhei4Map.Infrastructure/Logging/FileLogger.cs`
- Create: `tests/Anhei4Map.Tests/FileLoggerTests.cs`

**Implement:** `IAppLogger` with `Log(level, source, message)`. `FileLogger` writes to `%LocalAppData%\Anhei4Map\app.log`. Format: `[ISO8601] [LEVEL] [source] message`. 10MB rolling rotation, max 3 files.

---

### STAGE-01-TASK-09: build.ps1, test.ps1, README

**Type:** SCAFFOLD

**Files:**
- Create: `build.ps1`
- Create: `test.ps1`
- Modify: `README.md`

**Implement:** `build.ps1` runs `dotnet build -c Release`. `test.ps1` runs `dotnet test -c Release --no-build`. README.md documents project purpose, prerequisites (.NET 8 SDK, WebView2 Runtime for later), build/test commands, and repository structure.
