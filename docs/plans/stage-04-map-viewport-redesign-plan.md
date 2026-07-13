# Stage 04: Map Viewport Redesign

## Stage Info
- Stage: STAGE-04
- Name: Map Viewport Redesign
- Slug: map-viewport-redesign
- Branch: feature/04-map-viewport-redesign
- Base: de1bb3a1971c9f4a2fe9f1075d432bbe603c818b (v0.3.0-map-overlay-preview)

## Known Limitation
Current captured map region has ~3.17:1 aspect ratio, cutting off most vertical content. Overlay displays only ~400×126 meaningful pixels.

## Diagnostic Strategy
Before changing any layout behavior, TASK-01 will instrument the DOM query and capture pipeline with detailed diagnostics. All candidate elements, WebView2 metrics, and selection decisions are logged to disk for analysis.

## Prohibited Approaches (from FIX-04/FIX-05 failure)
- Force position=fixed / width=100vw / height=100vh
- Modify html/body overflow
- L.map.invalidateSize() or Leaflet internals
- Guess site JavaScript objects
- Only increase RendererWindow height
- Stretch=Fill / UniformToFill
- Secondary square crop

## Candidate Solutions (to be evaluated from diagnostics)
- Correct DOM selector to the true map root container
- Adjust WebView2 page zoom or responsive breakpoint
- Trigger site's built-in fullscreen map mode
- Adjust RendererWindow viewport dimensions
- Scroll-stitch captures only if diagnostics prove map exceeds viewport
- Use public data API to render map (last resort)

## Task Sequence
- TASK-01: Map DOM and capture diagnostics
- TASK-02: Select redesign strategy from evidence
- TASK-03: Implement viewport/map-region correction
- TASK-04: Integration and manual acceptance

## Scope
- Diagnostic instrumentation
- Data-driven selector/viewport correction
- Verified to produce approximately 1.6:1 capture ratio

## Non-Goals
- Periodic refresh, mouse passthrough, hotkeys, character sync, game memory access

## Codex Review Gates

### Planning Gate
Claude plan → Codex read-only review → Claude adjudication → User approval → TASK-01 dispatch.

### Per-Task Gate (TASK-01, TASK-03, TASK-04)
Cursor commit → Claude scope/build/test check → Codex independent diff review → Claude adjudication → Cursor fix (if needed) → Codex re-review → Claude marks DONE.

### TASK-02 Special Gate
TASK-01 diagnostics complete → Claude summarizes evidence → Codex reviews conclusions → Claude adjudicates and selects strategy → User approves strategy → TASK-03 dispatch.

### Stage Final Gate
TASK-04 acceptance → Codex Stage Final Review (all diffs, tests, diagnostics disabled, risks) → Codex APPROVE required before merge or version freeze.
