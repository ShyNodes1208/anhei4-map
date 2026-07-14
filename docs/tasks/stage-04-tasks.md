# Stage 04 Task Map

- Current Task: STAGE-04-TASK-02
- TASK-01: COMPLETE. Manual Diagnostic Run: PASS.
- TASK-02: ANALYSIS_COMPLETE_PENDING_CODEX_REREVIEW
- TASK-03: BLOCKED_BY_TASK_02_USER_APPROVAL
- Cursor: BLOCKED. User Approval: PENDING_CODEX_DIAGNOSTIC_REREVIEW. Next Reviewer: Codex.

Governance: `docs/governance/project-development-rules.md` applies. Each dispatch requires Minimal Implementation Check + Overengineering Check: PASS.

---

## STAGE-04-TASK-01: Minimal Map Viewport Diagnostics

- Task ID: STAGE-04-TASK-01
- Type: DIAGNOSTIC_IMPLEMENTATION
- Status: COMPLETE
- Objective: Determine why captured map is ~3.17:1 using minimal diagnostics.
- Dependencies: Codex simplified plan approval + user approval (MET)

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

### Codex Review Gate
Per-task code review after Claude acceptance.
### Next Executor
Cursor after plan + user approval.
### User Approval Rule
PENDING_CODEX_DIAGNOSTIC_REREVIEW. (Historical: approved at plan stage.)

---

## STAGE-04-TASK-02: Evidence Analysis and Strategy Approval

- Task ID: STAGE-04-TASK-02
- Type: DOCS_ANALYSIS
- Status: ANALYSIS_COMPLETE_PENDING_CODEX_REREVIEW
- Objective: Analyze TASK-01 output, determine root cause, select one minimal fix.
- Dependencies: TASK-01 completed + Claude + Codex accepted (MET — awaiting final Codex re-approval)

### Allowed Paths
docs/plans/stage-04-map-viewport-redesign-plan.md, docs/tasks/stage-04-tasks.md, docs/tasks/current-task.md, docs/status/project-status.md, docs/reviews/stage-04-diagnostic-analysis.md, docs/reviews/stage-04-diagnostic-review-request.md, docs/reviews/stage-04-diagnostic-adjudication.md

### Forbidden Paths
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

### Verification
- diagnostics.json parses correctly
- capture-full.png and capture-crop.png open correctly
- All 3 files from same application run
- PNG pixel dimensions match diagnostics.json capture metrics
- Root Cause has specific diagnostic fields and screenshot evidence
- Selected Fix is a single minimal approach
- git diff contains no src/** or tests/** changes
- Codex completed read-only diagnostic conclusion review

### Minimal Implementation Check
- Current Requirement: Determine root cause and minimal fix from evidence
- Minimum Change: Analyze 3 diagnostic files only
- Existing Reused: NONE (no code changes)
- New Files: Only Stage 04 analysis/review docs
- New Dependencies: NONE
- Overengineering Check: PASS

### Codex Review Gate
Diagnostic conclusion must pass independent Codex review.
### Next Executor
Claude + Codex.
### User Approval Rule
REQUIRED before TASK-03 dispatch.

---

## STAGE-04-TASK-03: Fix Implementation, Acceptance and Final Review

- Task ID: STAGE-04-TASK-03
- Type: IMPLEMENTATION_ACCEPTANCE
- Status: BLOCKED_BY_TASK_02_USER_APPROVAL
- Objective: Implement exactly one TASK-02 approved fix. No deviation.
- Dependencies: TASK-02 Codex + Claude + User approved

### Allowed Paths
Frozen by TASK-02. No dispatch until exact paths recorded.

### Inputs
- TASK-02 approved Root Cause
- TASK-02 approved Selected Fix
- TASK-02 frozen Exact Allowed Files
- Expected Result
- Rollback Plan
- User approval record

### Outputs
- Cursor implementation commit
- Release build result
- Complete test result
- git diff --check result
- Before/after screenshots
- Claude acceptance result
- Codex read-only code review result
- User manual acceptance result
- Codex Stage Final Review verdict

### Forbidden Paths
Unapproved files, unapproved alternatives, unrelated refactoring, new NuGet, solution changes, mouse passthrough, global hotkeys, settings UI, game memory, API redraw.

### Exit Criteria
- Single approved fix only
- Full vertical marked areas visible, no stretch, no core truncation
- Normal Overlay unchanged outside approved behavior
- Build 0e0w, all tests pass, diff check clean
- Before/after screenshots
- Windows 10/11 WebView2 smoke test
- Codex Stage Final Review APPROVE

### Verification
In Windows 10/11 PowerShell:
- `dotnet restore anhei4-map.sln`
- `dotnet build anhei4-map.sln -c Release --no-restore`
- `dotnet test anhei4-map.sln -c Release --no-build`
- `git diff --check`

And confirm:
- Build: 0 errors, 0 warnings
- All tests pass
- git diff --check clean
- Windows WebView2 actual run passes
- Full vertical marked areas visible after fix
- Map is not stretched
- Existing Overlay behavior is not broken
- No unapproved functionality implemented

### Minimal Implementation Check
- Current Requirement: Display complete vertical map region
- Minimum Change: Determined by TASK-02 evidence
- Existing Reused: Must prefer existing RendererWindow/MapRegion/CapturePreview/Overlay
- New Files: NONE unless TASK-02 proves necessary
- New Dependencies: NONE unless separately user-approved
- Explicitly NOT: mouse passthrough, hotkeys, settings UI, game memory, API redraw
- Overengineering Check: PASS

### Codex Review Gate
Cursor → Claude → Codex diff review → Claude adjudicate → fix if needed → Codex re-review → User manual acceptance → Codex Stage Final Review. No merge/freeze before APPROVE.
### Next Executor
Cursor after user approval.
### User Approval Rule
Only the single fix explicitly approved by User during TASK-02 may be implemented. User manual acceptance and Codex Stage Final Review APPROVE required before Stage 04 completion or version freeze.
