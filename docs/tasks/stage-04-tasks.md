# Stage 04 Task Map

All tasks: PENDING. Cursor BLOCKED until Codex plan approval + user approval.

---

## STAGE-04-TASK-01A: CLI Toggle + JSON Metrics

- Type: DIAGNOSTIC_IMPLEMENTATION
- Status: PLANNED_NOT_READY
- Objective: Diagnostic entry point. JSON-only metric collection. Default off. Pure observation.

### Allowed Paths (frozen — no wildcards)
- src/Anhei4Map.App/App.xaml.cs
- src/Anhei4Map.App/RendererWindow.xaml.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticRunner.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticRun.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticManifest.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticModels.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticJsonWriter.cs

### Forbidden Paths
OverlayWindow.xaml, OverlayWindow.xaml.cs, MainWindow*, DomainPolicy, MapRegion, Infrastructure/**, tests/**, *.csproj, solution, NuGet config. No DOM modify, resize, scroll, zoom, click, fullscreen, RendererWindow resize.

### RendererWindow Diagnostic Interface (frozen)
```csharp
internal Task<DiagnosticSnapshot?> TryCreateDiagnosticSnapshotAsync(DiagnosticRun run);
internal Task<DiagnosticCaptureSet?> TryCaptureDiagnosticImagesAsync(DiagnosticRun run, DiagnosticSnapshot snapshot);
```
- TryCreateDiagnosticSnapshotAsync: JSON data only. All WebView2 calls on UI Dispatcher. Returns frozen immutable snapshot. All failures return null (no app-level throw).
- TryCaptureDiagnosticImagesAsync: Reuses production CapturePreview/crop pipeline. Validates navigation identity matches run+snapshot. Returns frozen/safe-copied BitmapSource. No file I/O.

### Shared 01A/01B Run Contract (frozen)
1. --diagnose-map-viewport enables one-shot DiagnosticRunner per process.
2. 01A creates: run-id, run directory, navigation identity, initial manifest, frozen DiagnosticSnapshot.
3. 01B reuses same: DiagnosticRun, run-id, run directory, manifest, navigation generation, URL, frozen snapshot.
4. 01B does NOT re-collect JSON. 01B does NOT re-create run-id.
5. Sequence: NavigationReady -> one-shot guard -> create run -> snapshot -> write JSON -> (01B) capture bitmaps -> annotate -> write PNG -> finalize manifest -> continue Overlay.
6. After each await: check generation+URL+_isClosed. Change -> run aborted, manifest.complete=false, remaining skipped, no cross-navigation mixing.
7. 01A alone (01B pending): overlay normal, JSON only, manifest.complete=false, PNG status skipped.

### Diagnostic Error Contract (30 rows)

| # | Detection | Artifact Status | Manifest Effect | Overlay Effect | Retry | Safe Error |
|----|-----------|----------------|-----------------|----------------|-------|------------|
| 1 | CLI arg invalid | — | — | normal | none | — |
| 2 | run dir create fail | all skipped | complete=false | normal | none | IOException type |
| 3 | old dir enum fail | — | note in manifest | normal | none | IOException type |
| 4 | old dir delete fail | — | note in manifest | normal | none | IOException type |
| 5 | manifest init write fail | all skipped | — | normal | none | IOException type |
| 6 | ExecuteScriptAsync fail | JSON skipped | status=failed | normal | none | exception type |
| 7 | JS returns invalid JSON | JSON skipped | status=failed | normal | none | parse error |
| 8 | JSON deserialize fail | JSON skipped | status=failed | normal | none | JsonException type |
| 9 | JSON serialize fail | JSON skipped | status=failed | normal | none | JsonException type |
| 10 | page not ready | JSON skipped | status=failed | normal | none | "not ready" |
| 11 | WebView2 not init | all skipped | complete=false | normal | none | "not initialized" |
| 12 | WebView2 closed | all skipped | complete=false | normal | none | "closed" |
| 13 | App shutdown | all skipped | complete=false | normal | none | "shutdown" |
| 14 | new navigation | run aborted | complete=false | normal | none | "navigation changed" |
| 15 | URL changed | run aborted | complete=false | normal | none | "URL changed" |
| 16 | candidates empty | JSON with empty array | complete=depends | normal | none | "empty" |
| 17 | candidates >100 | truncated to 100 | truncated=true | normal | none | "truncated" |
| 18 | ancestors >12 | truncated to 12 | truncated=true | normal | none | "truncated" |
| 19 | iframe cross-origin | inaccessible marker | — | normal | none | "cross-origin" |
| 20 | shadow root closed | inaccessible marker | — | normal | none | "closed" |
| 21 | CapturePreviewAsync fail | PNG skipped | status=failed | normal | none | exception type |
| 22 | BitmapDecoder fail | PNG skipped | status=failed | normal | none | exception type |
| 23 | Bitmap Freeze fail | PNG skipped | status=failed | normal | none | exception type |
| 24 | annotation fail | annotated skipped | status=failed | normal | none | exception type |
| 25 | PNG encode fail | PNG skipped | status=failed | normal | none | exception type |
| 26 | file write fail | artifact skipped | status=failed | normal | none | IOException type |
| 27 | temp file replace fail | artifact skipped | status=failed | normal | none | IOException type |
| 28 | >50MB this run | remaining skipped | SizeLimitExceeded | normal | none | "size limit" |
| 29 | >250MB total | remaining skipped | SizeLimitExceeded | normal | none | "size limit" |
| 30 | background task cancel | remaining skipped | complete=false | normal | none | "cancelled" |

Uniform: no app exit, no Overlay block, no infinite retry, safeErrorMessage excludes URL query/fragment/cookies/auth/password/local username/absolute paths.

### JSON Size Caps
page.json: 1MB. candidates.json: 5MB. layers.json: 5MB. manifest.json: 1MB. Exceed -> truncate bounded collections + truncated=true. Still exceed -> failed/skipped.

### Iframe Rules
Max 20 iframes. Each: index, srcOriginOnly, boundingRect, sameOrigin, accessible, accessErrorCode, candidateCountWithinFrame. Cross-origin: no internal access. src: origin only.

### Shadow DOM Rules
Only along candidate path + top 10 candidates. Open root max 4 levels deep. Closed root: host tag/id/class + inaccessible=true. No full-page scan.

### Inputs
--diagnose-map-viewport CLI arg (default off).

### Outputs
run-id directory with manifest.json (complete=false initially), page.json, candidates.json, layers.json. No PNGs.

### Exit Criteria
JSON files written or failed + manifest updated. Diagnostic failure does not block Overlay. One-shot per process.

### Verification
dotnet restore + build 0e0w + tests all pass + git diff --check. Manual: run with --diagnose-map-viewport, verify JSON in diagnostics/<run-id>/.

### Codex Gate
Per-task review after Claude acceptance. Must pass before TASK-01B dispatch.

### Next Executor
Cursor (after Codex plan approval + user approval).
### User Approval
PENDING_CODEX_FINAL_REREVIEW.

---

## STAGE-04-TASK-01B: Capture Artifacts + Retention

- Type: DIAGNOSTIC_IMPLEMENTATION
- Status: BLOCKED_BY_TASK_01A
- Dependencies: TASK-01A completed + Codex reviewed + Claude accepted.

### Allowed Paths (frozen — no wildcards)
- src/Anhei4Map.App/RendererWindow.xaml.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticRunner.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticRun.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticManifest.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticModels.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticArtifactWriter.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticImageAnnotator.cs
- src/Anhei4Map.App/Diagnostics/DiagnosticRetentionPolicy.cs

### Forbidden
Same as TASK-01A + additionally: App.xaml.cs, OverlayWindow production, MapRegion production algorithm.

### Retention Algorithm (10 rules)
1. Current run never deleted. 2. Enumerate root excluding current. 3. Sort key: parseable run-id timestamp descending; fallback Directory.LastWriteTimeUtc; final tie-break OrdinalIgnoreCase. Oldest first. 4. Before create: delete oldest until old total <=200MB AND old count <=4. Reserve 50MB for current. 5. Cleanup failure + still >200MB: allow current run creation; skip non-critical large files; critical files still check 250MB total; can't write -> skipped/SizeLimitExceeded. 6. Total = all old + incomplete + current written. 7. Pre-write check: current <=50MB AND total <=250MB. 8. Exceed: keep current successes; skip remaining; SizeLimitExceeded. 9. After completion: keep max 5 runs. 10. User may delete diagnostics directory.

### Threading
UI thread: WebView2 data + CapturePreview. Bitmap Freeze. Background: JSON serialize, annotation render, PNG encode, file I/O. No long UI blocking.

### Annotation
Draw numbered rectangles on capture-full bitmap COPY matching candidates.json rank/index. Never DOM injection. Never modify capture-full.png.

### Manifest Integrity
Atomic write (tmp + replace). Each artifact: pending->success|failed|skipped + relativePath + sizeBytes + errorType + safeErrorMessage + timestamps. Critical (page.json, candidates.json, capture-full.png, capture-crop.png) all success + navigation consistency -> complete=true. Non-critical failure -> complete may still be true.

### Inputs
Same DiagnosticRun, run-id, run directory, manifest, navigation identity, frozen snapshot from TASK-01A.

### Outputs
capture-full.png, capture-annotated.png, capture-crop.png. Updated manifest.

### Exit Criteria
Same as TASK-01A + 3 PNGs present + annotated numbers match candidates.json.

### Verification
Same as TASK-01A + verify PNG annotated numbers.

### Codex Gate
Per-task review. Must pass before TASK-02.
### Next Executor: Cursor

---

## STAGE-02-TASK-02: Evidence Analysis

- Type: DOCS_ANALYSIS
- Status: BLOCKED_BY_DIAGNOSTIC_OUTPUT
- Allowed: docs/ under stage-04 only
- Forbidden: src/**, tests/**, *.csproj, solution, NuGet
- Inputs: All diagnostic artifacts from real Windows WebView2 run
- Outputs: Root Cause, Evidence, Selected Candidate, Why 3.17:1, Rejected Hypotheses, Selected Strategy, Expected Change, Risks, Rollback Plan, Exact Allowed Production Files, Target Ratio, Approved Tolerance
- Process: Claude summary -> Codex review -> Claude adjudication -> User approval -> TASK-03
- User Approval: REQUIRED
- Codex Gate: Required

---

## STAGE-03-TASK-03: Viewport Correction

- Type: IMPLEMENTATION
- Status: BLOCKED_BY_TASK_02_APPROVAL
- Allowed Paths: To be frozen by TASK-02. No dispatch until exact paths recorded.
- Objective: Implement exactly one approved strategy. No deviation.
- Codex Gate: Per-task diff review + re-review after fixes.
- Next Executor: Cursor

---

## STAGE-04-TASK-04: Integration & Acceptance

- Type: INTEGRATION_ACCEPTANCE
- Status: BLOCKED_BY_TASK_03
- Allowed: Stage 04 docs/status/review. Production code only for confirmed defect fixes from TASK-03; each fix requires its own dispatch.
- Forbidden: New strategies, new features, unapproved behavior, new deps, NuGet, solution changes.
- Exit Criteria (frozen):
  1. --diagnose-map-viewport default off — no diagnostics run on normal start
  2. No diagnostic loops, no diagnostic screenshot loops, no new permanent CapturePreview loops
  3. Diagnostic failure does not block Overlay
  4. Windows 10/11 native WebView2 smoke test (no Linux, no WSL, no server)
  5. Release build 0 errors, 0 warnings
  6. All tests pass
  7. git diff --check clean
  8. Before/after screenshots
  9. Full vertical marked areas visible
  10. No stretch, no core region truncation
  11. Ratio within TASK-02 approved tolerance
  12. Codex Stage Final Review APPROVE
- Verification: PowerShell restore + build 0e0w + tests all pass + diff check clean + manual screenshot comparison.
- Codex Gate: Stage Final Review required. No merge or freeze before APPROVE.
