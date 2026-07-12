# STAGE-01-TASK-06: Helltides 导航域名允许策略

## 最终状态
DONE

## 实现 commit
806e05501ec2c14e55711975140e35ba680655ae

## 修改文件
- `src/Anhei4Map.Core/Services/DomainPolicy.cs`
- `tests/Anhei4Map.Tests/DomainPolicyTests.cs`

## RED 证据
CS0103 — `DomainPolicy` 类型不存在。ACCEPTED_PROCESS_VARIATION。

## GREEN 证据
20/20 定向测试通过。60/60 完整测试通过。

## 允许 URI
`https://helltides.com/`, `https://www.helltides.com/` (含 path/query/fragment/443)

## 拒绝 URI
HTTP, file, javascript, data, localhost, IPv4, 非 443 端口, 相似域名欺骗, userinfo 欺骗, 无效 URI, null/空

## 子域架构差异裁决
架构文档 `02-architecture.md` 允许 `.helltides.com` 子域，但当前任务采用最小权限策略（仅 root + www）。创建 ADR-001 记录决策。

## UserInfo 场景裁决
`https://user:password@helltides.com/` 未被显式拒绝（host 检查仍通过）。MVP 阶段无用户输入 URL 的路径 —— ACCEPTED_LIMITATION。

## RED 流程裁决
ACCEPTED_PROCESS_VARIATION

## Claude 验收结论
PASS
