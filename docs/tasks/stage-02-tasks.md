# Stage 02 Task Map

| # | Component | Type | Dependencies | Tests |
|---|-----------|------|-------------|-------|
| STAGE-02-TASK-01 | WindowStateMachine | TDD | Stage 01 Core | ~12 |
| STAGE-02-TASK-02 | HotkeyDispatcher | TDD | Stage 01 Core | ~6 |
| STAGE-02-TASK-03 | IWin32Interop Interface | SCAFFOLD | Stage 01 Core | 0 |
| STAGE-02-TASK-04 | Win32Interop Adapter | SCAFFOLD | TASK-03, Stage 01 App | 0 |
| STAGE-02-TASK-05 | App Single Instance + Runtime | SCAFFOLD | Stage 01 App | 0 |
| STAGE-02-TASK-06 | WebView2 NuGet + MainWindow | SCAFFOLD | TASK-04, TASK-05 | 0 |
| STAGE-02-TASK-07 | WebView2 Setup + Navigation | SCAFFOLD | TASK-06 | 0 |

Execution order: TASK-01 → TASK-02 → TASK-03 → TASK-04 → TASK-05 → TASK-06 → TASK-07
