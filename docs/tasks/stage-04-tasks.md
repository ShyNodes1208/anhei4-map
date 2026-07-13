# Stage 04 Task Map

| # | Component | Type | Dependencies | Status |
|---|-----------|------|-------------|--------|
| STAGE-04-TASK-01A | CLI toggle + page/DOM/WPF metrics + JSON diagnostics | DIAGNOSTIC_IMPLEMENTATION | v0.3.0 baseline | PENDING |
| STAGE-04-TASK-01B | Capture artifacts + annotated bitmap + retention/failure handling | DIAGNOSTIC_IMPLEMENTATION | TASK-01A | PENDING |
| STAGE-04-TASK-02 | Diagnostic evidence analysis and strategy selection | DOCS_ANALYSIS | TASK-01A, TASK-01B | PENDING |
| STAGE-04-TASK-03 | Approved viewport correction implementation | IMPLEMENTATION | TASK-02 + User Approval | PENDING |
| STAGE-04-TASK-04 | Integration, regression, manual acceptance and final review | INTEGRATION_ACCEPTANCE | TASK-03 | PENDING |

Execution: 01A -> 01B -> 02 -> 03 -> 04. Each task requires Codex per-task review after Claude acceptance.

## TASK-01A: CLI Toggle + JSON Metrics

- Type: DIAGNOSTIC_IMPLEMENTATION
- Objective: Diagnostic entry point and JSON-only metric collection. Default off. Pure observation.
- Allowed: `src/Anhei4Map.App/App.xaml.cs` (CLI arg parse), `src/Anhei4Map.App/Diagnostics/DiagnosticRunner.cs`, `src/Anhei4Map.App/Diagnostics/PageMetrics.cs`, `src/Anhei4Map.App/Diagnostics/CandidateInfo.cs`, `src/Anhei4Map.App/Diagnostics/LayerInfo.cs`, `src/Anhei4Map.App/Diagnostics/WpfMetrics.cs`, `src/Anhei4Map.App/Diagnostics/RunManifest.cs`, `src/Anhei4Map.App/Diagnostics/DiagnosticRun.cs`
- Forbidden: OverlayWindow, DomainPolicy, MainWindow, Infrastructure, tests, csproj, solution, NuGet. No DOM modify, resize, scroll, zoom, click, fullscreen, RendererWindow resize.
- Inputs: --diagnose-map-viewport CLI arg (default off)
- Outputs: manifest.json (complete=false), page.json, candidates.json, layers.json. No PNGs (TASK-01B).
- Exit: JSON files written or failed + manifest updated. Diagnostic failure does not block Overlay. App continues normally after diagnostic completes or fails.
- One-shot: static bool guard, per-process once. No persistence config.
- Consistency: navigation generation recorded at run start; after each await check generation + _isClosed + URL; change terminates run.
- Exceptions: 20 cases covered (ExecuteScriptAsync fail, JSON parse fail, directory create fail, file write fail, page not ready, WebView2 closed, candidates empty, >100 cap, >12 ancestors, JSON size cap, navigation change, URL change, app shutdown, etc.). All caught; none exit app.
- Verification: dotnet restore + build 0e0w + tests all pass + git diff --check. Manual: run with --diagnose-map-viewport, verify JSON files in %LocalAppData%\Anhei4Map\diagnostics\<run-id>\.
- Codex: per-task review after Claude acceptance.
- Next Executor: Cursor (after Codex plan approval + user approval)
- User Approval: PENDING_CODEX_REREVIEW_2

## TASK-01B: Capture Artifacts + Retention

- Type: DIAGNOSTIC_IMPLEMENTATION
- Depends on: TASK-01A completed + Codex reviewed + Claude accepted
- Objective: Generate capture-full.png, capture-annotated.png, capture-crop.png. Implement retention, capacity, failure, and integrity rules.
- Allowed: `src/Anhei4Map.App/Diagnostics/AnnotatedCapture.cs`, `src/Anhei4Map.App/Diagnostics/CaptureWriter.cs`, `src/Anhei4Map.App/Diagnostics/RetentionManager.cs` (exact filenames may vary but must be listed in dispatch)
- Forbidden: same as TASK-01A. Annotation draws on bitmap copy only — never via DOM injection.
- Threading: UI thread collects WebView2 data + CapturePreview. Bitmap Freeze before background use. JSON serialize, annotation render, PNG encode, file I/O on background. No long UI thread blocking.
- Retention algorithm (10 rules): 50MB/run, 250MB total. Sort by run-id timestamp or LastWriteTimeUtc. Delete oldest first. Reserve max 50MB for current run. After completion keep max 5 runs. Per-file size limit check before write. Exceed → skip remaining non-critical + mark SizeLimitExceeded. Cleanup failure → continue but enforce total cap. Multi-process: unique random suffix, never overwrite.
- Manifest integrity: manifest written early with complete=false. Each artifact: pending->success|failed|skipped + relativePath + sizeBytes + errorType + safeErrorMessage + timestamps. Critical (page.json, candidates.json, capture-full.png, capture-crop.png) all success + navigation consistency → complete=true. Non-critical failure (annotated, layers detail) → complete can still be true. Atomic manifest writes (tmp + replace).
- Annotated capture: draw numbered rectangles on bitmap copy matching candidates.json rank/index. Never modify DOM. Never touch capture-full.png.
- Exit: same as TASK-01A.
- Verification: same as TASK-01A plus verify 3 PNGs present and annotated numbers match candidates.json.
- Codex: per-task review.
- Next Executor: Cursor

## TASK-02: Diagnostic Evidence Analysis

- Type: DOCS_ANALYSIS
- Depends on: TASK-01A + TASK-01B completed with diagnostic artifacts available
- Allowed: docs/ under stage-04 only. No src/, tests/, csproj, solution, NuGet.
- Inputs: All diagnostic artifacts from a real Windows WebView2 run.
- Outputs: Root Cause, Evidence, Selected Candidate Identity, Why 3.17:1 Occurs, Rejected Hypotheses, Selected Strategy, Expected Change, Risks, Rollback Plan, Exact Allowed Production Files for TASK-03, Target Aspect Ratio, Approved Tolerance. User Approval Required.
- Process: Claude summarizes evidence -> Codex reviews conclusions -> Claude adjudicates -> User approves strategy -> TASK-03 dispatch.
- Next Executor: Claude (analysis) + Codex (review)
- User Approval: REQUIRED before TASK-03

## TASK-03: Viewport Correction

- Type: IMPLEMENTATION
- Depends on: TASK-02 completed + strategy approved by Codex + adjudicated by Claude + approved by User
- Objective: Implement the single approved strategy from TASK-02. No deviation.
- Allowed Paths: Frozen in TASK-02 output. Exact file list at dispatch time.
- Codex: per-task diff review after Claude acceptance. Fixes if needed. Re-review after fixes.
- Next Executor: Cursor

## TASK-04: Integration & Acceptance

- Type: INTEGRATION_ACCEPTANCE
- Depends on: TASK-03 completed + Codex reviewed
- Exit Criteria: --diagnose-map-viewport default off; no diagnostic run on normal start; no diagnostic loops; diagnostic failure does not block Overlay; Windows 10/11 smoke test; build 0e0w; all tests pass; diff check clean; before/after screenshots; full vertical marked areas visible; no map stretch or truncation; target ratio within TASK-02 approved tolerance; Codex Stage Final Review APPROVE.
- Codex: Stage Final Review. No merge or freeze before APPROVE.
- Next Executor: Cursor (implementation) + Codex (final review)
