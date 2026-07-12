# 当前任务

## 状态
READY

## 类型
FIX

## 修复编号
STAGE-01-REVIEW-FIX-06

## 对应 Codex 发现
S01-006 (LOW)

## 阶段
STAGE-01-FOUNDATION (修复轮)

## 分支
feature/01-foundation

## Worktree
D:\AIProjects\anhei4-map-worktrees\stage-01-foundation

---

## 问题证据

[FileLoggerTests.cs:181-182](tests/Anhei4Map.Tests/FileLoggerTests.cs#L181-L182):

```csharp
var bytes = File.ReadAllBytes(LogFilePath(directory));
var content = Encoding.UTF8.GetString(bytes);
Assert.Contains(message, content);
Assert.Equal(Encoding.UTF8.GetPreamble().Length == 0 ? bytes : bytes, bytes);
```

`Encoding.UTF8.GetPreamble().Length` 恒为 3，条件恒为 false，断言退化为 `Assert.Equal(bytes, bytes)`。该行永远通过，不能检测 BOM 或编码策略回归。

---

## 允许修改的精确路径

- `tests/Anhei4Map.Tests/FileLoggerTests.cs`

## 禁止修改范围

- `src/**`（除非 Cursor 发现实际生产编码缺陷，此时返回 BLOCKED_PRODUCTION_DEFECT）
- 其他测试文件
- `docs/design/**`
- `*.csproj`
- `*.sln`

---

## UTF-8 BOM 规则

`FileLogger` 生产代码使用 `File.AppendAllText(..., Encoding.UTF8)`。

实际运行探针确认：.NET 8 的 `Encoding.UTF8` 属性**带 BOM**（`encoderShouldEmitUTF8Identifier = true`）。生成文件前三字节为 `EF-BB-BF`。

冻结设计只规定 UTF-8，未强制无 BOM。FIX-06 是测试质量修复，不改变生产编码行为。

**本任务采用：UTF-8 WITH BOM**

- 文件开头必须为 `EF BB BF`
- `expectedBytes = Encoding.UTF8.GetPreamble() + Encoding.UTF8.GetBytes(expectedLogText)`
- 或等价的 `expectedBytes = new UTF8Encoding(true).GetBytes(expectedLogText)`
- BOM 是 3 字节的 `EF BB BF`，属于规范 UTF-8 BOM 标记
- 如果实现与规则不一致，返回 `BLOCKED_PRODUCTION_DEFECT`，不得自行修改 src

---

## GREEN 阶段（一步到位 — 测试替换）

替换现有的 `Log_WritesUtf8Content` 测试方法。删除恒真断言，替换为精确的字节级验证。

### 新 `Log_WritesUtf8Content` 测试

```csharp
[Fact]
public void Log_WritesUtf8Content()
{
    var directory = CreateTempDirectory();
    const string message = "数据库连接成功";

    try
    {
        var logger = CreateLogger(directory);

        // 构造完整的预期日志文本（与实际 FormatEntry 一致）
        var expectedLine = $"[2026-07-12T10:20:30.123Z] [INFO] [i18n] {message}";
        var expectedText = expectedLine + Environment.NewLine;

        // 生产实现使用 Encoding.UTF8 (WITH BOM)
        var preamble = Encoding.UTF8.GetPreamble(); // EF BB BF
        var contentBytes = Encoding.UTF8.GetBytes(expectedText);
        var expectedBytes = preamble.Concat(contentBytes).ToArray();

        // 写入日志
        logger.Log(LogLevel.Info, "i18n", message);

        // 读取实际文件字节
        var actualBytes = File.ReadAllBytes(LogFilePath(directory));

        // 精确字节比较（含 BOM）
        Assert.Equal(expectedBytes, actualBytes);

        // 验证 UTF-8 BOM 存在（EF BB BF）
        Assert.True(
            actualBytes.Length >= 3 &&
            actualBytes[0] == 0xEF &&
            actualBytes[1] == 0xBB &&
            actualBytes[2] == 0xBF,
            "File must contain UTF-8 BOM (EF BB BF)");

        // 验证 UTF-8 解码后中文正确
        var decodedContent = Encoding.UTF8.GetString(actualBytes);
        Assert.Contains(message, decodedContent);
    }
    finally
    {
        CleanupTempDirectory(directory);
    }
}
```

**关键变更：**
- 删除恒真的 `Assert.Equal(bytes, bytes)`
- 使用固定 TimeProvider 确定预期内容
- `expectedBytes` 包含 BOM（`Encoding.UTF8.GetPreamble()` 即 `EF BB BF`）
- `Assert.Equal(expectedBytes, actualBytes)` — 真实验证含 BOM 的 UTF-8 编码
- 明确断言 BOM 存在（`EF BB BF` 必须在前三字节）
- 验证中文 UTF-8 解码正确
- 不修改生产代码

---

## 定向测试命令

```powershell
dotnet test tests/Anhei4Map.Tests/ --filter "FullyQualifiedName~Log_WritesUtf8Content" -c Release
```

## 完整 FileLogger 测试命令

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

- [ ] `Log_WritesUtf8Content` 测试替换完成
- [ ] 恒真 `Assert.Equal(bytes, bytes)` 已删除
- [ ] 包含精确字节比较 `Assert.Equal(expectedBytes, actualBytes)`
- [ ] 包含无 BOM 断言
- [ ] 包含 UTF-8 解码验证
- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] FileLoggerTests 全部通过 (26/26)
- [ ] 完整测试全部通过 (122/122)
- [ ] `git diff --check` clean
- [ ] 只修改了 `FileLoggerTests.cs`
- [ ] 如果发现生产代码编码缺陷，返回 BLOCKED_PRODUCTION_DEFECT

---

## Git 提交信息

```
test: verify actual UTF-8 log bytes without BOM

S01-006: Replace the tautological Assert.Equal(bytes, bytes) assertion
in Log_WritesUtf8Content with precise byte-level verification. The test
now constructs expected UTF-8 bytes including the BOM preamble (EF BB BF),
compares against actual file bytes, and explicitly verifies the presence
of the UTF-8 BOM signature. Also validates Chinese text round-trips
correctly through UTF-8 decode.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-01-REVIEW-FIX-06
Finding: S01-006
Status: DONE
Commit: <hash>
Tests Modified: 1 (Log_WritesUtf8Content replaced)
Tests: FileLoggerTests 26/26, Full 122/122
Tests Passed: 122
Tests Failed: 0
Build: Release 0 errors 0 warnings
Files Modified:
  - tests/Anhei4Map.Tests/FileLoggerTests.cs
Files NOT Modified (verified): <列出>
Limitations: None
```
