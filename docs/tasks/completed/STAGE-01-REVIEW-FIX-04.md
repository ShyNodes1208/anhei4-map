# STAGE-01-REVIEW-FIX-04

- Fix: STAGE-01-REVIEW-FIX-04
- Finding: S01-004 (MEDIUM)
- Status: DONE
- Implementation Commit: a8d7411db742a82305c02bf0698b80bc0ae9615d
- RED Evidence: Max size clamped to workAreas[0] regardless of window location; larger secondary windows incorrectly resized
- GREEN Tests: WindowBoundsNormalizerTests 24/24 PASS
- Full Tests: 113/113 PASS
- Release Build: 0 errors, 0 warnings
- Target WorkArea Rule: Largest intersection area wins
- Tie-Break Rule: Smaller workAreas index wins (> operator, not >=)
- Fallback: workAreas[0] when no positive intersection
- Empty List: Existing safe fallback preserved
- Negative Coordinates: Fully supported via GetIntersectionArea
- Independent Clamping: Width and height clamped independently to target work area
- Regression Tests Added: 10
  1. LargerSecondaryScreen_DoesNotClampToSmallerPrimary
  2. OversizedWindowOnSecondary_ClampsToSecondaryWorkArea
  3. NegativeCoordinateSecondary_IsSelectedByIntersection
  4. WindowSpanningTwoScreens_SelectsLargestIntersection
  5. EqualIntersection_UsesStableTieBreak
  6. FullyOffscreen_UsesPrimaryFallback
  7. EmptyWorkAreas_UsesExistingSafeFallback
  8. ExistingValidPrimaryWindow_RemainsUnchanged
  9. ExistingPartialVisibilityThresholdTests_RemainPassing
  10. WidthAndHeightAreClampedIndependently
- Known Limitations: None
- Claude Acceptance: PASS — FindBestWorkArea uses > for deterministic tie-break; primary retained for fallback reset only; all existing tests pass without modification
