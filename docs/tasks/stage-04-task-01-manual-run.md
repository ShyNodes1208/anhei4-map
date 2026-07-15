# Stage 04 TASK-01 Manual Diagnostic Run

## Worktree
D:\AIProjects\anhei4-map-worktrees\stage-04-map-viewport-redesign

## Diagnostic Toggle
--diagnose-map-viewport

## Output Directory
%LocalAppData%\Anhei4Map\diagnostics\latest\

## Expected Files (3)
- diagnostics.json
- capture-full.png
- capture-crop.png

## Steps

1. Close any running Anhei4Map program.
2. Open PowerShell.
3. Clean old diagnostics:
```powershell
$diag = Join-Path $env:LOCALAPPDATA "Anhei4Map\diagnostics\latest"
Remove-Item $diag -Recurse -Force -ErrorAction SilentlyContinue
```
4. `cd` to the Stage 04 worktree:
```powershell
cd "D:\AIProjects\anhei4-map-worktrees\stage-04-map-viewport-redesign"
```
5. Run with diagnostic toggle:
```powershell
dotnet run --project src\Anhei4Map.App\Anhei4Map.App.csproj -c Release -- --diagnose-map-viewport
```
6. Wait for Overlay to appear showing the map.
7. Close the application.
8. Verify output:
```powershell
Get-ChildItem $diag
```

## Pass Criteria
- App starts normally, overlay displays map
- App does not crash or exit due to diagnostics
- `latest/` contains exactly 3 files
- All 3 files open correctly

## After Successful Run
Provide the 3 files to Claude for STAGE-04-TASK-02 analysis.
