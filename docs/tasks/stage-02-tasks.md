# Stage 02 Task Map

| # | Component | Type | Dependencies | Tests |
|---|-----------|------|-------------|-------|
| STAGE-02-TASK-01 | WindowStateMachine | TDD | Stage 01 Core | ~12 | DONE (b480f2e) |
| STAGE-02-TASK-02 | HotkeyDispatcher | TDD | Stage 01 Core | ~6 | DONE (aaf24b3) |
| STAGE-02-TASK-03 | IWin32Interop Interface | SCAFFOLD | Stage 01 Core | 0 | DONE (05b1977) |
| STAGE-02-TASK-04 | Win32Interop Adapter | SCAFFOLD | TASK-03, Stage 01 App | 0 | DONE (fb962bd) |
| STAGE-02-TASK-05 | App + NuGet + Mutex + Runtime | SCAFFOLD | Stage 01 App | 0 | DONE (1fe4146) |
| STAGE-02-TASK-06 | MainWindow + WebView2 Control | SCAFFOLD | TASK-04, TASK-05 | 0 | DONE (b0673a5) |
| STAGE-02-TASK-07 | WebView2 Setup + Navigation | IMPLEMENTATION | TASK-06 | 0 | DONE (68d48a7) |

Execution order: TASK-01 → TASK-02 → TASK-03 → TASK-04 → TASK-05 → TASK-06 → TASK-07
