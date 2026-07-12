# Project status

- Main branch: main | Phase: STAGE-01-FOUNDATION
- Branch: feature/01-foundation | Worktree: D:\AIProjects\anhei4-map-worktrees\stage-01-foundation
- Completed: TASK-01..07 | Active: STAGE-01-TASK-08 (BEHAVIOR, READY)
- Tests: 70/70 PASS | Build: 0 errors

## Retry parameters (TASK-07)
Sequence: [0,1000,2000,4000,8000,30000]ms | Max retries: 10 (attempt 0-9)
Total max loads: 11 (1 initial + 10 retries) | attempt 0 = immediate (0ms)

## Stage 03 integration constraints
- Single retry schedule at a time
- Increment attempt on each failure
- Reset attempt to 0 on success
- No-activity map is NOT a load failure (do not retry)
- Manual refresh cancels pending retry
- App exit cancels pending retry

## Deferred
TASK-04B: Validate-after-Load | Log rotation: Stage 04