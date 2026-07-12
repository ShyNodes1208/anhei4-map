# Stage 01 Fixes — Codex Review 裁决后修复

> **Cursor:** 只按本文件逐任务执行。一次一个任务。每完成一个任务提交一次。不得改需求、架构和范围。

**来源:** docs/reviews/stage-01-adjudication.md
**裁决日期:** 2026-07-12
**分支:** feature/01-foundation

---

## 修复优先级

| 序号 | 发现 | 严重级别 | 阻塞合并 |
|------|------|----------|----------|
| FIX-01 | LoadAsync 调用 Validate() | MEDIUM | 是 | DONE (aac8777) |
| FIX-02 | DomainPolicy 检查 UserInfo | MEDIUM | 是 | DONE (bc8dfec) |
| FIX-03 | FileLogger 10MB 轮转 | MEDIUM | 是 | DONE (10aba4e) |
| FIX-04 | WindowBounds 多显示器 max size | MEDIUM | 是 | DONE (a8d7411) |
| FIX-05 | 备份时间戳精度 | LOW | 否 | DONE_AFTER_FIX1 (96f01e6→d599ca7) |
| FIX-06 | UTF-8 测试恒真断言 | LOW | 否 |

---
---

### FIX-01: LoadAsync 后调用 Validate()

**文件:**
- 修改: `src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs`
- 修改: `tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs`

**实现:**
1. 在 `JsonSettingsStore.LoadAsync()` 中，反序列化成功且非 null 后，调用 `settings.Validate()`
2. `Validate()` 返回 `false` 时 → `BackupCorruptedFile()` + 返回 `AppSettings.CreateDefaults()`
3. 补充测试（添加到 `JsonSettingsStoreTests.cs`）:
   - `Load_WidthZero_ReturnsDefaults`: 保存合法 JSON 但 Width=0 → Load 返回默认值 + 创建 .bak
   - `Load_OpacityAboveOne_ReturnsDefaults`: 保存合法 JSON 但 Opacity=1.5 → Load 返回默认值
   - `Load_ZoomLevelOutOfRange_ReturnsDefaults`: 保存合法 JSON 但 ZoomLevel=10 → Load 返回默认值
   - `Load_HeightBelowMin_ReturnsDefaults`: 保存合法 JSON 但 Height=100 → Load 返回默认值

**验证:** `dotnet build -c Release` 0 错误 0 警告; `dotnet test -c Release --no-build` 全部通过

---
---

### FIX-02: DomainPolicy 拒绝非空 UserInfo

**文件:**
- 修改: `src/Anhei4Map.Core/Services/DomainPolicy.cs`
- 修改: `tests/Anhei4Map.Tests/DomainPolicyTests.cs`

**实现:**
1. 在 `DomainPolicy.IsAllowed()` 中，host 检查之前添加:
   ```csharp
   if (!string.IsNullOrEmpty(parsed.UserInfo))
       return false;
   ```
2. 补充测试:
   - `Blocks_UserinfoOnRootDomain`: `https://user:pass@helltides.com/` → false
   - `Blocks_UserinfoOnWwwDomain`: `https://user:pass@www.helltides.com/` → false
   - `Blocks_UserinfoOnLegitPath`: `https://x@helltides.com/map` → false

**验证:** `dotnet build -c Release` 0 错误 0 警告; `dotnet test -c Release --no-build` 全部通过

---
---

### FIX-03: FileLogger 10MB 轮转 (最多 3 文件)

**文件:**
- 修改: `src/Anhei4Map.Infrastructure/Logging/FileLogger.cs`
- 修改: `tests/Anhei4Map.Tests/FileLoggerTests.cs`

**实现:**
1. 在 `FileLogger` 中添加:
   - 常量 `MaxFileSizeBytes = 10 * 1024 * 1024` (10MB)
   - 常量 `MaxRotatedFiles = 3`
   - 私有方法 `RotateIfNeeded()`: 检查 `app.log` 大小 ≥ 10MB 时:
     - 删除 `app.2.log`（如果存在）
     - 重命名 `app.1.log` → `app.2.log`（如果存在）
     - 重命名 `app.log` → `app.1.log`
   - 在 `Log()` 方法中 `File.AppendAllText` 之前调用 `RotateIfNeeded()`
   - `RotateIfNeeded` 需要在持有 `_writeLock` 时调用（已在锁内）

2. 补充测试（添加到 `FileLoggerTests.cs`）:
   - `Log_RotatesWhenFileExceeds10MB`: 写入超过 10MB 数据 → 验证创建 app.1.log
   - `Log_RotatesUpTo3Files`: 连续写入超过 30MB → 验证只有 app.log, app.1.log, app.2.log（无 app.3.log）
   - `Log_OldestRotatedFileRemoved`: 验证轮转时最旧文件被淘汰
   - `Log_WriteAfterExceptionDoesNotDeadlock`: 模拟写入异常后再次写入 → 验证不永久死锁

**注意:** 轮转阈值测试可使用较小的文件或通过注入 `FileInfo` 控制大小。如果 10MB 测试太重（慢），可引入内部可配置的阈值（如构造函数参数 `maxFileSizeBytes`，默认 10MB）并测试小阈值。

**验证:** `dotnet build -c Release` 0 错误 0 警告; `dotnet test -c Release --no-build` 全部通过

---
---

### FIX-04: WindowBoundsNormalizer 根据窗口所在显示器决定 max size

**文件:**
- 修改: `src/Anhei4Map.Core/Services/WindowBoundsNormalizer.cs`
- 修改: `tests/Anhei4Map.Tests/WindowBoundsNormalizerTests.cs`

**实现:**
1. 添加私有辅助方法 `FindBestWorkArea(left, top, width, height, workAreas)`:
   - 返回与窗口交集面积最大的 WorkArea
   - 如果没有任何交集（或交集为 0），返回 `workAreas[0]`（回退到主显示器）
2. 修改 `Normalize()` 中的 max size 裁剪:
   - 将 `if (width > primary.Width)` → `if (width > bestArea.Width)`
   - 将 `if (height > primary.Height)` → `if (height > bestArea.Height)`
   - 但保留 `primary` 用于第 59-64 行的 fallback reset（重置到主显示器是正确行为）

3. 补充测试:
   - `WidthExceedsSecondaryMonitor_ClampedToSecondary`: 副显示器 2560×1440，主显示器 1920×1080，窗口在副显示器上 Width=3000 → 裁剪为 2560
   - `HeightExceedsSecondaryMonitor_ClampedToSecondary`: 同上场景，Height=2000 → 裁剪为 1440
   - `ValidOnLargerSecondaryMonitor_Unchanged`: 副显示器大于主显示器时，窗口在副显示器上合法尺寸不变
   - `NegativeOriginLargerSecondary_Unchanged`: 负坐标 + 副显示器尺寸大于主显示器时，合法窗口不变

**验证:** `dotnet build -c Release` 0 错误 0 警告; `dotnet test -c Release --no-build` 全部通过

---
---

### FIX-05: 备份时间戳毫秒精度

**文件:**
- 修改: `src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs`
- 修改: `tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs`

**实现:**
1. 将 `BackupCorruptedFile()` 中的时间戳格式从 `"yyyyMMdd'T'HHmmss"` 改为 `"yyyyMMdd'T'HHmmssfff"`（毫秒精度）
2. 补充测试:
   - `Load_CorruptedJson_BakAlreadyExists_TimestampedBakCreated`: 已有 .bak + .bak.{timestamp} 均存在时 → 第三个损坏文件成功备份（不抛异常）

**验证:** `dotnet build -c Release` 0 错误 0 警告; `dotnet test -c Release --no-build` 全部通过

---
---

### FIX-06: 修复 UTF-8 测试恒真断言

**文件:**
- 修改: `tests/Anhei4Map.Tests/FileLoggerTests.cs`

**实现:**
1. 删除 [FileLoggerTests.cs:181](tests/Anhei4Map.Tests/FileLoggerTests.cs#L181) 的恒真断言
2. 替换为明确的断言: 验证写入的文件不包含 UTF-8 BOM（`0xEF, 0xBB, 0xBF`）
   ```csharp
   Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
       "File should not contain UTF-8 BOM");
   ```

**验证:** `dotnet build -c Release` 0 错误 0 警告; `dotnet test -c Release --no-build` 全部通过

---
---

## 完成标准

- [ ] FIX-01 至 FIX-04 全部完成（阻塞项）
- [ ] FIX-05、FIX-06 完成（建议项）
- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] `dotnet test -c Release --no-build` 全部通过（预期 90+ 测试）
- [ ] `git diff --check` clean
- [ ] 生成新的测试输出文件 `docs/reviews/stage-01-test-output.txt`
