# 当前任务

## 状态
READY

## 类型
FIX

## 修复编号
STAGE-01-REVIEW-FIX-02

## 对应 Codex 发现
S01-002 (MEDIUM)

## 阶段
STAGE-01-FOUNDATION (修复轮)

## 分支
feature/01-foundation

## Worktree
D:\AIProjects\anhei4-map-worktrees\stage-01-foundation

---

## 问题证据

[DomainPolicy.cs:22-33](src/Anhei4Map.Core/Services/DomainPolicy.cs#L22-L33):

```csharp
if (!string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    return false;
if (!parsed.IsDefaultPort && parsed.Port != 443)
    return false;
var host = parsed.Host.ToLowerInvariant();
return host is "helltides.com" or "www.helltides.com";
```

代码检查 scheme、port、host，但**从未检查 `parsed.UserInfo`**。

`https://user:password@helltides.com/` → `parsed.Host` = `helltides.com`，`parsed.UserInfo` = `user:password` → 当前代码返回 `true`。

现有测试 `Blocks_UserinfoSpoof` 测试的 `https://helltides.com@evil.example/` 中 Host 实际为 `evil.example`（`@` 前部分被解析为 UserInfo），因此被 host 检查拦截。但允许域名上携带 UserInfo 的场景未被测试覆盖。

ADB-001、安全设计文档和架构文档均未定义 UserInfo 的合法用途。

---

## 允许修改的精确路径

- `src/Anhei4Map.Core/Services/DomainPolicy.cs`
- `tests/Anhei4Map.Tests/DomainPolicyTests.cs`

## 禁止修改范围

- `src/Anhei4Map.Core/Models/**`
- `src/Anhei4Map.App/**`
- `src/Anhei4Map.Infrastructure/**`
- `docs/design/**`
- `docs/plans/**`
- `*.csproj`
- `*.sln`
- 其他所有测试文件

---

## RED 阶段 — 先写失败测试

在 `tests/Anhei4Map.Tests/DomainPolicyTests.cs` 中新增以下 3 个测试。

### 测试 1: `Blocks_UserinfoOnRootDomain`

```csharp
[Fact]
public void Blocks_UserinfoOnRootDomain()
{
    Assert.False(DomainPolicy.IsAllowed("https://user:pass@helltides.com/"));
}
```

### 测试 2: `Blocks_UserinfoOnWwwDomain`

```csharp
[Fact]
public void Blocks_UserinfoOnWwwDomain()
{
    Assert.False(DomainPolicy.IsAllowed("https://user:pass@www.helltides.com/"));
}
```

### 测试 3: `Blocks_UserinfoOnLegitPath`

```csharp
[Fact]
public void Blocks_UserinfoOnLegitPath()
{
    Assert.False(DomainPolicy.IsAllowed("https://x@helltides.com/map"));
}
```

### RED 预期

```
dotnet test --filter "Blocks_UserinfoOnRootDomain|Blocks_UserinfoOnWwwDomain|Blocks_UserinfoOnLegitPath"
```

预期 3 个测试全部 FAIL — 当前 `IsAllowed` 不检查 `parsed.UserInfo`，非空 UserInfo 的允许域名 URI 会返回 `true` 而非 `false`。

---

## GREEN 阶段 — 最小实现

修改 [DomainPolicy.cs:27-33](src/Anhei4Map.Core/Services/DomainPolicy.cs#L27-L33)，在 port 检查之后、host 检查之前插入 UserInfo 检查：

```csharp
if (!parsed.IsDefaultPort && parsed.Port != 443)
{
    return false;
}

// 新增：拒绝携带凭据的 URI
if (!string.IsNullOrEmpty(parsed.UserInfo))
{
    return false;
}

var host = parsed.Host.ToLowerInvariant();
return host is "helltides.com" or "www.helltides.com";
```

**最小修改原则：**
- 仅插入 4 行（空行 + 注释 + if 块）
- 不改动其他检查逻辑
- 不改变 `parsed.Host` 提取或 host 匹配方式

---

## 定向测试命令

```powershell
dotnet test tests/Anhei4Map.Tests/ --filter "FullyQualifiedName~DomainPolicyTests" -c Release
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

- [ ] 3 个新测试写入 `DomainPolicyTests.cs`
- [ ] `dotnet build -c Release` 0 错误 0 警告
- [ ] 定向测试 FAIL (RED — 3/3 新增测试失败)
- [ ] 最小实现写入 `DomainPolicy.cs`
- [ ] 定向测试 PASS (GREEN — 23/23: 20原有 + 3新增)
- [ ] 完整测试 `dotnet test -c Release --no-build` 全部通过 (89 测试: 86 原有 + 3 新增)
- [ ] `git diff --check` clean
- [ ] 只修改了 `DomainPolicy.cs` 和 `DomainPolicyTests.cs`，未触及禁止路径

---

## Git 提交信息

```
fix: reject URIs with non-empty UserInfo in DomainPolicy

S01-002: DomainPolicy.IsAllowed now checks parsed.UserInfo and rejects
URIs carrying credentials (e.g. https://user:pass@helltides.com/).
WebView2 navigation security policy should not allow credential-bearing URIs.

Added 3 tests: UserInfo on root domain, www domain, and legit path.
```

---

## Cursor 最终报告格式

```
FIX_COMPLETE

Fix: STAGE-01-REVIEW-FIX-02
Finding: S01-002
Status: DONE
Commit: <hash>
Tests Added: 3
Tests Total: 89
Tests Passed: 89
Tests Failed: 0
Build: Release 0 errors 0 warnings
Files Modified:
  - src/Anhei4Map.Core/Services/DomainPolicy.cs
  - tests/Anhei4Map.Tests/DomainPolicyTests.cs
Files NOT Modified (verified): <列出禁止路径确认未触及>
Limitations: <如有>
```
