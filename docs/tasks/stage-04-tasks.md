# Stage 04 Task Map

All tasks PENDING. Cursor BLOCKED until Codex simplified plan approval + user approval.

Governance: `docs/governance/project-development-rules.md` applies. Each dispatch requires Minimal Implementation Check + Overengineering Check: PASS.

---

## STAGE-04-TASK-01: Minimal Map Viewport Diagnostics

- Type: DIAGNOSTIC_IMPLEMENTATION
- Status: PLANNED_NOT_READY
- Objective: Determine why captured map is ~3.17:1 using minimal diagnostics.
- Dependencies: Codex simplified plan approval + user approval

### Allowed Paths
- src/Anhei4Map.App/App.xaml.cs
- src/Anhei4Map.App/RendererWindow.xaml.cs
- src/Anhei4Map.App/Diagnostics/MapViewportDiagnostics.cs

### Forbidden Paths
Other src files, tests/**, *.csproj, solution, NuGet, OverlayWindow, DomainPolicy, MainWindow, MapRegion model modifications. No DOM modify, resize, scroll, zoom, click, fullscreen, RendererWindow resize. No reflection. No second WebView2. No second map-selection or capture algorithm.

### Inputs
Current RendererWindow, MapRegion query, CapturePreview/crop pipeline, `--diagnose-map-viewport` CLI arg.

### Outputs
%LocalAppData%\Anhei4Map\diagnostics\latest\ — overwritten each run:
- diagnostics.json (page metrics, WPF/WebView2 metrics, max 50 candidates, crop coords)
- capture-full.png (raw CapturePreview)
- capture-crop.png (current production crop)

### Exit Criteria
- Default off; one-shot per process; diagnostic failure does not block Overlay; does not exit app
- No DOM modification, no new deps, no second capture/crop/map algorithm
- diagnostics.json readable; 2 PNGs from same navigation/run

### Minimal Implementation Check
- Current Requirement: Locate root cause of ~3.17:1 map ratio
- Minimum Change: One-time data + image output on existing capture pipeline
- Existing Reused: MapRegion, DOM query, CapturePreview, crop, Overlay
- New Files: MapViewportDiagnostics.cs only (DTO + JSON + 2 PNGs)
- New Dependencies: NONE
- Explicitly NOT implemented: multi-run, retention, manifest, capacity, iframe/shadow DOM, annotation, auto-cleanup, generic diagnostic framework
- Overengineering Check: PASS

### Verification
dotnet restore + build 0e0w + all tests pass + git diff --check. Manual: normal start without arg → no diagnostics; with --diagnose-map-viewport → 3 files in diagnostics/latest/; Overlay still displays.

### Codex Gate
Per-task code review after Claude acceptance.
### Next Executor
Cursor after plan + user approval.
### User Approval
PENDING_CODEX_SIMPLIFIED_PLAN_REVIEW.

---

## STAGE-04-TASK-02: Evidence Analysis and Strategy Approval

- Type: DOCS_ANALYSIS
- Status: BLOCKED_BY_TASK_01
- Objective: Analyze TASK-01 output, determine root cause, select one minimal fix.
- Dependencies: TASK-01 completed + Claude + Codex accepted

### Allowed Paths
docs/plans/stage-04-map-viewport-redesign-plan.md, docs/tasks/stage-04-tasks.md, docs/tasks/current-task.md, docs/status/project-status.md, docs/reviews/stage-04-diagnostic-analysis.md, docs/reviews/stage-04-diagnostic-review-request.md, docs/reviews/stage-04-diagnostic-adjudication.md

### Forbidden
src/**, tests/**, *.csproj, solution, NuGet, production config

### Inputs
diagnostics.json, capture-full.png, capture-crop.png

### Outputs
Root Cause, Selected Candidate, Why 3.17:1 Occurs, Evidence, Rejected Hypotheses, Selected Fix, Exact Allowed Files for TASK-03, Expected Result, Rollback Plan, User Approval Required

### Exit Criteria
- Evidence-based (all 3 diagnostic files from same run)
- Single minimal fix selected
- Codex conclusion review passed, Claude adjudicated, User approved
- No production diff

### Minimal Implementation Check
- Current Requirement: Determine root cause and minimal fix from evidence
- Minimum Change: Analyze 3 diagnostic files only
- Existing Reused: NONE (no code changes)
- New Files: Only Stage 04 analysis/review docs
- New Dependencies: NONE
- Overengineering Check: PASS

### Codex Gate
Diagnostic conclusion must pass independent Codex review.
### Next Executor
Claude + Codex.
### User Approval
REQUIRED before TASK-03 dispatch.

---

## STAGE-04-TASK-03: Fix Implementation, Acceptance and Final Review

- Type: IMPLEMENTATION_ACCEPTANCE
- Status: BLOCKED_BY_TASK_02_APPROVAL
- Objective: Implement exactly one TASK-02 approved fix. No deviation.
- Dependencies: TASK-02 Codex + Claude + User approved

### Allowed Paths
Frozen by TASK-02. No dispatch until exact paths recorded.

### Forbidden
Unapproved files, unapproved alternatives, unrelated refactoring, new NuGet, solution changes, mouse passthrough, global hotkeys, settings UI, game memory, API redraw.

### Exit Criteria
- Single approved fix only
- Full vertical marked areas visible, no stretch, no core truncation
- Normal Overlay unchanged outside approved behavior
- Build 0e0w, all tests pass, diff check clean
- Before/after screenshots
- Windows 10/11 WebView2 smoke test
- Codex Stage Final Review APPROVE

### Minimal Implementation Check
- Current Requirement: Display complete vertical map region
- Minimum Change: Determined by TASK-02 evidence
- Existing Reused: Must prefer existing RendererWindow/MapRegion/CapturePreview/Overlay
- New Files: NONE unless TASK-02 proves necessary
- New Dependencies: NONE unless separately user-approved
- Explicitly NOT: mouse passthrough, hotkeys, settings UI, game memory, API redraw
- Overengineering Check: PASS

### Codex Gate
Cursor → Claude → Codex diff review → Claude adjudicate → fix if needed → Codex re-review → User manual acceptance → Codex Stage Final Review. No merge/freeze before APPROVE.
### Next Executor
Cursor after user approval.
