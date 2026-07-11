# 当前任务

## 任务编号
STAGE-01-TASK-03

## 任务名称
JSON 设置保存和读取

## 任务类型
BEHAVIOR

## 状态
READY

## 所属阶段
STAGE-01-FOUNDATION

## 前置条件
STAGE-01-TASK-02 完成——`AppSettings`、`WindowPlacement`、`HotkeyBinding`、`HotkeyCommand`、`WindowState` 类型已定义，12 个测试通过。

## 任务目标
在 `Anhei4Map.Infrastructure` 中实现 JSON 设置文件存储：将 `AppSettings` 序列化为 JSON 保存到 `%LocalAppData%\Anhei4Map\settings.json`，并从该文件读取回 `AppSettings`。使用 `System.Text.Json`。

## 架构约束
- **Core** 不引用 Infrastructure —— Core 保持纯模型
- **Infrastructure** 引用 Core，实现文件存储
- 测试使用临时目录，不污染真实 `%LocalAppData%`
- 不新增第三方 JSON 包（仅 `System.Text.Json`）

## 允许修改
- Create: `src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs`
- Create: `tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs`

## 禁止修改
- 不得修改 `src/Anhei4Map.Core/`（Core 层不参与文件 IO）
- 不得修改 `src/Anhei4Map.App/`
- 不得修改 `docs/` 下任何文件
- 不得实现损坏 JSON 备份和恢复（保留给 Task 4）
- 不得实现 WPF 绑定、WebView2、快捷键、鼠标穿透

## 必须新增的测试

```csharp
// tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs

[Fact] public async Task Save_FileExists() { }
[Fact] public async Task SaveThenLoad_RoundTrip() { }
[Fact] public async Task Save_CreatesDirectoryIfMissing() { }
[Fact] public async Task Load_ReturnsDefaultsWhenFileNotFound() { }
[Fact] public async Task SaveMultiple_UsesLatestValues() { }
[Fact] public async Task SavedJson_UsesExpectedPropertyNames() { }
```

共 6 个测试。测试必须使用 `Path.GetTempPath()` 下的临时目录，并在测试 teardown 中清理。

## RED 预期
定向测试编译失败——`JsonSettingsStore` 类型不存在。

有效 RED 命令：
```powershell
dotnet test anhei4-map.sln -c Release --filter "FullyQualifiedName~JsonSettingsStoreTests"
```

## GREEN 最小实现

```csharp
// src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs

namespace Anhei4Map.Infrastructure.Services;
using System.Text.Json;
using Anhei4Map.Core.Models;

public class JsonSettingsStore
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    // Constructor takes directory path; settings.json is the file name
    public JsonSettingsStore(string directory)
    {
        _filePath = Path.Combine(directory, "settings.json");
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json, System.Text.Encoding.UTF8);
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return AppSettings.CreateDefaults();

        var json = await File.ReadAllTextAsync(_filePath,
            System.Text.Encoding.UTF8);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
               ?? AppSettings.CreateDefaults();
    }
}
```

注意：`System.Text.Json` 已内置在 .NET 8 中，无需 NuGet。

## 定向测试命令
```powershell
dotnet test anhei4-map.sln -c Release --filter "FullyQualifiedName~JsonSettingsStoreTests"
```

## 完整测试命令
```powershell
dotnet test anhei4-map.sln -c Release --no-build
```

## 完成标准
1. 6 个定向测试全部通过 + 12 个已有测试保持通过（共 18）
2. `dotnet build -c Release` 零错误
3. Infrastructure 引用 Core（不引用 App）
4. 测试不写入真实用户目录
5. 测试结束后清理临时文件（使用 `try/finally` 或 `IDisposable`）
6. UTF-8 编码
7. 保存目录不存在时自动创建

## Git 提交信息
```
feat: add JSON settings save/load (6 tests)
```

## 完成后报告格式
```
STAGE-01-TASK-03 完成报告
- 状态: DONE
- 创建文件: src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs, tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs
- 定向测试: [通过数]/6
- 完整测试: [通过数]/18
- dotnet build -c Release: [通过/失败]
- Git commit: [hash]
- 已知限制: [如有]
```
