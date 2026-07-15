# Stage 04 Plan Review Request

## Reviewer
Codex

## Review Mode
Read-only independent review

## Branch
feature/04-map-viewport-redesign

## Worktree
D:\AIProjects\anhei4-map-worktrees\stage-04-map-viewport-redesign

## Base Commit
de1bb3a1971c9f4a2fe9f1075d432bbe603c818b

## Planning Commit
7c9a636

## Documents To Review
- docs/plans/stage-04-map-viewport-redesign-plan.md
- docs/tasks/stage-04-tasks.md
- docs/tasks/current-task.md
- docs/status/project-status.md

## Review Objectives

1. Is the proposed diagnostic task (TASK-01) sufficient to locate the root cause of the ~3.17:1 ratio anomaly?
2. Is there any premature solution commitment before diagnostic evidence?
3. Are the DOM candidate diagnostic fields complete enough?
4. Will full WebView2 screenshot and DOM crop screenshot allow effective comparison?
5. Does writing diagnostics to `%LocalAppData%\Anhei4Map\diagnostics` introduce coverage, cleanup, or privacy risks?
6. How is the one-shot diagnostic mode enabled and disabled?
7. Can the diagnostic instrumentation itself alter page layout or affect capture results?
8. Should iframe, shadow DOM, canvas, SVG, image tiles, and parent-child hierarchy also be documented?
9. Should page URL, document.readyState, viewport meta, zoom factor, WebView2 RasterizationScale, browser zoom, and CSS transform also be recorded?
10. Are the listed prohibited approaches reasonable and complete?
11. Are the boundaries between TASK-01 through TASK-04 clearly defined?
12. Does TASK-02 genuinely select a strategy from evidence rather than begin development?
13. Are exception handling, file write failure, and capture failure behaviors specified?
14. Is there unnecessary complexity that could be simplified?
15. Can reliable diagnostics be achieved with smaller changes?
16. Is the plan consistent with the Windows-only constraint (no Linux/WSL)?

## Severity Levels
- CRITICAL — must block plan approval
- HIGH — must be addressed before implementation
- MEDIUM — should be addressed
- LOW — optional improvement
- INFO — observation, no action needed

## Finding Format
```
Finding ID:
Severity:
Document:
Location:
Problem:
Evidence:
Risk:
Required Change:
Suggested Fix:
```

## Final Verdict
- APPROVE — plan is complete and ready
- APPROVE_WITH_CHANGES — plan needs specific changes before dispatch
- REJECT — plan has critical gaps requiring redesign
