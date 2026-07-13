# Stage 04: Map Viewport Redesign

## Stage Info
- Stage: STAGE-04 | Name: Map Viewport Redesign
- Slug: map-viewport-redesign | Branch: feature/04-map-viewport-redesign
- Base: de1bb3a1971c9f4a2fe9f1075d432bbe603c818b (v0.3.0)

## Known Limitation
Captured map region ~3.17:1 ratio, vertical content cut off.

## Diagnostic Toggle
`--diagnose-map-viewport` CLI argument. Default off. One-shot per process. No persistence.

## Diagnostic Output
`%LocalAppData%\Anhei4Map\diagnostics\<yyyyMMdd-HHmmss-fff-<random6>>\`
- manifest.json, page.json, candidates.json, layers.json
- capture-full.png, capture-annotated.png, capture-crop.png
- Retention: max 5 runs, 50MB/run, 250MB total
- Privacy: no cookies, localStorage, sessionStorage, headers, auth, credentials
- Full-page screenshots may capture visible page state (documented in release notes)

## Candidate Schema (33 fields per element)
selector, selectorMatchIndex, tagName, id, className, parentTagName, parentId, parentClassName, boundedDomPath, boundedAncestorChain, left, top, right, bottom, width, height, clientWidth, clientHeight, offsetWidth, offsetHeight, scrollWidth, scrollHeight, display, visibility, opacity, position, overflow, overflowX, overflowY, zIndex, transform, transformOrigin, zoom, childCount, area, rank, selected, selectionReason
- Types: double or explicit int; unavailable = null; no empty-string masking
- Cap: 100 candidates, 12 ancestor levels, no full DOM export

## Layer Summary
Per selected candidate and limited ancestors/children: map root, viewport, parent layout, pane, tile layer, marker layer, control layer, canvas, SVG, image tile
- Canvas: CSS size, backing-store size, bounding rect
- SVG: viewBox, bounding rect, child count
- Tile: total/loaded count, naturalWidth/Height, displayed size, className, nearest pane
- Marker/control: count, pane/layer name, z-index
- Caps on child summary depth/width; no full DOM traversal

## Page Metrics
url, title, document.readyState, window.innerWidth/Height, window.outerWidth/Height, visualViewport.width/height/scale, devicePixelRatio, documentElement.clientWidth/Height/scrollWidth/ScrollHeight, body.clientWidth/Height/scrollWidth/ScrollHeight, viewport meta content, page zoom, media query results: max-width:768px, max-width:1024px, max-width:1280px, min-width:1281px

## WPF/WebView2/Bitmap Metrics (all with explicit unit labels)
- WPF DIP: RendererWindow Width/Height/ActualWidth/ActualHeight, WebView2 ActualWidth/ActualHeight
- DPI: VisualTreeHelper.GetDpi scale values, PresentationSource transforms
- WebView2: CoreWebView2.ZoomFactor, RasterizationScale (null if unavailable)
- Bitmap px: CapturePreview PixelWidth/Height, scaleX/Y, MapRegion CSS px, pre-floor float crop bounds, clamped Int32Rect, cropped PixelWidth/Height, final ratio
- Each field tagged with unit: CSS px | WPF DIP | bitmap px | dimensionless

## TASK-01 Absolute Prohibitions
DOM modification, resize, scroll, page zoom, map interaction, window size experiments. Pure observation only.

## Prohibited Approaches (3 Tiers)
- Tier A (TASK-01 absolute): DOM modify, resize, scroll, zoom, map interact, window resize
- Tier B (default-rejected; reassessable with evidence + Codex + user approval in TASK-03): RendererWindow resize, site fullscreen mode, ZoomFactor adjust, DOM selector fix
- Tier C (permanent): Leaflet internals, JS object guessing, process injection, game memory, API redraw, Stretch=Fill, bitmap non-proportional stretch

## Codex Review Gates
Plan Review -> Per-Task (TASK-01/03/04) -> TASK-02 Strategy -> Stage Final. Codex APPROVE required at each gate.

## Task Sequence
- TASK-01: Map DOM and Capture Diagnostics (DIAGNOSTIC_IMPLEMENTATION)
- TASK-02: Diagnostic Evidence Analysis and Strategy Selection (DOCS_ANALYSIS)
- TASK-03: Viewport/Map-Region Correction (IMPLEMENTATION)
- TASK-04: Integration and Manual Acceptance (INTEGRATION_ACCEPTANCE)

## Windows-Only Verification
All: Windows 10/11, PowerShell, .NET/WPF/WebView2. No Linux/WSL. Per task: restore + build 0e0w + all tests pass + git diff --check clean + manual smoke test.
