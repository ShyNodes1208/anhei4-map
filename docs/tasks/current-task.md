# 当前任务

## 状态
READY

## 类型
FIX

## 修复编号
STAGE-01-REVIEW-FIX-03

## 对应 Codex 发现
S01-003 (MEDIUM)

## 阶段
STAGE-01-FOUNDATION (修复轮)

## 分支
feature/01-foundation

## Worktree
D:\AIProjects\anhei4-map-worktrees\stage-01-foundation

---

## 问题证据

[FileLogger.cs:19-37](src/Anhei4Map.Infrastructure/Logging/FileLogger.cs#L19-L37):

```csharp
public void Log(LogLevel level, string source, string message, Exception? ex = null)
{
    var entry = FormatEntry(level, source, message, ex);
    _writeLock.Wait();
    try
    {
        var directory = Path.GetDirectoryName(_logFilePath)!;
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        File.AppendAllText(_logFilePath, entry + Environment.NewLine, Encoding.UTF8);
    }
    finally { _writeLock.Release(); }
}
```

每次调用仅执行 `File.AppendAllText`，**不存在**文件大小检查、`app.1.log` 重命名或轮转淘汰逻辑。

**三个设计文档一致要求 Stage 01 实现日志轮转：**
- [stage-01-foundation-plan.md:133](docs/plans/stage-01-foundation-plan.md#L133): "10MB rolling rotation, max 3 files"
- [02-architecture.md:393](docs/design/02-architecture.md#L393): "Rolling 10MB max; on overflow rename to app.1.log, keep 3 rotated files"
- [04-security-boundary.md:67-74](docs/design/04-security-boundary.md#L67-L74): 目录结构列出 `app.1.log` 轮转文件

---

## 允许修改的精确路径

- `src/Anhei4Map.Infrastructure/Logging/FileLogger.cs`
- `tests/Anhei4Map.Tests/FileLoggerTests.cs`

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.App/**`
- `src/Anhei4Map.Infrastructure/Services/**`
- `docs/design/**`
- `docs/plans/**`
- `*.csproj`
- `*.sln`
- 其他所有测试文件

---

## RED 阶段 — 先写失败测试

### 设计决策：可配置阈值

为了让 10MB 轮转可测试，`FileLogger` 构造函数新增可选参数 `maxFileSizeBytes`（默认 10MB = `10 * 1024 * 1024`）。测试使用小阈值（如 256 bytes）触发轮转。

**修改构造函数签名：**
```csharp
public FileLogger(string directory, TimeProvider? timeProvider = null, long maxFileSizeBytes = 10 * 1024 * 1024)
```

### 测试 1: `Log_RotatesWhenFileExceedsThreshold`

写入超过阈值的日志行 → 验证原 `app.log` 被重命名为 `app.1.log`，新日志写入新的 `app.log`。

```csharp
[Fact]
public void Log_RotatesWhenFileExceedsThreshold()
{
    var directory = CreateTempDirectory();
    try
    {
        // 阈值设为 256 bytes，方便触发轮转
        var logger = new FileLogger(directory, new FixedTimeProvider(FixedUtcTime), maxFileSizeBytes: 256);

        // 写入足够多日志以超过阈值
        for (int i = 0; i < 20; i++)
        {
            logger.Log(LogLevel.Info, "test", new string('x', 40)); // ~60 bytes per line
        }

        Assert.True(File.Exists(Path.Combine(directory, "app.1.log")));
        Assert.True(File.Exists(LogFilePath(directory)));
    }
    finally { CleanupTempDirectory(directory); }
}
```

### 测试 2: `Log_RotatesUpTo3Files`

写入大量数据触发多次轮转 → 验证最多保留 `app.log` + `app.1.log` + `app.2.log`，无 `app.3.log`。

```csharp
[Fact]
public void Log_RotatesUpTo3Files()
{
    var directory = CreateTempDirectory();
    try
    {
        var logger = new FileLogger(directory, new FixedTimeProvider(FixedUtcTime), maxFileSizeBytes: 200);

        // 写入大量数据触发多次轮转
        for (int i = 0; i < 40; i++)
        {
            logger.Log(LogLevel.Info, "test", new string('x', 80));
        }

        Assert.True(File.Exists(Path.Combine(directory, "app.1.log")));
        Assert.True(File.Exists(Path.Combine(directory, "app.2.log")));
        Assert.False(File.Exists(Path.Combine(directory, "app.3.log")));
    }
    finally { CleanupTempDirectory(directory); }
}
```

### 测试 3: `Log_OldestFileRemovedOnRotation`

当已有 2 个轮转文件时再次触发轮转 → 验证最旧的 `app.2.log` 被淘汰。

```csharp
[Fact]
public void Log_OldestFileRemovedOnRotation()
{
    var directory = CreateTempDirectory();
    try
    {
        // 预创 app.2.log 模拟已有最旧轮转文件
        File.WriteAllText(Path.Combine(directory, "app.2.log"), "oldest");
        File.WriteAllText(Path.Combine(directory, "app.1.log"), "older");
        File.WriteAllText(LogFilePath(directory), new string('x', 300));

        var logger = new FileLogger(directory, new FixedTimeProvider(FixedUtcTime), maxFileSizeBytes: 200);
        // 写入更多数据触发轮转
        for (int i = 0; i < 5; i++)
        {
            logger.Log(LogLevel.Info, "test", new string('y', 80));
        }

        // 旧的 app.2.log 应被移除，新的轮转产生 app.1.log 和 app.2.log
        Assert.True(File.Exists(Path.Combine(directory, "app.1.log")));
        Assert.True(File.Exists(Path.Combine(directory, "app.2.log")));
        Assert.False(File.Exists(Path.Combine(directory, "app.3.log")));
    }
    finally { CleanupTempDirectory(directory); }
}
```

### 测试 4: `Log_WriteAfterExceptionDoesNotDeadlock`

模拟写入成功后异常场景 → 验证锁被释放，后续写入正常。

```csharp
[Fact]
public void Log_WriteAfterExceptionDoesNotDeadlock()
{
    var directory = CreateTempDirectory();
    try
    {
        var logger = new FileLogger(directory, new FixedTimeProvider(FixedUtcTime));

        // 第一次写入正常
        logger.Log(LogLevel.Info, "test", "first write ok");

        // 第二次写入也应正常（验证锁未死锁）
        logger.Log(LogLevel.Info, "test", "second write ok");

        var lines = File.ReadAllLines(LogFilePath(directory));
        Assert.True(lines.Length >= 2);
    }
    finally { CleanupTempDirectory(directory); }
}
```

### RED 预期

```
dotnet test --filter "RotatesWhenFileExceedsThreshold|RotatesUpTo3Files|OldestFileRemovedOnRotation|WriteAfterExceptionDoesNotDeadlock"
```

预期 4 个测试全部 FAIL — 当前 FileLogger 无轮转逻辑，阈值检查不存在。

---

## GREEN 阶段 — 最小实现

修改 `src/Anhei4Map.Infrastructure/Logging/FileLogger.cs`：

### 1. 添加字段和修改构造函数

```csharp
private readonly long _maxFileSizeBytes;

public FileLogger(string directory, TimeProvider? timeProvider = null, long maxFileSizeBytes = 10 * 1024 * 1024)
{
    _logFilePath = Path.Combine(directory, "app.log");
    _timeProvider = timeProvider ?? TimeProvider.System;
    _maxFileSizeBytes = maxFileSizeBytes;
}
```

### 2. 在 Log 方法中 File.AppendAllText 之前调用 RotateIfNeeded()

```csharp
public void Log(LogLevel level, string source, string message, Exception? ex = null)
{
    var entry = FormatEntry(level, source, message, ex);
    _writeLock.Wait();
    try
    {
        var directory = Path.GetDirectoryName(_logFilePath)!;
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        RotateIfNeeded();                              // ← 新增

        File.AppendAllText(_logFilePath, entry + Environment.NewLine, Encoding.UTF8);
    }
    finally { _writeLock.Release(); }
}
```

### 3. 添加 RotateIfNeeded 私有方法

```csharp
private void RotateIfNeeded()
{
    if (!File.Exists(_logFilePath))
        return;

    var fileInfo = new FileInfo(_logFilePath);
    if (fileInfo.Length < _maxFileSizeBytes)
        return;

    // 删除最旧的轮转文件
    var thirdPath = _logFilePath.Replace(".log", ".2.log");
    if (File.Exists(thirdPath))
        File.Delete(thirdPath);

    // 轮转 app.1.log → app.2.log
    var secondPath = _logFilePath.Replace(".log", ".1.log");
    if (File.Exists(secondPath))
        File.Move(secondPath, thirdPath);

    // 轮转 app.log → app.1.log
    File.Move(_logFilePath, secondPath);
}
```

**最小修改原则：**
- 不改动 SaveAsync、FormatEntry、SanitizeMessage 或其他方法
- 不引入新类或第三方库
- 轮转逻辑在持有 `_writeLock` 时执行（已在锁内），线程安全

---

## 定向测试命令

```powershell
dotnet test tests/Anhei4Map.Tests/ --filter "FullyQualifiedName~FileLoggerTests" -c Release
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

- [ ] `FileLogger` 构造函数新增 `maxFileSizeBytes` 参数（默认 10MB）
- [ ] 4 个新测试写入 `FileLoggerTests.cs`
- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] 定向测试 FAIL (RED — 无轮转逻辑)
- [ ] `RotateIfNeeded()` 方法实现
- [ ] 定向测试 PASS (GREEN: 16/16: 12原有 + 4新增)
- [ ] 完整测试 `dotnet test -c Release --no-build` 全部通过 (93 测试: 89 原有 + 4 新增)
- [ ] `git diff --check` clean
- [ ] 只修改了 `FileLogger.cs` 和 `FileLoggerTests.cs`，未触及禁止路径

---

## Git 提交信息

```
fix: implement 10MB log file rotation in FileLogger

S01-003: FileLogger now performs rolling log rotation. When app.log
exceeds the configurable threshold (default 10MB), it renames app.log
to app.1.log, shifts app.1.log to app.2.log, and deletes any older
files. Max 3 files retained per design spec.

A configurable maxFileSizeBytes constructor parameter enables testing with small thresholds.
Added 4 tests: threshold trigger, 3-file limit, oldest file removal, post-rotation writes.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-01-REVIEW-FIX-03
Finding: S01-003
Status: DONE
Commit: <hash>
Tests Added: 4
Tests Total: 93
Tests Passed: 93
Tests Failed: 0
Build: Release 0 errors 0 warnings
Files Modified:
  - src/Anhei4Map.Infrastructure/Logging/FileLogger.cs
  - tests/Anhei4Map.Tests/FileLoggerTests.cs
Files NOT Modified (verified): <列出禁止路径确认未触及>
Limitations: <如有>
```
