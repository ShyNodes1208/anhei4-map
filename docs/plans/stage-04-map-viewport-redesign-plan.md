# Stage 04: Map Viewport Redesign

## Stage Info
- Stage: STAGE-04 | Name: Map Viewport Redesign
- Slug: map-viewport-redesign | Branch: feature/04-map-viewport-redesign
- Base: de1bb3a1971c9f4a2fe9f1075d432bbe603c818b (v0.3.0)

## Known Limitation
Captured map region ~3.17:1 ratio. Vertical content cut off.

## Goal
Diagnose why the selected map region is ~3.17:1, then apply a single approved fix.

## Diagnostic Toggle
`--diagnose-map-viewport` CLI arg. Default off. One-shot per process. No persistence.

## Diagnostic Output
`%LocalAppData%\Anhei4Map\diagnostics\latest\`
- diagnostics.json (overwritten each run)
- capture-full.png (overwritten each run)
- capture-crop.png (overwritten each run)

No multi-run retention. No manifest. No annotation. No capacity management.

## Prohibited (permanent)
- DOM modification, resize, scroll, zoom, map interaction, window resize
- Leaflet internals, JS object guessing
- Process injection, game memory
- API redraw, Stretch=Fill, non-proportional stretch

## Codex Review Gates
Plan Review -> Per-Task (TASK-01, TASK-03) -> TASK-02 Analysis -> Stage Final.

## Tasks (3)
- STAGE-04-TASK-01: Minimal map viewport diagnostics (3 files, overwrite latest)
- STAGE-04-TASK-02: Evidence analysis and strategy approval
- STAGE-04-TASK-03: Fix implementation, acceptance, Codex final review

## Windows-Only
Windows 10/11, PowerShell, .NET/WPF/WebView2. No Linux/WSL.
