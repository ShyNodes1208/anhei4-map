# STAGE-02-TASK-05

- Component: App Single Instance + WebView2 Runtime Detection
- Type: SCAFFOLD | Status: DONE
- Implementation Commit: 1fe414667089a380a21a2a487c9965c28b2109ed
- Files: App.csproj (NuGet), App.xaml.cs (Mutex + Runtime)
- NuGet: Microsoft.Web.WebView2 1.0.2903.40
- Mutex: Global\Anhei4Map_SingleInstance, static field, createdNew=false→Dispose+Shutdown
- Runtime: GetAvailableBrowserVersionString, null/whitespace→missing dialog, other exceptions→error dialog
- Build: Release 0e0w | Tests: 160/160 PASS
- Claude: PASS
