# STAGE-02-TASK-01

- Task: STAGE-02-TASK-01
- Component: WindowStateMachine
- Type: TDD
- Status: DONE
- Implementation Commit: b480f2ef494d42bc677521430bebf5e1475f65ca
- Files:
  - src/Anhei4Map.Core/State/Trigger.cs (new)
  - src/Anhei4Map.Core/State/WindowStateMachine.cs (new)
  - tests/Anhei4Map.Tests/WindowStateMachineTests.cs (new)
- Initial State: Locked
- States: Hidden, Locked, Edit
- Triggers: ToggleHide, ToggleLock
- Invalid Transition Rule: return false, no throw, state unchanged
- RED Evidence: CS0246 (WindowStateMachine class not found)
- Focused Tests: 24/24 PASS
- Full Tests: 146/146 PASS
- Build: Release 0 errors, 0 warnings
- Claude Acceptance: PASS — All 6 state-trigger combinations covered; 5 property truth tables × 3 states verified; no WPF/Win32/WebView2 deps
