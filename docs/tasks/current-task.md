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
每次 `Log()` 仅执行 `File.AppendAllText`，**不存在**文件大小检查、`app.1.log` 重命名或轮转淘汰逻辑。

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

## 日志轮转精确规格

### 1. 生产阈值

生产默认最大日志文件大小: `10 * 1024 * 1024` bytes，即 `10485760` bytes。

构造函数新增可选参数 `long maxFileSizeBytes = 10485760`（默认值必须精确等于 `10 * 1024 * 1024`）。

`maxFileSizeBytes` 必须 > 0，否则构造函数抛出 `ArgumentOutOfRangeException(nameof(maxFileSizeBytes))`。

测试通过传入较小阈值触发轮转。

### 2. 完整日志记录大小

轮转判断必须使用**完整格式化日志记录**的 UTF-8 实际字节数。必须包含：时间戳、日志级别、source、message、异常类型、异常消息、异常堆栈、行结束符。

不得使用 `string.Length`。必须使用与实际写入 `File.AppendAllText` 一致的 UTF-8 `Encoding.UTF8.GetByteCount()` 计算字节数。

### 3. 轮转判断公式

在写入前计算：

```
projectedSize = currentSize + entryBytes
```

其中 `currentSize` 为 `app.log` 当前字节数（不存在则为 0），`entryBytes` 为格式化后完整日志记录（含换行符）的 UTF-8 字节数。

**只有同时满足**以下两个条件时才执行轮转：

1. `currentSize > 0`
2. `projectedSize > maxFileSizeBytes`

**不轮转的情况：**

- `currentSize == 0`（空文件）→ 直接写入，不轮转。
- `projectedSize <= maxFileSizeBytes` → 直接追加，不轮转。
- **`projectedSize == maxFileSizeBytes` → 不轮转，直接追加。**（精确等于阈值时保留在活动文件中）

### 4. 单条超大日志规则

如果单条完整日志记录本身 `entryBytes > maxFileSizeBytes`：

- 不拆分
- 不截断
- 不丢弃
- 不返回"日志过大"业务错误
- 允许本次写入后 `app.log` 超过阈值
- 同一次 `Log()` 调用最多执行一次轮转
- 写入完成后不在同一次调用内再次轮转
- 不允许循环轮转

### 5. 空文件加超大日志

如果 `app.log` 不存在或 `app.log` 大小为 0，新日志本身超过阈值：

- 直接将完整日志写入 `app.log`
- 不轮转空文件
- 不创建空的 `app.1.log`

### 6. 超大日志后的下一次写入

如果当前 `app.log` 因上一条超大日志已超过阈值：

- 下一条日志写入前执行一次正常轮转
- 原超大日志移动到 `app.1.log`
- 新日志写入新的 `app.log`
- 不重复轮转
- 不死锁

### 7. 文件命名和保留规则

| 文件 | 角色 |
|------|------|
| `app.log` | 活动文件 |
| `app.1.log` | 第一份历史文件 |
| `app.2.log` | 第二份历史文件 |

最多保留 3 个文件（含 `app.log`）。

**轮转顺序（按此精确顺序执行）：**

1. 如果 `app.2.log` 存在 → 删除 `app.2.log`
2. 如果 `app.1.log` 存在 → `File.Move(app.1.log, app.2.log)`
3. `File.Move(app.log, app.1.log)`
4. 将当前新记录写入新的 `app.log`

不得静默覆盖已有文件。移动和删除失败必须向调用方传播。

### 8. 并发规则

以下操作必须处于**同一个 `SemaphoreSlim` 临界区**内：

- 获取当前 `app.log` 大小
- 格式化日志
- 计算 UTF-8 字节数
- 判断是否轮转
- 删除最旧日志
- 移动历史日志
- 移动活动日志
- 追加新日志

`SemaphoreSlim.Release()` 必须位于 `finally` 块。任何异常发生后都不得永久占用锁。

### 9. 失败规则

以下错误不得吞掉，必须向调用方传播：

- `Directory.CreateDirectory` 失败
- 文件大小读取失败
- 文件删除失败
- 文件移动失败
- 文件追加失败
- `UnauthorizedAccessException`
- `IOException`

不得在 `FileLogger` 内递归记录轮转失败。

---

## RED 阶段 — 先写失败测试

测试使用小阈值（如 256 bytes）触发轮转，不实际写 10MB。使用临时目录，finally 中清理。不写真实 `LocalApplicationData`。

### 测试 1: `DefaultMaxFileSize_IsTenMegabytes`

验证生产默认值为 `10 * 1024 * 1024`。非 public 字段可通过反射或间接测试。

### 测试 2: `Append_WhenProjectedSizeBelowLimit_DoesNotRotate`

写入后 `currentSize + entryBytes < threshold` → 不轮转，无 `app.1.log`。

### 测试 3: `Append_WhenProjectedSizeEqualsLimit_DoesNotRotate`

写入后 `currentSize + entryBytes == threshold` → 不轮转，无 `app.1.log`。精确等于阈值不触发轮转。

### 测试 4: `Append_WhenProjectedSizeExceedsLimit_RotatesBeforeWriting`

`currentSize > 0` 且 `currentSize + entryBytes > threshold` → 先轮转再写新日志。`app.1.log` 存在。

### 测试 5: `Rotation_PreservesOldContent`

验证旧 `app.log` 内容完整存在于 `app.1.log`。

### 测试 6: `Rotation_NewActiveLogContainsOnlyNewEntry`

验证新的 `app.log` 不包含旧活动文件内容。

### 测试 7: `Rotation_ShiftsExistingFilesWithoutOverwriting`

预创 `app.log`、`app.1.log`、`app.2.log`，再次轮转时验证：`app.2.log` 被删除、`app.1.log` → `app.2.log`、原 `app.log` → `app.1.log`、新记录进入新 `app.log`。

### 测试 8: `EmptyFile_OversizedEntry_WritesWithoutRotatingEmptyFile`

空文件 + 单条超大日志 → 直接写入，不创建空 `app.1.log`。

### 测试 9: `ExistingContent_OversizedEntry_RotatesOnceThenWritesCompleteEntry`

已有内容 + 单条超大日志 → 轮转一次，完整写入超大日志（不截断）。

### 测试 10: `WriteAfterOversizedEntry_RotatesOversizedActiveFileOnce`

上一条超大日志导致 `app.log` 超阈值 → 下一条普通日志写入前正常轮转一次，原超大日志进入 `app.1.log`。

### 测试 11: `Rotation_UsesUtf8ByteCount_NotCharacterCount`

使用中文或多字节字符（如"日志"），验证按 UTF-8 字节数判断而非字符数。

### 测试 12: `ConcurrentWritesNearLimit_DoNotLoseEntriesOrDeadlock`

并发写入接近阈值时验证：全部调用完成、无死锁、日志总数不丢失、无重复轮转异常、文件数量不超过 3 个。

### 测试 13: `RotationFailure_PropagatesException`

模拟文件删除/移动/写入失败 → 验证异常向调用方传播。

### 测试 14: `Constructor_ThrowsOnNonPositiveMaxFileSize`

`maxFileSizeBytes <= 0` → `ArgumentOutOfRangeException`。

### RED 预期

```
dotnet test --filter "FullyQualifiedName~FileLoggerTests" -c Release
```

预期新增测试全部 FAIL — 当前 FileLogger 无轮转逻辑，无 `maxFileSizeBytes` 参数。

---

## GREEN 阶段 — 最小实现

### 1. 修改构造函数

```csharp
private readonly long _maxFileSizeBytes;

public FileLogger(string directory, TimeProvider? timeProvider = null, long maxFileSizeBytes = 10 * 1024 * 1024)
{
    if (maxFileSizeBytes <= 0)
        throw new ArgumentOutOfRangeException(nameof(maxFileSizeBytes));

    _logFilePath = Path.Combine(directory, "app.log");
    _timeProvider = timeProvider ?? TimeProvider.System;
    _maxFileSizeBytes = maxFileSizeBytes;
}
```

### 2. 在 Log 方法中 File.AppendAllText 之前调用 RotateIfNeeded

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

        RotateIfNeeded(entry);                          // ← 新增

        File.AppendAllText(_logFilePath, entry + Environment.NewLine, Encoding.UTF8);
    }
    finally { _writeLock.Release(); }
}
```

### 3. 添加 RotateIfNeeded 私有方法（严格按规格实现）

```csharp
private void RotateIfNeeded(string entry)
{
    // 计算完整日志记录的 UTF-8 字节数（含换行符）
    var entryLine = entry + Environment.NewLine;
    var entryBytes = Encoding.UTF8.GetByteCount(entryLine);

    // 获取当前文件大小
    long currentSize = 0;
    if (File.Exists(_logFilePath))
        currentSize = new FileInfo(_logFilePath).Length;

    // projectedSize == maxFileSizeBytes 时不轮转
    // currentSize == 0 时不轮转（空文件或不存在）
    if (currentSize == 0 || currentSize + entryBytes <= _maxFileSizeBytes)
        return;

    var directory = Path.GetDirectoryName(_logFilePath)!;

    // 1. 如果 app.2.log 存在 → 删除
    var thirdPath = Path.Combine(directory, "app.2.log");
    if (File.Exists(thirdPath))
        File.Delete(thirdPath);

    // 2. 如果 app.1.log 存在 → 移动为 app.2.log
    var secondPath = Path.Combine(directory, "app.1.log");
    if (File.Exists(secondPath))
        File.Move(secondPath, thirdPath);

    // 3. app.log → app.1.log
    File.Move(_logFilePath, secondPath);
}
```

### 最小修改原则

- 不改动 `FormatEntry`、`SanitizeMessage` 或其他方法
- 不引入新类或第三方库
- 轮转逻辑在持有 `_writeLock` 时执行（已在锁内），线程安全
- 失败不吞异常，不递归记录

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

- [ ] `FileLogger` 构造函数新增 `maxFileSizeBytes` 参数（默认 `10*1024*1024`）
- [ ] `maxFileSizeBytes <= 0` 抛出 `ArgumentOutOfRangeException`
- [ ] 14 个新测试写入 `FileLoggerTests.cs`
- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] 定向测试 FAIL (RED — 无轮转逻辑)
- [ ] `RotateIfNeeded()` 方法严格按规格实现
- [ ] 定向测试 PASS (GREEN: 26/26: 12原有 + 14新增)
- [ ] 完整测试 `dotnet test -c Release --no-build` 全部通过 (103 测试: 89 原有 + 14 新增)
- [ ] `git diff --check` clean
- [ ] 只修改了 `FileLogger.cs` 和 `FileLoggerTests.cs`，未触及禁止路径

---

## Git 提交信息

```
fix: implement 10MB log file rotation in FileLogger

S01-003: FileLogger now performs rolling log rotation with a configurable
threshold (default 10MB). Uses UTF-8 byte count for accurate size checks.
Oversized single entries are written without truncation and trigger
rotation on the next write. ProjectedSize == threshold does NOT rotate.
Max 3 files retained. All rotation operations are inside the SemaphoreSlim
critical section. Rotation/deletion/move failures propagate to caller.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-01-REVIEW-FIX-03
Finding: S01-003
Status: DONE
Commit: <hash>
Tests Added: 14
Tests Total: 103
Tests Passed: 103
Tests Failed: 0
Build: Release 0 errors 0 warnings
Files Modified:
  - src/Anhei4Map.Infrastructure/Logging/FileLogger.cs
  - tests/Anhei4Map.Tests/FileLoggerTests.cs
Files NOT Modified (verified): <列出禁止路径确认未触及>
Limitations: <如有>
```
