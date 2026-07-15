# STAGE-02-TASK-03

- Task: STAGE-02-TASK-03
- Component: IWin32Interop Interface
- Type: SCAFFOLD
- Status: DONE
- Implementation Commit: 05b1977ca6747b7c9b2d336864689c67965119b8
- Files:
  - src/Anhei4Map.Core/Interop/IWin32Interop.cs (new)
  - src/Anhei4Map.Core/Interop/ScreenInfo.cs (new)
  - src/Anhei4Map.Core/Interop/DpiInfo.cs (new)
- Methods: SetWindowLongPtr, GetWindowLongPtr, SetWindowPos, GetMonitorWorkingAreas, GetDpiForWindow
- Excluded: RegisterHotKey, UnregisterHotKey (Stage 03)
- Build: Release 0 errors, 0 warnings
- Tests: 160/160 PASS
- Claude Acceptance: PASS — No P/Invoke, no Win32 constants, no WPF deps, correct scope
