# STAGE-02-TASK-06

- Component: MainWindow + WebView2 Control Shell
- Type: SCAFFOLD | Status: DONE
- Implementation Commit: b0673a5703b29a974ca6e58cd207368541400242
- Files: App.xaml, App.xaml.cs, MainWindow.xaml, MainWindow.xaml.cs
- Window: WindowStyle=None, Topmost=True, ShowInTaskbar=False, no AllowsTransparency
- WS_EX_TOOLWINDOW: SourceInitialized, always OR, SetWindowPos refresh
- Settings: LoadAsync.GetAwaiter().GetResult(), ScreenInfo→WorkArea, Normalize, apply Left/Top/Width/Height/Opacity
- WebView2: Named "webView", no Source, no events, no navigation
- Build: Release 0e0w | Tests: 160/160 PASS
- Claude: PASS
