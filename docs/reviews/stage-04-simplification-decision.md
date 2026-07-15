# Stage 04 Simplification Decision

## Decision
User-directed simplification. Remove all unnecessary diagnostic infrastructure.

## Removed
- Multi-run retention system (50MB/250MB caps, automated cleanup)
- Complex manifest state machine (pending/success/failed/skipped per artifact)
- 30-row exception matrix
- Annotated capture with candidate numbering
- Iframe/shadow DOM traversal
- Layer classification summaries
- DiagnosticRunner, DiagnosticRun, DiagnosticManifest, DiagnosticModels, DiagnosticJsonWriter, DiagnosticArtifactWriter, DiagnosticImageAnnotator, DiagnosticRetentionPolicy
- Multiple diagnostic service/class abstractions
- Background task threading framework

## Kept
- Single diagnostics.json (flat JSON, minimal fields)
- capture-full.png + capture-crop.png (same navigation, same run)
- Single diagnostic class: MapViewportDiagnostics.cs
- Single output directory: diagnostics/latest/ (overwritten each run)
- Codex review gates preserved at all stages

## Rationale
The 3.17:1 ratio problem requires understanding which candidate element is selected and what its bounding box is vs the full capture. A single diagnostics.json with candidate list, page metrics, WPF metrics, and crop coordinates, plus two PNGs, is sufficient.
