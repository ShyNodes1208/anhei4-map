# Stage 04 Task Map

All tasks: PENDING. Cursor BLOCKED until Codex plan approval + user approval.

**Governance:** All tasks must comply with `docs/governance/project-development-rules.md`. Each dispatch requires Minimal Implementation Check and Overengineering Check: PASS.

---

## STAGE-04-TASK-01: Minimal Map Viewport Diagnostics

- Type: DIAGNOSTIC_IMPLEMENTATION
- Status: PLANNED_NOT_READY
- Objective: Determine why captured map region is ~3.17:1 using minimal diagnostics.
- Dependencies: v0.3.0 baseline (de1bb3a)

### Allowed Paths (frozen — 3 files)
- src/Anhei4Map.App/App.xaml.cs
- src/Anhei4Map.App/RendererWindow.xaml.cs
- src/Anhei4Map.App/Diagnostics/MapViewportDiagnostics.cs

### Forbidden Paths
OverlayWindow, DomainPolicy, MainWindow, Infrastructure/**, tests/**, *.csproj, solution, NuGet. No DOM modify, resize, scroll, zoom, click, fullscreen, RendererWindow resize. No reflection. No second WebView2. No second map-selection algorithm.

### Diagnostic Toggle
--diagnose-map-viewport CLI arg. Default off. One-shot per process. No persistence.

### Diagnostic Output
%LocalAppData%\Anhei4Map\diagnostics\latest\ — overwritten each run.
- diagnostics.json
- capture-full.png (raw CapturePreview, no crop)
- capture-crop.png (current production crop result)

### diagnostics.json Minimal Fields

Page: url (no query/fragment), documentReadyState, windowInnerWidth, windowInnerHeight, devicePixelRatio, documentClientWidth, documentClientHeight, documentScrollWidth, documentScrollHeight

WPF/WebView2: rendererWindowWidth, rendererWindowHeight, rendererWindowActualWidth, rendererWindowActualHeight, webViewActualWidth, webViewActualHeight, dpiScaleX, dpiScaleY, webViewZoomFactor

Capture: capturePixelWidth, capturePixelHeight, scaleX, scaleY

Per candidate (max 50): selector, matchIndex, tagName, id, className, parentTagName, parentId, parentClassName, left, top, right, bottom, width, height, clientWidth, clientHeight, scrollWidth, scrollHeight, display, visibility, position, overflow, transform, area, rank, selected

Crop result: selectedCandidate, productionMapRegion, cropLeft, cropTop, cropRight, cropBottom, cropWidth, cropHeight, finalAspectRatio

No manifest. No full DOM export. No iframe/shadow DOM. No cookie/storage/credentials.

### Error Handling (minimal)
Page not ready, WebView2 not init/closed, navigation changed, ExecuteScriptAsync fail, JSON parse/serialize fail, CapturePreviewAsync fail, Bitmap decode fail, directory create fail, file write fail. All: catch, don't exit app, don't block Overlay, no infinite retry.

### Both PNGs
Same navigation, same run, same CapturePreview data. No re-navigation.

### Inputs
--diagnose-map-viewport CLI arg.

### Outputs
diagnostics/latest/ directory with 3 files.

### Exit Criteria
3 files written or failures caught. Overlay not blocked. JSON readable.

### Verification
dotnet restore + build 0e0w + tests all pass + git diff --check. Manual: run with --diagnose-map-viewport, inspect diagnostics/latest/.

### Codex Gate
Per-task code review after Claude acceptance.
### Next Executor
Cursor (after Codex plan approval + user approval)
### User Approval
PENDING_CODEX_SIMPLIFIED_PLAN_REVIEW

---

## STAGE-04-TASK-02: Evidence Analysis and Strategy Approval

- Type: DOCS_ANALYSIS
- Status: BLOCKED_BY_TASK_01
- Dependencies: TASK-01 completed, Codex reviewed, Claude accepted
- Allowed: Stage 04 docs/ only
- Forbidden: src/**, tests/**, *.csproj, solution, NuGet

### Inputs
diagnostics.json, capture-full.png, capture-crop.png

### Outputs
Root Cause, Selected Candidate, Why ~3.17:1 Occurs, Evidence, Rejected Hypotheses, Selected Fix, Exact Allowed Files, Expected Result, Rollback Plan, User Approval Required

### Process
Claude analysis -> Codex review -> Claude adjudication -> User approval -> TASK-03

### Codex Gate
Required at conclusion.
### User Approval
REQUIRED before TASK-03

---

## STAGE-04-TASK-03: Fix Implementation, Acceptance and Codex Final Review

- Type: IMPLEMENTATION_ACCEPTANCE
- Status: BLOCKED_BY_TASK_02_APPROVAL
- Dependencies: TASK-02 approved by Codex + Claude + User
- Allowed Paths: To be frozen by TASK-02.
- Objective: Implement exactly one approved fix. No deviation.

### Exit Criteria
- --diagnose-map-viewport default off
- No diagnostic loops
- Diagnostic failure does not block Overlay
- Windows 10/11 WebView2 smoke test
- Build 0e0w, all tests pass, diff check clean
- Before/after screenshots
- Full vertical marked areas visible
- Ratio within TASK-02 approval
- Codex Stage Final Review APPROVE

### Codex Gate
Cursor commit -> Claude -> Codex diff review -> Claude adjudication -> fixes -> Codex re-review -> Stage Final Review. No merge or freeze before APPROVE.
### Next Executor
Cursor (after TASK-02 approval)
