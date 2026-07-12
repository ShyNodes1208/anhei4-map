# 当前任务

## 任务编号
STAGE-01-TASK-06

## 任务名称
Helltides 导航域名允许策略

## 任务类型
BEHAVIOR

## 状态
READY

## 所属阶段
STAGE-01-FOUNDATION

## 前置条件
STAGE-01-TASK-05 完成——`WindowBoundsNormalizer` 实现，40 个测试通过。

## 任务目标
实现纯逻辑 URI 导航决策，为后续 WebView2 `NavigationStarting` 拦截提供安全基础。输入 URI 字符串，返回 `bool`（allow/block）。完全与 WebView2 解耦。

## 允许修改
- Create: `src/Anhei4Map.Core/Services/DomainPolicy.cs`
- Create: `tests/Anhei4Map.Tests/DomainPolicyTests.cs`

## 禁止修改
- 不得修改 `src/Anhei4Map.App/`、`src/Anhei4Map.Infrastructure/`
- 不得修改 `docs/design/`、现有 Services
- 不得引用 WebView2、网络访问、外部浏览器
- 不得添加 NuGet 包

## 策略规则（以安全边界文档为准）
- 允许: `https://helltides.com/*` 和 `https://www.helltides.com/*`
- 拒绝: HTTP、file、javascript、data、非 helltides 域、IP 地址、userinfo 欺骗、无效 URI、null/空/空白

## RED 测试清单 (20 tests)
[Fact] public void Allows_HttpsRootDomain() { }
[Fact] public void Allows_HttpsWww() { }
[Fact] public void Allows_HostCaseInsensitive() { }
[Fact] public void Allows_LegitPath() { }
[Fact] public void Allows_LegitQuery() { }
[Fact] public void Allows_LegitFragment() { }
[Fact] public void Blocks_Http() { }
[Fact] public void Blocks_EvilSubdomain() { }
[Fact] public void Blocks_LookalikeSuffix() { }
[Fact] public void Blocks_UserinfoSpoof() { }
[Fact] public void Blocks_FileScheme() { }
[Fact] public void Blocks_JavascriptScheme() { }
[Fact] public void Blocks_DataScheme() { }
[Fact] public void Blocks_Localhost() { }
[Fact] public void Blocks_IPv4() { }
[Fact] public void Blocks_Non443Port() { }
[Fact] public void Blocks_InvalidUri() { }
[Fact] public void Blocks_Null() { }
[Fact] public void Blocks_EmptyString() { }
[Fact] public void Allows_Default443Port() { }

## 有效 RED
`dotnet test --filter "FullyQualifiedName~DomainPolicyTests"` 编译失败——类型不存在。

## GREEN 最小实现
`DomainPolicy.IsAllowed(string uri)` → `bool`:
1. null/empty/whitespace → false
2. `new Uri(uri, UriKind.Absolute)` — catch `UriFormatException` → false
3. `uri.Scheme != "https"` → false
4. `uri.IsDefaultPort` 或 `uri.Port == 443`
5. `uri.Host` 小写后 == `"helltides.com"` 或 `"www.helltides.com"` → true
6. 所有其他 → false

## 验证命令
```powershell
dotnet test anhei4-map.sln -c Release --filter "FullyQualifiedName~DomainPolicyTests"
dotnet test anhei4-map.sln -c Release --no-build
```

## 完成标准
20/20 定向 + 40 已有 = 60 全部通过

## Git 提交信息
```
feat: add Helltides domain allowlist policy (20 tests)
```

## 完成后报告格式
```
STAGE-01-TASK-06 完成报告
- 状态: DONE
- 定向测试: [N]/20
- 完整测试: [N]/60
- dotnet build -c Release: [结果]
- Git commit: [hash]
```
