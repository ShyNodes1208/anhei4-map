# Stage 04 Diagnostic Evidence Review Request

## Reviewer
Codex (read-only independent review)

## Inputs
- diagnostics.json (1 candidate: #map, top=417, bottom=1137, crop=975×303)
- capture-full.png (1280×720)
- capture-crop.png (975×303)

## Analysis
docs/reviews/stage-04-diagnostic-analysis.md

## Key Question
Is the root cause (map partially below viewport) supported by the evidence? Is Option A (scrollIntoView) the minimal fix?

## Review Objectives
- Verify root cause is evidence-based
- Verify 3.218 ratio calculation
- Confirm selector (#map) is correct Leaflet container
- Assess whether Selected Fix (Option A) is minimal
- Check that Allowed Production Files (1 file) are sufficient and minimal
- Check for overengineering in proposed fix
