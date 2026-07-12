# 当前任务

## 状态
READY

## 类型
FIX

## 修复编号
STAGE-01-REVIEW-FIX-01

## 对应 Codex 发现
S01-001 (MEDIUM)

## 阶段
STAGE-01-FOUNDATION (修复轮)

## 分支
feature/01-foundation

## Worktree
D:\AIProjects\anhei4-map-worktrees\stage-01-foundation

---

## 问题证据

[JsonSettingsStore.cs:58-68](src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs#L58-L68):

```csharp
var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
if (settings is null)
{
    BackupCorruptedFile();
    return AppSettings.CreateDefaults();
}
return settings;  // ← 未调用 settings.Validate()
```

`AppSettings.Validate()` 已实现 ([AppSettings.cs:51-74](src/Anhei4Map.Core/Models/AppSettings.cs#L51-L74))，检查 Width≥200、Height≥150、Opacity 0.1-1.0、ZoomLevel 0.25-5.0。但 `LoadAsync` 反序列化成功后从未调用它。

[stage-01-foundation-plan.md:84](docs/plans/stage-01-foundation-plan.md#L84): "Invalid values (Width=0) → use defaults"

现有测试 `Load_CorruptedJson_ReturnsDefaults` 仅覆盖语法损坏 JSON（`{ not valid json }}}`），未覆盖格式合法但值无效的 JSON（如 `Width=0`）。

---

## 允许修改的精确路径

- `src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs`
- `tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs`

## 禁止修改范围

- `src/Anhei4Map.Core/**` (含 AppSettings.cs、DomainPolicy.cs、RetryPolicy.cs、WindowBoundsNormalizer.cs)
- `src/Anhei4Map.App/**`
- `src/Anhei4Map.Infrastructure/Logging/**`
- `docs/design/**`
- `docs/plans/**`
- `*.csproj`
- `*.sln`
- 其他所有测试文件

---

## RED 阶段 — 先写失败测试

在 `tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs` 中新增以下测试方法。

**每个测试写入格式合法但值无效的 JSON 到 `settings.json`，然后 `LoadAsync()`，断言返回默认值且创建 .bak。**

### 测试 1: `Load_WidthZero_ReturnsDefaults`

```csharp
[Fact]
public async Task Load_WidthZero_ReturnsDefaults()
{
    var directory = CreateTempDirectory();
    try
    {
        var defaults = AppSettings.CreateDefaults();
        var invalidJson = JsonSerializer.Serialize(
            defaults with { Placement = defaults.Placement with { Width = 0 } });
        await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), invalidJson);

        var store = new JsonSettingsStore(directory);
        var loaded = await store.LoadAsync();

        Assert.Equal(640, loaded.Placement.Width);  // 默认值，非 0
        Assert.True(File.Exists(Path.Combine(directory, "settings.json.bak")));
    }
    finally { CleanupTempDirectory(directory); }
}
```

### 测试 2: `Load_OpacityAboveOne_ReturnsDefaults`

```csharp
[Fact]
public async Task Load_OpacityAboveOne_ReturnsDefaults()
{
    var directory = CreateTempDirectory();
    try
    {
        var defaults = AppSettings.CreateDefaults();
        var invalidJson = JsonSerializer.Serialize(
            defaults with { Placement = defaults.Placement with { Opacity = 1.5 } });
        await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), invalidJson);

        var store = new JsonSettingsStore(directory);
        var loaded = await store.LoadAsync();

        Assert.Equal(0.9, loaded.Placement.Opacity);  // 默认值，非 1.5
        Assert.True(File.Exists(Path.Combine(directory, "settings.json.bak")));
    }
    finally { CleanupTempDirectory(directory); }
}
```

### 测试 3: `Load_ZoomLevelOutOfRange_ReturnsDefaults`

```csharp
[Fact]
public async Task Load_ZoomLevelOutOfRange_ReturnsDefaults()
{
    var directory = CreateTempDirectory();
    try
    {
        var defaults = AppSettings.CreateDefaults();
        var invalidJson = JsonSerializer.Serialize(defaults with { ZoomLevel = 10.0 });
        await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), invalidJson);

        var store = new JsonSettingsStore(directory);
        var loaded = await store.LoadAsync();

        Assert.Equal(1.0, loaded.ZoomLevel);  // 默认值，非 10.0
        Assert.True(File.Exists(Path.Combine(directory, "settings.json.bak")));
    }
    finally { CleanupTempDirectory(directory); }
}
```

### 测试 4: `Load_HeightBelowMin_ReturnsDefaults`

```csharp
[Fact]
public async Task Load_HeightBelowMin_ReturnsDefaults()
{
    var directory = CreateTempDirectory();
    try
    {
        var defaults = AppSettings.CreateDefaults();
        var invalidJson = JsonSerializer.Serialize(
            defaults with { Placement = defaults.Placement with { Height = 100 } });
        await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), invalidJson);

        var store = new JsonSettingsStore(directory);
        var loaded = await store.LoadAsync();

        Assert.Equal(360, loaded.Placement.Height);  // 默认值，非 100
        Assert.True(File.Exists(Path.Combine(directory, "settings.json.bak")));
    }
    finally { CleanupTempDirectory(directory); }
}
```

**注意:** 测试文件需在 using 区添加 `using System.Text.Json;`。

### RED 预期

```
dotnet test --filter "Load_WidthZero|Load_OpacityAboveOne|Load_ZoomLevelOutOfRange|Load_HeightBelowMin"
```

预期 4 个测试全部 FAIL — 当前 `LoadAsync` 不会调用 `Validate()`，反序列化成功即返回，无效值（Width=0, Opacity=1.5, ZoomLevel=10, Height=100）会被原样返回而非回退默认值。

---

## GREEN 阶段 — 最小实现

修改 `src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs` 的 `LoadAsync` 方法。

在 `return settings;` 之前插入 `Validate()` 检查：

```csharp
try
{
    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
    if (settings is null)
    {
        BackupCorruptedFile();
        return AppSettings.CreateDefaults();
    }

    // 新增：语义校验 → 无效值回退默认值
    if (!settings.Validate())
    {
        BackupCorruptedFile();
        return AppSettings.CreateDefaults();
    }

    return settings;
}
```

**最小修改原则：**
- 只插入 4 行（空行 + 注释 + if 块）
- 不改动其他任何代码路径
- 不改动 `BackupCorruptedFile()` 或其他方法

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

- [ ] 4 个新测试写入 `JsonSettingsStoreTests.cs`
- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] 定向测试 FAIL (RED — 证明缺陷存在)
- [ ] 最小实现写入 `JsonSettingsStore.cs`
- [ ] 定向测试 PASS (GREEN)
- [ ] 完整测试 `dotnet test -c Release --no-build` 全部通过 (86 测试: 82 原有 + 4 新增)
- [ ] `git diff --check` clean
- [ ] 只修改了 `JsonSettingsStore.cs` 和 `JsonSettingsStoreTests.cs`，未触及禁止路径

---

## Git 提交信息

```
fix: validate AppSettings after JSON deserialization in LoadAsync

S01-001: LoadAsync now calls AppSettings.Validate() after successful
deserialization. Invalid semantic values (Width=0, Opacity>1, etc.)
trigger BackupCorruptedFile() and return defaults instead of being
passed through to the UI layer.

Added 4 tests: Width=0, Height<150, Opacity>1, ZoomLevel out of range.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-01-REVIEW-FIX-01
Finding: S01-001
Status: DONE
Commit: <hash>
Tests Added: 4
Tests Total: 86
Tests Passed: 86
Tests Failed: 0
Build: Release 0 errors 0 warnings
Files Modified:
  - src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs
  - tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs
Files NOT Modified (verified): <列出禁止路径确认未触及>
Limitations: <如有>
```
