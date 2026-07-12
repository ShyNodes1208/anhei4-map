# Stage 01 Codex Review 裁决

**裁决人:** Claude Code + DeepSeek
**审核来源:** Codex Stage 01 Review
**裁决日期:** 2026-07-12
**分支:** feature/01-foundation
**总体结论:** CHANGES_REQUIRED — 确认 Codex 全部 4 个 MEDIUM 发现，需修复后方可合并。

---

## 裁决方法

逐项阅读 Codex 审核中引用的源文件、测试文件、Stage 01 计划、设计文档，交叉验证每条发现的事实基础。计划 > 任务完成记录 > 审核范围文档。

---

## MEDIUM 发现裁决

### S01-001: Validate() 未在 LoadAsync 后调用 — CONFIRMED

**事实:**
- [JsonSettingsStore.cs:60-67](src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs#L60-L67): 反序列化成功后直接 `return settings`，未调用 `AppSettings.Validate()`
- [AppSettings.cs:51-74](src/Anhei4Map.Core/Models/AppSettings.cs#L51-L74): `Validate()` 方法已实现，检查 Width≥200、Height≥150、Opacity 0.1-1.0、ZoomLevel 0.25-5.0
- [stage-01-foundation-plan.md:84](docs/plans/stage-01-foundation-plan.md#L84): 明确要求 "Invalid values (Width=0) → use defaults"
- [03-test-strategy.md:67-71](docs/design/03-test-strategy.md#L67-L71): T3.4 (Width=0 回退)、T3.5 (Opacity>1 回退) 在测试策略中明确列出
- 审核范围文档将此标为 "Deferred: TASK-04B"，但 Stage 01 计划中无此范围变更授权

**裁决:** CONFIRMED。计划明确要求语义无效值回退默认值。任务完成记录的单方面推迟不构成范围变更。`Validate()` 方法本身已实现并测试，缺失的仅是 LoadAsync 中的调用及加载层测试。

**修复要求:** 反序列化成功后调用 `Validate()`；失败时执行 `BackupCorruptedFile()` + 返回默认值。补充 Width=0、Opacity>1 等语义无效 JSON 的加载测试。

**修复记录:**
- Commit: aac877786ee038851a2ea31fcd04d55dcaf60946
- 日期: 2026-07-12
- 修改: JsonSettingsStore.cs (+4行 Validate 调用), JsonSettingsStoreTests.cs (+4测试)
- 测试: 86/86 PASS (新增4: Width=0, Height=100, Opacity=1.5, ZoomLevel=10)
- Build: Release 0 errors 0 warnings
- Claude 验收: APPROVED — 变更最小精确、测试有效、仅触及允许路径

---

### S01-002: DomainPolicy 接受非空 UserInfo — CONFIRMED

**事实:**
- [DomainPolicy.cs:32-33](src/Anhei4Map.Core/Services/DomainPolicy.cs#L32-L33): 仅检查 `parsed.Host`，未检查 `parsed.UserInfo`
- `https://user:password@helltides.com/` → Host=`helltides.com`，UserInfo=`user:password` → 当前代码返回 `true`
- 现有测试 `Blocks_UserinfoSpoof` 测试的是 `https://helltides.com@evil.example/`，其中 Host 实际为 `evil.example`，被正确拒绝
- 无设计文档或 ADR 说明 UserInfo 的合法用途
- ADR-001 收紧为 root + www 仅缩小 host 范围，未涉及 UserInfo

**裁决:** CONFIRMED。`parsed.UserInfo` 非空时 Host 仍可为允许域名，当前代码不会拒绝。WebView2 导航安全策略不应允许携带凭据的 URI。

**修复要求:** 在允许 host 前检查 `string.IsNullOrEmpty(parsed.UserInfo)`。补充 `https://user:pass@helltides.com/` 和 `https://user:pass@www.helltides.com/` 两种 URL 均被拒绝的测试。

**修复记录:**
- Commit: bc8dfec523faa0bd16a9c877d4669760dd4c87a3
- 日期: 2026-07-12
- 修改: DomainPolicy.cs (+5行 UserInfo 检查), DomainPolicyTests.cs (+3测试)
- 测试: 89/89 PASS (新增3: UserInfo on root domain, www domain, legit path)
- Build: Release 0 errors 0 warnings
- Claude 验收: APPROVED — 变更最小精确、测试覆盖 root/www 两种域名 + 纯用户名场景

---

### S01-003: FileLogger 缺少轮转 — CONFIRMED

**事实:**
- [FileLogger.cs:32](src/Anhei4Map.Infrastructure/Logging/FileLogger.cs#L32): 仅执行 `File.AppendAllText`，无大小检查、无重命名、无旧文件淘汰
- [stage-01-foundation-plan.md:133](docs/plans/stage-01-foundation-plan.md#L133): "10MB rolling rotation, max 3 files"
- [02-architecture.md:393](docs/design/02-architecture.md#L393): "Rolling 10MB max; on overflow rename to app.1.log, keep 3 rotated files"
- [04-security-boundary.md:67-74](docs/design/04-security-boundary.md#L67-L74): 目录结构图中列出 `app.1.log` 轮转文件
- 审核范围将此标为 "Deferred: Log rotation (Stage 04)"，但 Stage 01 计划中无此授权

**裁决:** CONFIRMED。三个独立设计文档一致要求在 Stage 01 实现日志轮转。单方面推迟到 Stage 04 不成立。

**修复要求:** 实现 10MB 阈值检查、app.log → app.1.log → app.2.log → 删除最旧文件的轮转逻辑。补充阈值触发、轮转顺序、淘汰行为的测试。补充写入异常后再次写入的测试（验证 finally 释放锁，非证明死锁缺陷）。

---

### S01-004: WindowBoundsNormalizer 按主显示器裁剪所有窗口 — CONFIRMED

**事实:**
- [WindowBoundsNormalizer.cs:28](src/Anhei4Map.Core/Services/WindowBoundsNormalizer.cs#L28): `var primary = workAreas[0]`
- [WindowBoundsNormalizer.cs:44-52](src/Anhei4Map.Core/Services/WindowBoundsNormalizer.cs#L44-L52): Width/Height 上限裁剪均使用 `primary.Width`/`primary.Height`
- `IsSufficientlyVisible()` 正确遍历所有 work area 检查可见性，但 max size 裁剪始终用 `workAreas[0]`
- 测试中两个显示器均为 1920×1080，未覆盖异构尺寸场景
- [02-architecture.md:344-355](docs/design/02-architecture.md#L344-L355): 架构文档说 "Clamp width/height: max = screen dimensions"，未明确说是哪个 screen

**裁决:** CONFIRMED。当副显示器尺寸大于主显示器时（如笔记本 1920×1080 外接 3840×2160），副显示器上合法的宽窗口会被错误裁剪。`IsSufficientlyVisible()` 已正确使用多显示器逻辑，max size 裁剪应与其一致。

**修复要求:** 根据窗口主要相交的 WorkArea（而非固定 `workAreas[0]`）决定 max size。补充副显示器大于主显示器的负坐标多显示器测试。

---

## LOW 发现裁决

### S01-005: 备份时间戳秒级精度 — CONFIRMED

**事实:**
- [JsonSettingsStore.cs:98](src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs#L98): `"yyyyMMdd'T'HHmmss"` 精确到秒
- 已有 .bak 且同一秒内再次损坏时，`File.Move` 因目标已存在抛出异常
- 触发条件极低概率（需同一秒内两次损坏 + .bak 已存在）

**裁决:** CONFIRMED (LOW)。建议修复但不阻塞合并。

**修复建议:** 使用毫秒精度时间戳或 GUID 后缀。补充候选文件名已存在时的测试。

---

### S01-006: UTF-8 测试恒真断言 — CONFIRMED

**事实:**
- [FileLoggerTests.cs:181](tests/Anhei4Map.Tests/FileLoggerTests.cs#L181): `Assert.Equal(Encoding.UTF8.GetPreamble().Length == 0 ? bytes : bytes, bytes)`
- `Encoding.UTF8.GetPreamble().Length` 恒为 3，条件恒为 false，断言退化为 `Assert.Equal(bytes, bytes)` — 永远通过

**裁决:** CONFIRMED (LOW)。建议修复但不阻塞合并。

**修复建议:** 删除恒真断言，替换为明确的 BOM 策略断言（如 `Assert.NotEqual(0xEF, bytes[0])` 验证无 BOM）。

---

## 测试缺口裁决

| 缺口 | 关联发现 | 是否阻塞合并 |
|------|----------|-------------|
| 语义无效 JSON 加载测试 (Width=0, Opacity>1, ZoomLevel 越界) | S01-001 | 是 |
| UserInfo 非空拒绝测试 | S01-002 | 是 |
| FileLogger 10MB 轮转测试 | S01-003 | 是 |
| FileLogger 写入异常后恢复测试 | S01-003 | 是 |
| 异构尺寸多显示器测试 | S01-004 | 是 |
| 备份文件名冲突测试 | S01-005 | 否 (LOW) |

---

## 范围偏差裁决

| 偏差 | 裁决 |
|------|------|
| FileLogger 轮转推迟到 Stage 04 | 拒绝。Stage 01 计划 + 架构 + 安全文档一致要求。必须在本阶段实现。 |
| Validate() 推迟为 TASK-04B | 拒绝。计划 TASK-04 明确要求语义无效值回退。必须在本阶段实现。 |
| ADR-001 收紧 host 范围 | 接受。缩小权限，有 ADR 支持，不视为缺陷。 |

---

## 安全边界裁决

与 Codex 一致：
- 未发现红线违规
- DomainPolicy host 匹配阻止相似后缀、子域名、IP、非 HTTPS、非 443 端口
- Core → Infrastructure → App 引用方向正确
- RetryPolicy attempt 0-9 正确，未发现 off-by-one
- FileLogger 异常路径释放锁正确
- PowerShell 脚本 $PSScriptRoot 定位正确，退出码传播正确

---

## 合并判定

**不可合并。** 必须先修复 S01-001、S01-002、S01-003、S01-004 并补齐对应测试，提交新的 Release 构建 + 82+ 测试通过 + git diff --check clean 证据。

S01-005 和 S01-006 建议同批次修复但不阻塞合并。

---

## 下一步

1. 将本裁决写入 `docs/reviews/stage-01-adjudication.md`
2. 创建 `docs/tasks/stage-01-fixes.md` 供 Cursor 执行
3. Cursor 逐项修复 → 提交 → Codex 复审
