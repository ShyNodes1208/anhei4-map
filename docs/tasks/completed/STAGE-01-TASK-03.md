# STAGE-01-TASK-03: JSON 设置保存和读取

## 最终状态
DONE

## 任务类型
BEHAVIOR

## 实现 commit
a7e41252418dd5d3d72a5b7ec6a90c90dae5df9f

## 修改文件
- Create: `src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs`
- Create: `tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs`
- Modify: `src/Anhei4Map.Infrastructure/Anhei4Map.Infrastructure.csproj`（新增 Core 引用）

## RED 证据
`dotnet build` 编译失败——`JsonSettingsStore` 类型不存在。可接受。

## GREEN 结果
定向测试 `JsonSettingsStoreTests` 6/6 通过。完整测试 18/18 通过。

## 完整测试结果
```
已通过! - 失败: 0，通过: 18，已跳过: 0，总计: 18，持续时间: 96 ms
```

## 临时目录清理结果
所有测试使用 `Path.Combine(Path.GetTempPath(), "Anhei4Map.Tests", Guid.NewGuid().ToString("N"))` + `try/finally` 清理。无泄漏。

## TASK_SPEC_PATH_OMISSION 结论
Cursor 修改了 `Infrastructure.csproj` 以添加 `ProjectReference`（Core → Infrastructure）。该引用为 `JsonSettingsStore` 编译所必需。分类为 TASK_SPEC_PATH_OMISSION——任务规格应显式允许修改 csproj。规则已更新至 AGENTS.md。

## 设置存储抽象检查结论
阶段计划提到"定义设置存储抽象"但架构文档（02）将 `JsonSettingsStore` 设计为 Infrastructure 中的具体类，未定义 `IAppSettingsStore` 接口。当前实现符合架构基线。Task 4 可根据需要引入接口（如原子写入需要可注入的写入步骤）。

## Claude 验收结论
PASS —— 21 项行为检查通过，6 项测试质量检查通过，1 项流程偏差记录（csproj 路径遗漏）。

## 已知限制
- 非原子写入（`File.WriteAllTextAsync` 直接覆盖）——Task 4 实现原子替换
- 损坏 JSON 未备份——Task 4 实现 `.bak` 恢复
- 无 `CancellationToken` 支持
