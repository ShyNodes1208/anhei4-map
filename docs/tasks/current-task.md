# 当前任务

## 任务编号
STAGE-01-TASK-07

## 任务名称
有限指数退避重试策略

## 任务类型
BEHAVIOR

## 状态
READY

## 所属阶段
STAGE-01-FOUNDATION

## 前置条件
STAGE-01-TASK-06 完成——`DomainPolicy` 实现，60 个测试通过。

## 任务目标
实现与网络、Task.Delay、WebView2 完全解耦的纯逻辑退避策略。输入为 attempt 编号，返回延迟毫秒数或 null（停止重试）。

## 重试参数（来自 `01-product-design.md`）

| 参数 | 值 |
|------|----|
| attempt 起始 | 0（首次调用 = attempt 0） |
| 延迟序列 | [0, 1000, 2000, 4000, 8000, 30000] |
| attempt 6+ | 30000ms（最大） |
| 最大重试 | 10（attempt 10 → null） |
| attempt < 0 | 抛出 ArgumentOutOfRangeException |

## 允许修改
- Create: `src/Anhei4Map.Core/Services/RetryPolicy.cs`
- Create: `tests/Anhei4Map.Tests/RetryPolicyTests.cs`

## 禁止修改
- 不得修改 `src/Anhei4Map.App/`、`src/Anhei4Map.Infrastructure/`
- 不得调用 `Task.Delay`、网络、WebView2
- 不得修改现有 Services

## RED 测试清单 (10 tests)
```
T7.1  attempt 0 → 0ms
T7.2  attempt 1 → 1000ms
T7.3  attempt 2 → 2000ms
T7.4  attempt 3 → 4000ms
T7.5  attempt 4 → 8000ms
T7.6  attempt 5 → 30000ms
T7.7  attempt 6-9 → 30000ms (max)
T7.8  attempt 10 → null (stop)
T7.9  attempt -1 → ArgumentOutOfRangeException
T7.10 attempt 11 → null (beyond max)
```

## GREEN 最小实现
`RetryPolicy.NextDelay(int attempt) → int?` 纯静态方法。
Delays = {0, 1000, 2000, 4000, 8000, 30000}，MaxRetries = 10。

## 验证命令
```powershell
dotnet test anhei4-map.sln -c Release --filter "FullyQualifiedName~RetryPolicyTests"
dotnet test anhei4-map.sln -c Release --no-build
```

## 完成标准
10/10 定向 + 60 已有 = 70 全部通过

## Git 提交信息
```
feat: add retry backoff policy (10 tests)
```

## 完成后报告格式
```
STAGE-01-TASK-07 完成报告
- 定向测试: [N]/10
- 完整测试: [N]/70
- dotnet build: [结果]
- Git commit: [hash]
```
