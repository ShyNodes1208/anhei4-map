# 当前任务

## 任务编号
Task 1

## 所属阶段
Stage-1: Core Library + Solution Scaffold

## 前置条件
无（绿色场项目）

## 任务目标
创建 .NET 8 解决方案和三个项目（Anhei4Map.Core、Anhei4Map.Wpf、Anhei4Map.Core.Tests），建立项目引用关系，添加 WebView2 NuGet 包，确认 `dotnet build` 和 `dotnet test` 可运行。

## 行为要求
1. 创建 `anhei4-map.sln` 解决方案文件
2. 创建 `src/Anhei4Map.Core/` .NET 8 类库项目
3. 创建 `src/Anhei4Map.Wpf/` .NET 8 WPF 应用程序项目
4. 创建 `tests/Anhei4Map.Core.Tests/` xUnit 测试项目
5. 将所有三个项目添加到解决方案
6. Tests 项目添加对 Core 项目的引用
7. WPF 项目添加 `Microsoft.Web.WebView2` NuGet 包
8. 删除 Core 项目模板生成的 `Class1.cs`
9. 验证 `dotnet build` 零错误
10. 验证 `dotnet test` 可运行（0 测试，无失败）

## 允许修改
- Create: `anhei4-map.sln`
- Create: `src/Anhei4Map.Core/Anhei4Map.Core.csproj`
- Create: `src/Anhei4Map.Core/Class1.cs`（临时生成，立即删除）
- Create: `src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj`
- Create: `tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj`
- Create: `tests/Anhei4Map.Core.Tests/Usings.cs`
- Modify: `src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj`（dotnet add package 自动修改）

## 禁止修改
- 不得修改 `docs/` 下任何文件
- 不得修改 `CLAUDE.md`
- 不得修改 `AGENTS.md`
- 不得修改 `.gitignore`

## 必须新增或修改的测试
无——此任务为纯脚手架，不涉及业务逻辑测试。

## RED 预期
无——此任务为纯脚手架，无 TDD RED 步骤。

## GREEN 最小实现

```bash
# Step 1: Create solution
dotnet new sln -n anhei4-map

# Step 2: Create Core classlib
dotnet new classlib -n Anhei4Map.Core -o src/Anhei4Map.Core -f net8.0

# Step 3: Create WPF application
dotnet new wpf -n Anhei4Map.Wpf -o src/Anhei4Map.Wpf -f net8.0

# Step 4: Create xUnit test project
dotnet new xunit -n Anhei4Map.Core.Tests -o tests/Anhei4Map.Core.Tests -f net8.0

# Step 5: Add projects to solution
dotnet sln add src/Anhei4Map.Core/Anhei4Map.Core.csproj
dotnet sln add src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj
dotnet sln add tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj

# Step 6: Add project references (Tests → Core)
dotnet add tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj reference src/Anhei4Map.Core/Anhei4Map.Core.csproj

# Step 7: Add WebView2 NuGet to WPF project
dotnet add src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj package Microsoft.Web.WebView2

# Step 8: Remove template Class1.cs
rm src/Anhei4Map.Core/Class1.cs
```

## 验证命令
```bash
dotnet build
dotnet test tests/Anhei4Map.Core.Tests/
```
期望输出：
- `dotnet build`: "Build succeeded." 0 Error(s)
- `dotnet test`: 无测试运行或 "Test Run Successful."

## 人工验证
- [ ] `anhei4-map.sln` 存在
- [ ] `src/Anhei4Map.Core/Anhei4Map.Core.csproj` 存在，目标框架 `net8.0`
- [ ] `src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj` 存在，含 `Microsoft.Web.WebView2` 包引用
- [ ] `tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj` 存在，含对 Core 的项目引用
- [ ] `src/Anhei4Map.Core/Class1.cs` 已删除
- [ ] `dotnet build` 零错误
- [ ] `dotnet test` 可正常运行（测试数为 0 或通过）

## 完成标准
1. `dotnet build` 零错误
2. 解决方案含三个项目，引用关系正确
3. WPF 项目含 `Microsoft.Web.WebView2` NuGet 依赖
4. Core 项目无残留模板文件
5. `dotnet test` 可正常运行

## Git 提交信息
```
chore: scaffold solution with Core, WPF, and xUnit test projects
```

## 完成后报告格式
```
Task 1 完成报告
- 状态: DONE
- 创建文件: [文件列表]
- dotnet build: [通过/失败]
- dotnet test: [通过/失败]
- Git commit: [hash]
```
