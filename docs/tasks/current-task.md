# 当前任务

## 任务编号
Task 1-FIX1（原 Task 1 返工——上游未执行）

## 所属阶段
Stage-1: Core Library + Solution Scaffold

## 前置条件
无（绿色场项目）。当前分支 `design-baseline-v1` 无 src/ 和 tests/。

## 任务目标
与 Task 1 完全相同：创建 .NET 8 解决方案和三个项目，建立引用关系，添加 WebView2 NuGet。

## 行为要求
1. `dotnet new sln -n anhei4-map`
2. `dotnet new classlib -n Anhei4Map.Core -o src/Anhei4Map.Core -f net8.0`
3. `dotnet new wpf -n Anhei4Map.Wpf -o src/Anhei4Map.Wpf -f net8.0`
4. `dotnet new xunit -n Anhei4Map.Core.Tests -o tests/Anhei4Map.Core.Tests -f net8.0`
5. `dotnet sln add` 三个项目到解决方案
6. `dotnet add tests reference src/Anhei4Map.Core`
7. `dotnet add src/Anhei4Map.Wpf package Microsoft.Web.WebView2`
8. 删除模板生成的 `src/Anhei4Map.Core/Class1.cs`
9. `dotnet build` 零错误
10. `dotnet test` 可运行
11. `git add` 所有新文件
12. `git commit -m "chore: scaffold solution with Core, WPF, and xUnit test projects"`
13. `git push origin design-baseline-v1`

## 允许修改
- Create: `anhei4-map.sln`
- Create: `src/Anhei4Map.Core/Anhei4Map.Core.csproj`
- Create: `src/Anhei4Map.Core/Class1.cs`（生成后立即删除）
- Create: `src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj`
- Create: `tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj`
- Create: `tests/Anhei4Map.Core.Tests/Usings.cs`

## 禁止修改
- 不得修改 `docs/` 下任何文件
- 不得修改 `CLAUDE.md`、`AGENTS.md`、`.gitignore`

## 必须新增或修改的测试
无（纯脚手架任务）

## RED 预期
无

## GREEN 最小实现
```bash
dotnet new sln -n anhei4-map
dotnet new classlib -n Anhei4Map.Core -o src/Anhei4Map.Core -f net8.0
dotnet new wpf -n Anhei4Map.Wpf -o src/Anhei4Map.Wpf -f net8.0
dotnet new xunit -n Anhei4Map.Core.Tests -o tests/Anhei4Map.Core.Tests -f net8.0
dotnet sln add src/Anhei4Map.Core/Anhei4Map.Core.csproj
dotnet sln add src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj
dotnet sln add tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj
dotnet add tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj reference src/Anhei4Map.Core/Anhei4Map.Core.csproj
dotnet add src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj package Microsoft.Web.WebView2
rm src/Anhei4Map.Core/Class1.cs
dotnet build
dotnet test tests/Anhei4Map.Core.Tests/
```

## 验证命令
```bash
dotnet build && echo "BUILD OK" || echo "BUILD FAIL"
dotnet test tests/Anhei4Map.Core.Tests/ && echo "TEST OK" || echo "TEST FAIL"
```

## 人工验证
- [ ] `anhei4-map.sln` 存在
- [ ] `src/Anhei4Map.Core/Anhei4Map.Core.csproj` 存在
- [ ] `src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj` 存在，含 WebView2 包引用
- [ ] `tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj` 存在，引用 Core
- [ ] `Class1.cs` 已删除

## 完成标准
`dotnet build` 零错误 且 `dotnet test` 可运行 且 git push 成功。

## Git 提交信息
```
chore: scaffold solution with Core, WPF, and xUnit test projects
```

## 完成后报告格式
```
Task 1-FIX1 完成报告
- 状态: DONE
- 创建文件: anhei4-map.sln, src/Anhei4Map.Core/Anhei4Map.Core.csproj, src/Anhei4Map.Wpf/Anhei4Map.Wpf.csproj, tests/Anhei4Map.Core.Tests/Anhei4Map.Core.Tests.csproj, tests/Anhei4Map.Core.Tests/Usings.cs
- 删除文件: src/Anhei4Map.Core/Class1.cs
- dotnet build: [通过/失败]
- dotnet test: [通过/失败]
- Git commit: [hash]
```
