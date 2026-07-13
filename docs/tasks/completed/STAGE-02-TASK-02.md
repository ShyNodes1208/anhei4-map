# STAGE-02-TASK-02

- Task: STAGE-02-TASK-02
- Component: HotkeyDispatcher
- Type: TDD
- Status: DONE
- Implementation Commit: aaf24b34fad8740685c31d0cc8fdbc5a91c381e5
- Files:
  - src/Anhei4Map.Core/Services/HotkeyDispatcher.cs (new)
  - tests/Anhei4Map.Tests/HotkeyDispatcherTests.cs (new)
- API: Constructor(IEnumerable<HotkeyBinding>?), Dispatch(int id) → HotkeyCommand
- Rules: Known ID → mapped command; Unknown ID → Unknown; null bindings → empty; duplicate IDs → last-wins
- Focused Tests: 14/14 PASS
- Full Tests: 160/160 PASS
- Build: Release 0 errors, 0 warnings
- Claude Acceptance: PASS — Pure mapping service; no Win32/WPF; all 8 commands verified
