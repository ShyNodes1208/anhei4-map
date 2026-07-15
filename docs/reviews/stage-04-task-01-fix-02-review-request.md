# Stage 04 TASK-01 FIX-02 Regression Review Request

## Reviewer
Codex

## Previous
FIX-01: 84bb8f5 — 4/5 fixed, REGRESSION-001 remaining

## Fix
FIX-02: 466ad62 (+20/-3 in RendererWindow.xaml.cs only)

## Change Summary
- One-shot claimed BEFORE TryGetProductionMapRegionAsync (line 508-513)
- MapRegion await guard: IsDiagnosticNavigationValid check at line 521-524
- CapturePreview await guard: IsDiagnosticNavigationValid check at line 545-548
- runDiagnostics flag permanently unset on any guard failure
- one-shot never reset

## Claude Acceptance
- Build: 0e0w | Tests: 160/160 | Allowed Paths: PASS
- Minimal Implementation: PASS | Overengineering: PASS
