# STAGE-02-TASK-04

- Task: STAGE-02-TASK-04
- Component: Win32Interop Adapter
- Type: SCAFFOLD
- Status: DONE
- Implementation Commit: fb962bdd9d6859b9ffad58ebe8f1c58f77d263f7
- Files:
  - src/Anhei4Map.App/Win32Interop.cs (new)
  - src/Anhei4Map.App/Win32Native.cs (new)
- x64/x86: IntPtr.Size branching, SetWindowLongPtrW/GetWindowLongPtrW vs SetWindowLongW/GetWindowLongW
- Monitor: GetMonitorInfo false → skip, EnumDisplayMonitors false → empty array
- DPI: dpi / 96.0f, dpi==0 → 0.0f
- Build: Release 0 errors, 0 warnings
- Tests: 160/160 PASS
- Claude Acceptance: PASS — All 5 methods correctly implemented; delegate stored in local variable; rcWork used correctly
