# 当前任务

## 状态
READY

## 类型
FIX

## 修复编号
STAGE-01-REVIEW-FIX-05

## 对应 Codex 发现
S01-005 (LOW)

## 阶段
STAGE-01-FOUNDATION (修复轮)

## 分支
feature/01-foundation

## Worktree
D:\AIProjects\anhei4-map-worktrees\stage-01-foundation

---

## 问题证据

[JsonSettingsStore.cs:97-100](src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs#L97-L100):

```csharp
var timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss");
var timestampedBakPath = Path.Combine(directory, $"settings.json.bak.{timestamp}");
File.Move(_filePath, timestampedBakPath);
```

`BackupCorruptedFile()` 在 `.bak` 已存在时使用秒级时间戳。同一秒内第二次损坏 → 相同文件名 → `File.Move` 抛 `IOException`（目标已存在） → 损坏文件无法备份。

---

## 允许修改的精确路径

- `src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs`
- `tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs`

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.App/**`
- `src/Anhei4Map.Infrastructure/Logging/**`
- `docs/design/**`
- `docs/plans/**`
- `*.csproj`
- `*.sln`
- 不删除旧备份
- 不修改设置校验语义
- 不新增第三方依赖

---

## 备份命名规则

### 第一份备份

始终使用: `settings.json.bak`

### 后续备份

当 `settings.json.bak` 已存在时，使用带唯一后缀的名称:

```
settings.json.bak.<UTC毫秒时间戳>.<GUID后缀>
```

- 时间戳格式: `yyyyMMddTHHmmssfffZ`（毫秒精度，UTC，Z 后缀）
- GUID 后缀: `Guid.NewGuid().ToString("N")[..8]`（取 GUID 前 8 个十六进制字符）

### 不覆盖规则

- 不得以 `overwrite: true` 覆盖已有备份
- 每次 `BackupCorruptedFile()` 必须创建**独立的新文件**
- 如果目标路径恰好已存在（极端碰撞），`File.Move` 的 `IOException` 自然传播

### 适用范围

- JSON 语法损坏 (`JsonException`) → 使用相同唯一备份策略
- 语义校验失败 (`Validate() == false`) → 使用相同唯一备份策略
- 两种路径都通过 `BackupCorruptedFile()` 处理，无需区分

### 旧备份保留

- 不删除任何旧备份文件
- 不限制备份总数上限（LOW severity，不引入复杂性换有限磁盘节省）

### 异常传播

- `File.Move` 失败 → 异常向 `LoadAsync()` 调用方传播
- 不在 `BackupCorruptedFile()` 内吞异常

---

## RED 阶段 — 先写失败测试

### 测试 1: `Backup_UsesBaseBakNameForFirstCorruption`

首次损坏 → 创建 `settings.json.bak`，内容匹配原始损坏 JSON。

### 测试 2: `Backup_ExistingBaseBak_CreatesUniqueAdditionalBackup`

预创 `settings.json.bak`，写入新的损坏 `settings.json` → 新备份使用带时间戳和 GUID 的文件名，旧 `.bak` 未被覆盖。

### 测试 3: `Backup_MultipleBackups_AreAllUnique`

快速连续 3 次损坏 → 每次创建独立备份文件，文件名全部不同，内容分别保留。

```csharp
[Fact]
public async Task Backup_MultipleBackups_AreAllUnique()
{
    var directory = CreateTempDirectory();
    try
    {
        var store = new JsonSettingsStore(directory);

        // First: writes corrupted JSON to settings.json, load → backup
        await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), "{ corrupt A }");
        await store.LoadAsync();

        // Second: new corrupted content
        await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), "{ corrupt B }");
        await store.LoadAsync();

        // Third: new corrupted content
        await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), "{ corrupt C }");
        await store.LoadAsync();

        var bakFiles = Directory.GetFiles(directory, "settings.json.bak*");
        Assert.True(bakFiles.Length >= 3);
        // All filenames are distinct
        Assert.Equal(bakFiles.Distinct().Count(), bakFiles.Length);
    }
    finally { CleanupTempDirectory(directory); }
}
```

### 测试 4: `Backup_ExistingTimestampLikeBak_IsNotOverwritten`

预创 `settings.json.bak.20260712T120000000Z.abc12345` → 新损坏加载不覆盖该文件。

### 测试 5: `Backup_EveryBackupPreservesOriginalBytes`

每轮写入不同损坏内容 → 验证每个备份文件分别保留当次原始内容。

### 测试 6: `Backup_SemanticInvalidSettings_UsesUniqueBackupPolicy`

格式合法但 Validate()=false（如 Width=0）→ 也使用唯一备份文件名。

### 测试 7: `Backup_SyntaxCorrupted_UsesUniqueBackupPolicy`

JSON 语法错误 → 也使用唯一备份文件名。

### 测试 8: `Backup_ExistingManyBackups_DoesNotDeleteOldBackups`

预创 5 个旧 `settings.json.bak.*` 文件 → 新损坏加载后旧备份全部保留。

### RED 预期

```
dotnet test --filter "FullyQualifiedName~JsonSettingsStoreTests" -c Release
```

预期新增测试 FAIL — 当前秒级时间戳在同一秒内多次损坏时产生相同文件名导致冲突。

---

## GREEN 阶段 — 最小实现

修改 `BackupCorruptedFile()` 方法：

```csharp
private void BackupCorruptedFile()
{
    if (!File.Exists(_filePath))
        return;

    if (!File.Exists(_bakFilePath))
    {
        File.Move(_filePath, _bakFilePath);
        return;
    }

    var directory = Path.GetDirectoryName(_filePath)!;
    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'");
    var suffix = Guid.NewGuid().ToString("N")[..8];
    var uniquePath = Path.Combine(directory, $"settings.json.bak.{timestamp}.{suffix}");
    File.Move(_filePath, uniquePath);
}
```

**变更：** 仅修改 `BackupCorruptedFile()` 方法中的时间戳格式和文件名生成逻辑。不改动 `LoadAsync`、`SaveAsync`、`CleanupResidualTmp` 或其他方法。

---

## 定向测试命令

```powershell
dotnet test tests/Anhei4Map.Tests/ --filter "FullyQualifiedName~JsonSettingsStoreTests" -c Release
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

- [ ] `BackupCorruptedFile()` 修改为毫秒时间戳 + GUID 后缀
- [ ] 8 个新测试写入 `JsonSettingsStoreTests.cs`
- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] 定向测试 FAIL (RED)
- [ ] 定向测试 PASS (GREEN: 26/26: 18原有 + 8新增)
- [ ] 完整测试 `dotnet test -c Release --no-build` 全部通过 (121: 113原有 + 8新增)
- [ ] `git diff --check` clean
- [ ] 只修改了 `JsonSettingsStore.cs` 和 `JsonSettingsStoreTests.cs`

---

## Git 提交信息

```
fix: prevent settings backup name collisions

S01-005: BackupCorruptedFile now uses millisecond-precision UTC timestamps
with a GUID suffix to guarantee unique backup filenames. Previously,
second-precision timestamps could collide within the same second when
.bak already existed, causing File.Move to fail with IOException.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-01-REVIEW-FIX-05
Finding: S01-005
Status: DONE
Commit: <hash>
Tests Added: 8
Tests Total: 121
Tests Passed: 121
Tests Failed: 0
Build: Release 0 errors 0 warnings
Files Modified:
  - src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs
  - tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs
Files NOT Modified (verified): <列出禁止路径确认未触及>
Limitations: <如有>
```
