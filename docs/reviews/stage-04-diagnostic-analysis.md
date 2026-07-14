# Stage 04 Diagnostic Evidence Analysis

## Root Cause
The `#map` element (Leaflet container) is partially below the WebView2 viewport. Its `getBoundingClientRect().top = 417` places the map 417px below the viewport top. The 720px-tall CapturePreview only captures pixels y=0 through y=719. The crop bottom is clamped to 720, so only 720−417=303 pixels of the map are visible. The map itself is 720px tall in DOM, fully hidden below the fold.

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
**Option A**: Scroll `#map` into viewport top before capture. Single-file change, fully reuses existing pipeline, no new files/deps.

## Expected Result
- After scroll, MapRegion top ≈ 0
- CapturePreview 1280×720 captures full map height (720 DOM pixels)
- Crop ≈ 974×720, ratio ≈ 1.35:1
- Overlay displays full vertical marked areas proportional at ≤400×250

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
