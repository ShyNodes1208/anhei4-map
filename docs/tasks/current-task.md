# 当前任务

## 任务编号
STAGE-01-TASK-01

## 任务类型
SCAFFOLD

## 状态
READY

## 所属阶段
STAGE-01-FOUNDATION

## 前置条件
无（绿色场项目）。当前分支 `feature/01-foundation` 基于 `origin/main`(@44dbada)，无 src/、tests/。

## 任务目标
创建 .NET 8 解决方案骨架，含四个项目：
- `anhei4-map.sln`（解决方案）
- `src/Anhei4Map.App/`（WPF 应用程序，.NET 8）
- `src/Anhei4Map.Core/`（类库，.NET 8）
- `src/Anhei4Map.Infrastructure/`（类库，.NET 8）
- `tests/Anhei4Map.Tests/`（xUnit 测试项目，.NET 8）

建立最小项目引用链：
- Anhei4Map.App → Anhei4Map.Core
- Anhei4Map.App → Anhei4Map.Infrastructure
- Anhei4Map.Tests → Anhei4Map.Core
- Anhei4Map.Tests → Anhei4Map.Infrastructure

验证 `dotnet restore`、`dotnet build -c Release`、`dotnet test -c Release --no-build` 全部成功。

## 允许修改
- Create: `anhei4-map.sln`
- Create: `src/Anhei4Map.App/Anhei4Map.App.csproj`
- Create: `src/Anhei4Map.App/MainWindow.xaml`
- Create: `src/Anhei4Map.App/MainWindow.xaml.cs`
- Create: `src/Anhei4Map.App/App.xaml`
- Create: `src/Anhei4Map.App/App.xaml.cs`
- Create: `src/Anhei4Map.App/AssemblyInfo.cs`
- Create: `src/Anhei4Map.Core/Anhei4Map.Core.csproj`
- Create: `src/Anhei4Map.Core/Class1.cs`（生成后必须删除）
- Create: `src/Anhei4Map.Infrastructure/Anhei4Map.Infrastructure.csproj`
- Create: `src/Anhei4Map.Infrastructure/Class1.cs`（生成后必须删除）
- Create: `tests/Anhei4Map.Tests/Anhei4Map.Tests.csproj`
- Create: `tests/Anhei4Map.Tests/Usings.cs`
- Create: `tests/Anhei4Map.Tests/UnitTest1.cs`（生成后必须删除）

## 禁止修改
- 不得修改 `docs/` 下任何文件（含设计文档、计划、状态文件）
- 不得修改 `CLAUDE.md`、`AGENTS.md`、`.gitignore`、`README.md`
- 不得添加 NuGet 包（含 WebView2——Stage-02 才引入）
- 不得在 Core 或 Infrastructure 中引用 WPF/Win32 命名空间
- 不得实现业务逻辑

## 必须新增或修改的测试
无。SCAFFOLD 类型任务不强制编造 RED 失败测试。验证方式为 `dotnet build` + `dotnet test` 命令级验证。

## RED 预期
无。

## GREEN 最小实现

```powershell
# Step 1: Create solution
dotnet new sln -n anhei4-map

# Step 2: Create projects
dotnet new wpf -n Anhei4Map.App -o src/Anhei4Map.App -f net8.0
dotnet new classlib -n Anhei4Map.Core -o src/Anhei4Map.Core -f net8.0
dotnet new classlib -n Anhei4Map.Infrastructure -o src/Anhei4Map.Infrastructure -f net8.0
dotnet new xunit -n Anhei4Map.Tests -o tests/Anhei4Map.Tests -f net8.0

# Step 3: Add to solution
dotnet sln add src/Anhei4Map.App/Anhei4Map.App.csproj
dotnet sln add src/Anhei4Map.Core/Anhei4Map.Core.csproj
dotnet sln add src/Anhei4Map.Infrastructure/Anhei4Map.Infrastructure.csproj
dotnet sln add tests/Anhei4Map.Tests/Anhei4Map.Tests.csproj

# Step 4: Add project references (App → Core, App → Infrastructure)
dotnet add src/Anhei4Map.App/Anhei4Map.App.csproj reference src/Anhei4Map.Core/Anhei4Map.Core.csproj
dotnet add src/Anhei4Map.App/Anhei4Map.App.csproj reference src/Anhei4Map.Infrastructure/Anhei4Map.Infrastructure.csproj

# Step 5: Add project references (Tests → Core, Tests → Infrastructure)
dotnet add tests/Anhei4Map.Tests/Anhei4Map.Tests.csproj reference src/Anhei4Map.Core/Anhei4Map.Core.csproj
dotnet add tests/Anhei4Map.Tests/Anhei4Map.Tests.csproj reference src/Anhei4Map.Infrastructure/Anhei4Map.Infrastructure.csproj

# Step 6: Remove template files
Remove-Item src/Anhei4Map.Core/Class1.cs
Remove-Item src/Anhei4Map.Infrastructure/Class1.cs
Remove-Item tests/Anhei4Map.Tests/UnitTest1.cs
```

## 验证命令
```powershell
dotnet restore anhei4-map.sln
dotnet build anhei4-map.sln -c Release
dotnet test anhei4-map.sln -c Release --no-build
```

期望输出：
- `dotnet restore`: 无错误
- `dotnet build -c Release`: "Build succeeded." 0 Error(s)
- `dotnet test -c Release --no-build`: 无测试运行（或 "Test Run Successful"），0 Failed

## 人工验证
- [ ] `anhei4-map.sln` 存在
- [ ] `src/Anhei4Map.App/Anhei4Map.App.csproj` 存在，目标框架 `net8.0`，无 NuGet 引用
- [ ] `src/Anhei4Map.Core/Anhei4Map.Core.csproj` 存在，目标框架 `net8.0`
- [ ] `src/Anhei4Map.Infrastructure/Anhei4Map.Infrastructure.csproj` 存在，目标框架 `net8.0`
- [ ] `tests/Anhei4Map.Tests/Anhei4Map.Tests.csproj` 存在，引用 Core + Infrastructure
- [ ] 所有模板 Class1.cs / UnitTest1.cs 已删除
- [ ] `dotnet restore` 零错误
- [ ] `dotnet build -c Release` 零错误
- [ ] `dotnet test -c Release --no-build` 零失败

## 完成标准
1. 四个项目均存在于解决方案中
2. 引用关系：App→Core, App→Infrastructure, Tests→Core, Tests→Infrastructure
3. 零 NuGet 包引用（不含 WebView2）
4. 无模板残留文件
5. `dotnet build -c Release` 零错误
6. `dotnet test -c Release --no-build` 零失败
7. `git commit` + `git push origin feature/01-foundation`

## Git 提交信息
```
chore: scaffold solution with App, Core, Infrastructure, and xUnit test projects
```

## 完成后报告格式
```
STAGE-01-TASK-01 完成报告
- 状态: DONE
- 创建文件:
  anhei4-map.sln
  src/Anhei4Map.App/Anhei4Map.App.csproj
  src/Anhei4Map.App/MainWindow.xaml
  src/Anhei4Map.App/MainWindow.xaml.cs
  src/Anhei4Map.App/App.xaml
  src/Anhei4Map.App/App.xaml.cs
  src/Anhei4Map.App/AssemblyInfo.cs
  src/Anhei4Map.Core/Anhei4Map.Core.csproj
  src/Anhei4Map.Infrastructure/Anhei4Map.Infrastructure.csproj
  tests/Anhei4Map.Tests/Anhei4Map.Tests.csproj
  tests/Anhei4Map.Tests/Usings.cs
- 删除文件:
  src/Anhei4Map.Core/Class1.cs
  src/Anhei4Map.Infrastructure/Class1.cs
  tests/Anhei4Map.Tests/UnitTest1.cs
- dotnet restore: [通过/失败]
- dotnet build -c Release: [通过/失败]
- dotnet test -c Release --no-build: [通过/失败]
- Git commit: [hash]
```
