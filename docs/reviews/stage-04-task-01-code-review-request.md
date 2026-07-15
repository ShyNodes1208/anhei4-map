# Stage 04 TASK-01 Code Review Request

## Reviewer
Codex (read-only independent review)

## Implementation
- Base: d798c541e0340958694ebdeed6d2437d9dc78354
- Commit: d64846ea2ebfe42bfc4aa1ac36e7dc2d4b45bdec
- Files: App.xaml.cs, RendererWindow.xaml.cs, MapViewportDiagnostics.cs

## Claude Acceptance
- Build: 0 errors, 0 warnings
- Tests: 160/160 PASS
- Allowed Paths: PASS (3 files only)
- Minimal Implementation: PASS
- Overengineering Check: PASS

## Review Objectives
- Verify no overengineering, no second capture/crop/map algorithm
- Verify diagnostic is default-off, one-shot, does not block Overlay
- Verify diagnostics.json, capture-full.png, capture-crop.png only
- Verify URL sanitized, no cookies/credentials/storage captured
- Verify existing production flow unchanged when diagnostic off
