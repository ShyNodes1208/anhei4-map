# Stage 04 Diagnostic Evidence Analysis

## Root Cause
The `#map` element (Leaflet container) is partially below the WebView2 viewport. Its `getBoundingClientRect().top = 417` places the map 417px below the viewport top. The 720px-tall CapturePreview only captures pixels y=0 through y=719. The crop bottom is clamped to 720, so only 720−417=303 pixels of the map are visible. The remaining portion of the map extends below the current viewport — the map itself is 720px tall in DOM, and only 303 of those 720 pixels are within the visible area.

## Evidence
- RendererWindow/WebView2: 1280×720
- CapturePreview: 1280×720 (scaleX=1, scaleY=1)
- #map: left=0, top=417, right=974.36, bottom=1137, width=974.36, height=720
- Crop: 0,417 → 975,720 = 975×303
- Ratio: 975/303 = 3.218
- DPI/ZoomFactor/devicePixelRatio all = 1, no scaling artifacts
- documentScrollHeight = 3569px (full page much taller than viewport)

## Why 3.17:1 Occurs
Map visible height = clamp(720, bottom=720) − floor(417) = 720 − 417 = 303. Width = 975. Ratio = 975/303 = 3.218. The map scrollHeight (868) is also larger than its bounding height (720), indicating even more content than the 720px DOM container.

## Rejected Hypotheses
- Wrong selector: #map is the correct Leaflet container (class "leaflet-container")
- DPI scaling: all scale values = 1
- ZoomFactor: 1
- Image stretch: Stretch=Uniform, no distortion
- Duplicate crop: single CroppedBitmap, verified in code audit
- Map actual height only 303: DOM height is 720, scrollHeight is 868

## Candidate Fixes

### Option A: Scroll map into viewport (Recommended)
Before CapturePreview, call `scrollIntoView({block:"start"})` on the production-selected #map element, wait for visual stability, re-query MapRegion, then proceed with existing capture/crop pipeline.
- Files: RendererWindow.xaml.cs only
- Reuses: existing MapRegion/CapturePreview/CroppedBitmap/Overlay
- Risk: tile re-render after scroll; needs re-query of MapRegion

### Option B: Enlarge RendererWindow temporarily
Set RendererWindow/WebView2 height to ≥1137px before capture, restore after.
- Files: RendererWindow.xaml + .cs
- Reuses: existing pipeline
- Risk: visible window resize during offscreen rendering; more files

### Option C: Full-page capture or tile stitching
Build scroll-stitched full-page capture.
- Files: multiple
- Reuses: nothing — new capture pipeline
- Risk: massive overengineering; rejected by Phase 03 FIX-04 experience

## Selected Fix
**Option A**: Bounded single scroll of the production-selected `#map` element into viewport top before capture. Single-file change, fully reuses existing pipeline, no new files/deps.

### Frozen Execution Boundaries
1. Use the existing production query result's selector and matchIndex to locate the element.
2. Execute exactly one `scrollIntoView({ block: "start", inline: "nearest" })` on that element per capture cycle.
3. Wait using the existing bounded visual readiness mechanism — no new wait framework or infinite retry.
4. Re-query MapRegion using the existing production query after the scroll.
5. Use the re-queried actual MapRegion values — no assumptions about top==0 or fixed crop size.
6. Re-query, CapturePreview, and crop must belong to the same navigation.
7. Navigation change during scroll/wait → exit this path, do not retry scroll.
8. Scroll or wait failure → no loop, no retry, no crash — reuse existing safe-failure behavior.
9. Do not modify CapturePreview or CroppedBitmap algorithms.
10. At most one scroll per capture cycle; no permanent scroll loop; no generic web automation.

## Expected Result
- Post-scroll MapRegion top must be less than pre-fix 417
- Post-scroll cropHeight must be greater than pre-fix 303
- finalAspectRatio must be less than pre-fix 3.217821782
- capture-crop.png must show more vertical map content than pre-fix
- Marked circle regions must be more complete than pre-fix
- Map must not be stretched; no previously visible core content lost
- If a sticky/fixed header obscures key map content, acceptance must fail
- Exact final size and ratio determined by re-queried actual MapRegion values
- No fixed pixel dimensions promised

## Risks
- Tile re-render delay after scroll (mitigated by existing visual readiness check)
- Must re-query MapRegion after scroll
- Must verify navigation unchanged

## Rollback
Revert TASK-03 implementation commit.

## Exact Allowed Production Files
- src/Anhei4Map.App/RendererWindow.xaml.cs

## New Dependencies
NONE

## Overengineering Check
PASS — one method modification, no new files/types/deps.

## User Approval Required
YES — before TASK-03 dispatch.
