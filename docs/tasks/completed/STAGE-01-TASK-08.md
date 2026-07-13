# STAGE-01-TASK-08: 日志抽象和本地文件日志
## 最终状态: DONE | commit: c5db641
## RED: ACCEPTED_PROCESS_VARIATION (CS0234)
## GREEN: 12/12 FileLoggerTests, 82/82 full, build 0 errors
## 接口: IAppLogger.Log(level, source, message, ex?)
## 文件: app.log (注入目录), UTF-8, 追加, SemaphoreSlim 串行化
## 格式: [UTC ISO8601.ms] [LEVEL] [source] message
## 已知限制: 无轮转(DEFERRED_STAGE_04), 单实例 SemaphoreSlim
## Claude 验收: PASS
