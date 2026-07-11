# 当前任务

## 任务编号
STAGE-01-TASK-04

## 任务名称
损坏设置文件恢复与原子保存

## 任务类型
BEHAVIOR

## 状态
READY

## 所属阶段
STAGE-01-FOUNDATION

## 前置条件
STAGE-01-TASK-03 完成——`JsonSettingsStore` 已实现基础 JSON 保存和读取（非原子），18 个测试通过。

## 任务目标
增强 `JsonSettingsStore`：原子写入（tmp + rename）、损坏 JSON 检测与 `.bak` 备份、损坏时回退默认值。保持向后兼容 Task 3 的 API。

## 架构约束
- Infrastructure 引用 Core（不引用 App）
- 不引入第三方包
- 不实现 WPF/WebView2/快捷键/鼠标穿透
- 测试使用临时目录

## 允许修改
- Modify: `src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs`
- Modify: `tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs`
- Modify: `src/Anhei4Map.Infrastructure/Anhei4Map.Infrastructure.csproj`（仅当需要新引用时）

## 禁止修改
- 不得修改 `src/Anhei4Map.Core/`
- 不得修改 `src/Anhei4Map.App/`
- 不得修改 `docs/`
- 不得添加 NuGet 包

## 必须新增的测试

```csharp
// 追加到 tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs

[Fact] public async Task Load_CorruptedJson_ReturnsDefaults() { }
[Fact] public async Task Load_CorruptedJson_RenamesToBak() { }
[Fact] public async Task Load_CorruptedJson_BakContentsMatchOriginal() { }
[Fact] public async Task Load_ExistingBak_NotOverwrittenByNewCorruption() { }
[Fact] public async Task SaveAtomic_Success_FormalFileCorrect() { }
[Fact] public async Task SaveAtomic_Success_TmpCleaned() { }
[Fact] public async Task Load_ValidJson_DoesNotCreateBak() { }
[Fact] public async Task Load_FileNotFound_ReturnsDefaults() { }
```

共 8 个新测试。已有 6 个测试应保持通过。

## RED 预期
新增测试编译失败——`SaveAtomic`、损坏恢复方法不存在，或 `LoadAsync` 行为未更新。

## GREEN 最小实现

**原子保存 (`SaveAsync`)：**
```
1. 序列化为 JSON
2. WriteAllTextAsync to settings.json.tmp
3. File.Move(tmp, settings.json, overwrite: true)
4. File.Move is atomic on NTFS
```

**损坏恢复 (`LoadAsync`)：**
```
1. 文件不存在 → CreateDefaults()
2. 读取 JSON → JsonSerializer.Deserialize
3. JsonException 或 null → File.Move(json, .bak, overwrite: false)
   - 如果 .bak 已存在 → 追加时间戳后缀 (settings.json.bak.20260711T012345)
4. 返回 CreateDefaults()
```

**注意：**
- 使用 `File.Move(json, bak)` 而非先删后移
- `.bak` 已存在时不覆盖，使用时间戳后缀
- 原子写入后清理 `.tmp`（正常路径不残留）
- `LoadAsync` 启动时清理任何残留 `.tmp`

## 定向测试命令
```powershell
dotnet test anhei4-map.sln -c Release --filter "FullyQualifiedName~JsonSettingsStoreTests"
```

## 完整测试命令
```powershell
dotnet test anhei4-map.sln -c Release --no-build
```

## 完成标准
1. 8 个新测试 + 6 个已有测试 = 14 个 `JsonSettingsStoreTests` 全部通过
2. 加 Task 2 的 12 个测试 = 26 个测试全部通过
3. `dotnet build -c Release` 零错误
4. 原子写入：成功后无 `.tmp` 残留
5. 损坏恢复：`.bak` 创建，内容与损坏源一致
6. 已有 `.bak` 时不覆盖（时间戳备选名）
7. 测试不写真实 `%LocalAppData%`

## Git 提交信息
```
feat: add atomic save and corrupted settings recovery (8 tests)
```

## 完成后报告格式
```
STAGE-01-TASK-04 完成报告
- 状态: DONE
- 修改文件: src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs, tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs
- 定向测试: [通过数]/14
- 完整测试: [通过数]/26
- dotnet build -c Release: [通过/失败]
- Git commit: [hash]
- 已知限制: [如有]
```
