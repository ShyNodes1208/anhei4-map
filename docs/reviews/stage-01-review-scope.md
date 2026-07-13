# Stage 01 Codex Review Scope

**Base:** origin/main (44dbada)
**Head:** feature/01-foundation (fdce531)
**Stage:** STAGE-01-FOUNDATION
**Completed:** TASK-01 through TASK-09

## Review Scope
All diffs between origin/main and HEAD.

## Excluded from Stage 01
- WPF Overlay window behavior
- Global hotkey registration (RegisterHotKey)
- Mouse click-through (WS_EX_TRANSPARENT)
- WebView2 integration, navigation interception
- System tray icon
- Release packaging / publish

## Key Design Decisions
- ADR-001: Navigation host allowlist — root + www only (minimum privilege)
- Retry: [0,1000,2000,4000,8000,30000]ms, max 10 retries, attempt 0-indexed
- Log: [UTC ISO8601.ms] [LEVEL] [source] message, app.log, SemaphoreSlim
- Settings: atomic tmp+rename, corrupted→.bak+defaults, no Validate-after-Load yet
- WindowBounds: 20% visibility threshold, 16px edge padding, workAreas[0] as primary
- DomainPolicy: exact host match (no subdomain wildcard per ADR-001)

## Deferred
- TASK-04B: AppSettings.Validate() after LoadAsync
- Log rotation (Stage 04)
- WPF integration (Stage 02)

## Test Evidence
docs/reviews/stage-01-test-output.txt
