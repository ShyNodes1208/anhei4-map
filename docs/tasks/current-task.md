# 当前任务

## 状态
READY

## 类型
FIX

## 修复编号
STAGE-01-REVIEW-FIX-05-FIX1

## 对应 Codex 发现
S01-005 (LOW)

## 父任务
STAGE-01-REVIEW-FIX-05 (96f01e6 — 已实现但 GUID 后缀不足)

## 阶段
STAGE-01-FOUNDATION (修复轮)

## 分支
feature/01-foundation

## Worktree
D:\AIProjects\anhei4-map-worktrees\stage-01-foundation

---

## 返工原因

父任务实现 (96f01e6) 使用 `Guid.NewGuid().ToString("N")[..8]`，仅 8 个十六进制字符作为唯一后缀（~32 位随机空间）。无碰撞重试循环。不满足"杜绝备份名称碰撞"的规格要求。

**裁决:** 不能用"概率很低"替代确定性唯一性要求。

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
- 不改变第一份备份命名规则（settings.json.bak 仍正确）
- 不删除旧备份
- 不修改 load/save/validate 逻辑

---

## RED 阶段 — 调整测试

### 1. 修改 `IsUniqueTimestampedBackup` 辅助方法

将 `<test-file>:49` 的 `suffix.Length == 8` 改为 `suffix.Length == 32`：

```csharp
private static bool IsUniqueTimestampedBackup(string fileName)
{
    const string prefix = "settings.json.bak.";
    if (!fileName.StartsWith(prefix, StringComparison.Ordinal))
        return false;

    var remainder = fileName[prefix.Length..];
    var separator = remainder.LastIndexOf('.');
    if (separator <= 0 || separator >= remainder.Length - 1)
        return false;

    var timestamp = remainder[..separator];
    var suffix = remainder[(separator + 1)..];
    return timestamp.EndsWith("Z", StringComparison.Ordinal)
        && suffix.Length == 32;  // ← 8 → 32
}
```

### 2. 新增测试: `Backup_UsesFullGuidSuffix`

验证后缀为 32 位十六进制：

```csharp
[Fact]
public async Task Backup_UsesFullGuidSuffix()
{
    var directory = CreateTempDirectory();
    try
    {
        await File.WriteAllTextAsync(BaseBakPath(directory), "existing");
        await File.WriteAllTextAsync(SettingsPath(directory), "{ corrupt }");
        var store = new JsonSettingsStore(directory);
        await store.LoadAsync();

        var additional = BackupFiles(directory)
            .Where(p => !string.Equals(p, BaseBakPath(directory), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Single(additional);
        var fileName = Path.GetFileName(additional[0]);
        Assert.True(IsUniqueTimestampedBackup(fileName));
    }
    finally { CleanupTempDirectory(directory); }
}
```

### RED 预期

`dotnet test --filter "FullyQualifiedName~JsonSettingsStoreTests" -c Release`

预期 `Backup_UsesFullGuidSuffix` FAIL — 当前后缀仅 8 字符，`IsUniqueTimestampedBackup` 修改后（要求 32 字符）或新增正则测试会失败。

---

## GREEN 阶段 — 最小修复

**只修改一行：** [JsonSettingsStore.cs:105](src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs#L105)

```csharp
// 改前:
var suffix = Guid.NewGuid().ToString("N")[..8];

// 改后:
var suffix = Guid.NewGuid().ToString("N");
```

**完整方法应变为：**

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
    var suffix = Guid.NewGuid().ToString("N");
    var uniquePath = Path.Combine(directory, $"settings.json.bak.{timestamp}.{suffix}");
    File.Move(_filePath, uniquePath);
}
```

不改动其他任何代码。

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

- [ ] `JsonSettingsStore.cs:105` 改为完整 GUID（移除 `[..8]`）
- [ ] `IsUniqueTimestampedBackup` 改为要求 `suffix.Length == 32`
- [ ] `Backup_UsesFullGuidSuffix` 测试新增或现有测试验证通过
- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] 定向测试 PASS (26/26)
- [ ] 完整测试 PASS (121/121)
- [ ] `git diff --check` clean
- [ ] 只修改了允许的两个文件
- [ ] 不处理 FIX-06

---

## Git 提交信息

```
fix: strengthen settings backup uniqueness with full GUID suffix

S01-005-FIX1: Replace 8-character GUID truncation with the full
32-character GUID to guarantee unique backup filenames. A truncated
8-char suffix provides only ~32 bits of randomness and risks collision
without retry logic. Also updated IsUniqueTimestampedBackup validator
to accept 32-char suffixes.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-01-REVIEW-FIX-05-FIX1
Parent Fix: STAGE-01-REVIEW-FIX-05 (96f01e6)
Finding: S01-005
Status: DONE
Commit: <hash>
Tests Added: 1 (Backup_UsesFullGuidSuffix)
Tests Modified: IsUniqueTimestampedBackup (8 → 32)
Tests Total: 121
Tests Passed: 121
Tests Failed: 0
Build: Release 0 errors 0 warnings
Files Modified:
  - src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs
  - tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs
Files NOT Modified (verified): <列出>
Limitations: None
```
