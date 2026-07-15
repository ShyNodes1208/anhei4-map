# Stage 04 TASK-03 Code Review Request

## Reviewer
Codex (read-only independent review)

## Implementation
- Dispatch: 1d8a44f
- Commit: 0a817ec8ec0b7352a3daa10938220ac1b5334ac6
- Files: RendererWindow.xaml.cs (+74/-1)

## Claude Acceptance
- Build: 0e0w | Tests: 160/160 | Allowed Paths: PASS
- Production selector reused: PASS
- Scroll count: once per capture cycle
- MapRegion re-queried after scroll: PASS
- Navigation guards: 7 checkpoints
- No second WebView2/selector/stitching
- Overengineering: PASS

## What Changed
- `TryScrollProductionMapIntoViewAsync` — bounded scrollIntoView on production-selected element
- `captureNavigationGeneration` — guards scroll, re-query, wait, and capture
- MapRegion re-query after scroll replaces old values before final crop
