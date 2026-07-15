# Stage 02: App Shell + Core State Logic

## Stage Info

- Stage: STAGE-02
- Name: App Shell + Core State Logic
- Slug: app-shell
- Branch: feature/02-app-shell
- Worktree: D:\AIProjects\anhei4-map-worktrees\stage-02-app-shell
- Base: a48bf37725879ee4172e2321d21491e563461846 (Stage 01 final)

## Goal

将 Stage 01 的 Core 和 Infrastructure 层集成到可运行的 WPF 应用外壳中，并实现状态机、热键分发等 Core 层纯逻辑。应用启动后显示无边框置顶窗口，加载 WebView2 并导航到 https://helltides.com/。

## User Value

用户首次看到可运行的应用窗口——无边框、始终置顶、加载 Helltides.com 地图页面。应用有完整的启动序列（单实例检测 → Runtime 检测 → 设置加载 → 窗口创建 → WebView2 初始化 → 导航），但尚未激活全局热键和状态切换功能。

## Deliverables

1. `WindowStateMachine` (Core) — 三状态转换表 + guard 逻辑
2. `HotkeyDispatcher` (Core) — 热键 ID → Command 解析
3. `IWin32Interop` 接口 (Core) — Win32 调用的薄抽象
4. `Win32Interop` 实现 (App) — P/Invoke 适配器
5. `App.xaml.cs` 单实例 Mutex + WebView2 Runtime 检测
6. `MainWindow` — 无边框、Topmost、WebView2 宿主、窗口尺寸/位置恢复
7. `WebView2Setup` — 环境初始化、导航白名单、事件拦截

## Scope

- WPF 无边框置顶窗口
- WebView2 嵌入 helltides.com
- 单实例 Mutex
- WebView2 Runtime 缺失检测 + MessageBox
- 窗口尺寸/位置恢复
- 导航白名单 (DomainPolicy)
- 窗口状态机 (Hidden/Locked/Edit)
- 热键命令分发器
- 日志记录启动/关闭事件
- 设置加载和保存

## Non-Goals

- 全局热键注册 (RegisterHotKey) — Stage 03
- 鼠标穿透 (WS_EX_TRANSPARENT) — Stage 03
- EditOverlay 控制面板 — Stage 03
- 置顶维持定时器 — Stage 03
- 状态切换的实际执行 (SetWindowLong etc.) — Stage 03
- 加载失败重试 UI — Stage 03
- System tray — 不在 MVP 范围
- EditOverlay 位置/大小/透明度/缩放控件 — Stage 03

## Technical Approach

### Architecture

WPF App 层引用 Core 和 Infrastructure。Core 层增加状态机和热键分发器。Win32 调用通过 `IWin32Interop` 接口隔离。

### Dependency: Microsoft.Web.WebView2

WPF 项目新增 NuGet 引用 `Microsoft.Web.WebView2`（仅 App 项目）。

### Startup Sequence (this stage)

1. SingleInstance Mutex → collision → Exit(0)
2. WebView2 Runtime detection → missing → MessageBox + Exit(1)
3. Load settings (JsonSettingsStore)
4. Window bounds normalization (WindowBoundsNormalizer)
5. Create MainWindow (WindowStyle=None, Topmost=true, 640x360)
6. WebView2 async init + nav to helltides.com
7. Configure navigation whitelist (DomainPolicy)
8. Enter initial state (Locked, visual only)

### WindowStateMachine

纯逻辑：三个状态 (Hidden, Locked, Edit)，四个合法转换 (Hidden↔Locked↔Edit)，两个非法转换 (Hidden→Edit, Edit→Hidden)。幂等切换 (相同状态 toggle 不异常)。

### HotkeyDispatcher

绑定列表 → Command 映射。未知 ID 返回 Unknown。空列表不抛异常。重复 ID 以最后绑定为准。

## Security Boundary

延续 Stage 01 全部 7 条红线。新增 WebView2 安全配置：
- Navigation whitelist: helltides.com only
- Popups blocked
- Downloads blocked
- DevTools disabled
- Password autosave disabled
- Script dialogs disabled
- Web messaging disabled

## Dependencies

- Stage 01 (Core + Infrastructure 完整)
- .NET 8 SDK
- WebView2 Runtime (用户安装)

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| WebView2 在无边框窗口渲染异常 | Medium | High | 手动验证 M1 |
| NuGet 包版本兼容性 | Low | Low | 使用稳定版本 |
| P/Invoke 签名错误 | Medium | Medium | 代码审核确认 Win32 签名 |

## Test Strategy

- Core: TDD (WindowStateMachine 12 tests, HotkeyDispatcher 6 tests)
- Infrastructure: 现有测试继续通过
- App: SCAFFOLD (build verification only, per design — thin WPF/P/Invoke glue)

## Task List

| # | Component | Type | Tests |
|---|-----------|------|-------|
| 1 | WindowStateMachine | TDD | ~12 |
| 2 | HotkeyDispatcher | TDD | ~6 |
| 3 | IWin32Interop Interface | SCAFFOLD | 0 |
| 4 | Win32Interop Adapter | SCAFFOLD | 0 |
| 5 | App Single Instance + WebView2 NuGet + Runtime Check | SCAFFOLD | 0 |
| 6 | MainWindow Shell + WebView2 Control | SCAFFOLD | 0 |
| 7 | WebView2 Setup + Navigation | SCAFFOLD | 0 |

## Acceptance Gate

- `dotnet build -c Release` 0 errors, 0 warnings
- `dotnet test` 全部通过 (140+: 122 existing + 18+ new)
- Core 层无 WPF/Win32/WebView2 依赖
- WebView2 正确加载 helltides.com（手动验证）
- 单实例检测生效
- Runtime 缺失提示正常

## Rollback

Stage 02 仅添加新文件和修改 App 项目。回退 = 切回 main 分支。
