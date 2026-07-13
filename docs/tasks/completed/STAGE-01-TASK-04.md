# STAGE-01-TASK-04: 损坏设置文件恢复与原子保存

## 最终状态
DONE

## 任务类型
BEHAVIOR

## 实现 commit
246f03469c9de9f0422dc505065abdd6cdb21a04

## 修改文件
- Modify: `src/Anhei4Map.Infrastructure/Services/JsonSettingsStore.cs`
- Modify: `tests/Anhei4Map.Tests/JsonSettingsStoreTests.cs`

## RED 证据
4 项新增测试因损坏 JSON 未被处理而失败。有效 RED。

## GREEN 证据
`JsonSettingsStoreTests` 14/14 通过。

## 完整测试结果
```
已通过! - 失败: 0，通过: 26，已跳过: 0，总计: 26，持续时间: 221 ms
```
(26 = AppSettingsTests 12 + JsonSettingsStoreTests 14)

## Release build 结果
0 警告，0 错误

## 已知限制及裁决

| # | 限制 | 裁决 | 理由 |
|---|------|------|------|
| 1 | 加载后不执行 `AppSettings.Validate()` | DEFERRED → 新增 STAGE-01-TASK-04B | 校验逻辑属于系统完整性；当前 Task 范围限定 JSON 持久化和损坏恢复 |
| 2 | 备份名仅秒级时间戳 `yyyyMMddTHHmmss` | ACCEPTED_LIMITATION | 单人桌面应用，同秒两次损坏概率极低 |
| 3 | 跨卷 `File.Move` 不原子 | ACCEPTED_LIMITATION | `%LocalAppData%` 始终在系统卷，`.tmp` 和 `settings.json` 同目录同卷 |

## Claude 验收结果
PASS —— 14/14 定向测试，26/26 完整测试，原子写入和损坏恢复按架构设计实现。3 项已知限制均已裁决。
