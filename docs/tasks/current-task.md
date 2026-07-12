# 当前任务

## 任务编号
STAGE-01-TASK-08

## 任务名称
日志抽象和本地文件日志

## 任务类型
BEHAVIOR

## 状态
READY

## 所属阶段
STAGE-01-FOUNDATION

## 前置条件
STAGE-01-TASK-07 完成——70 个测试通过。

## 日志格式（Stage 01 决策）
```
[2026-07-12T10:20:30.123Z] [INFO] [source] message
```
- 时间：UTC ISO 8601，含毫秒
- 级别：INFO / WARN / ERROR
- 文件：app.log（固定名）
- 编码：UTF-8
- 模式：追加写入
- 并发：`SemaphoreSlim` 串行化
- 消息 CR/LF：替换为空格
- 写入失败：向调用方抛出 `IOException`
- 空消息：写入 `"(empty)"`
- 异常：`{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}`
- 日志目录：构造函数注入（生产路径 `%LocalAppData%\Anhei4Map\`）
- 轮转：Stage 04 延期

## 允许修改
- Create: `src/Anhei4Map.Core/Logging/IAppLogger.cs`
- Create: `src/Anhei4Map.Infrastructure/Logging/FileLogger.cs`
- Create: `tests/Anhei4Map.Tests/FileLoggerTests.cs`
- Modify: `src/Anhei4Map.Infrastructure/Anhei4Map.Infrastructure.csproj`（如果需要 Core 引用）

## 禁止修改
- 不得修改 `src/Anhei4Map.App/`
- 不得修改现有 Core Services（RetryPolicy/DomainPolicy/WindowBoundsNormalizer）
- 不得修改 JsonSettingsStore

## RED 测试清单 (12 tests)
```
T8.1  目录不存在时自动创建
T8.2  首次写入生成日志文件
T8.3  第二次写入追加（不覆盖）
T8.4  INFO 格式正确
T8.5  WARN 格式正确
T8.6  ERROR 含异常类型和消息
T8.7  可控时间（构造函数注入 TimeProvider）
T8.8  UTF-8 内容验证
T8.9  空消息写 "(empty)"
T8.10 多行消息 CR/LF 替换为空格
T8.11 并发写入不交错（SemaphoreSlim）
T8.12 测试不写真实 LocalAppData（使用临时目录）
```

## GREEN 最小实现
`IAppLogger` (Core): `void Log(LogLevel level, string source, string message, Exception? ex = null)`

`FileLogger` (Infrastructure): 构造函数 `(string directory, TimeProvider? timeProvider = null)`，实现 `IAppLogger`。

## 验证命令
```powershell
dotnet test anhei4-map.sln -c Release --filter "FullyQualifiedName~FileLoggerTests"
dotnet test anhei4-map.sln -c Release --no-build
```

## 完成标准
12/12 定向 + 70 已有 = 82 全部通过。build 0 错误。

## Git 提交信息
```
feat: add local file logging (12 tests)
```
