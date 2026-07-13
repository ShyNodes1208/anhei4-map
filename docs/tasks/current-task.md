# 当前任务

## 状态
READY

## 类型
GATE_FIX

## 任务编号
STAGE-01-REREVIEW-GATE-FIX-01

## 阶段
STAGE-01-FOUNDATION (合并门禁)

## 分支
feature/01-foundation

## Worktree
D:\AIProjects\anhei4-map-worktrees\stage-01-foundation

---

## 问题

`git diff --check origin/main...HEAD` 报告两处空白格式问题：

1. `src/Anhei4Map.App/App.xaml:7` — trailing whitespace
2. `src/Anhei4Map.App/App.xaml.cs:13` — new blank line at EOF

这些是 Stage 01 早期 scaffold 阶段引入的格式问题，非六项修复引入。

---

## 允许修改的精确路径

- `src/Anhei4Map.App/App.xaml`
- `src/Anhei4Map.App/App.xaml.cs`

## 禁止修改范围

- `src/Anhei4Map.Core/**`
- `src/Anhei4Map.Infrastructure/**`
- `tests/**`
- `docs/design/**`
- `docs/plans/**`
- `*.csproj`
- `*.sln`
- 不改变 XAML 结构
- 不改变应用启动行为
- 不改变命名空间
- 不改变任何业务逻辑
- 不处理 NU1900
- 不修改 NuGet 配置

---

## 精确修改

### App.xaml 第 7 行

```xml
    <Application.Resources>

    </Application.Resources>
```

第 7 行末尾有不可见空白字符（空格或 tab）。删除该行 `/>` 之后的所有空白字符，使该行变为空行（仅换行符）。

### App.xaml.cs 第 13-14 行

```csharp
}

```

删除第 14 行（文件末尾额外空行），使文件以 `}` 所在行结束，无末尾空行。

---

## 验证命令

```powershell
git diff --check
git diff --check origin/main...HEAD
```

## Release Build

```powershell
dotnet build -c Release
```

## 完整测试

```powershell
dotnet test -c Release --no-build
```

---

## 完成标准

- [ ] App.xaml 第 7 行 trailing whitespace 已删除
- [ ] App.xaml.cs 末尾多余空行已删除
- [ ] `git diff --check` 无输出
- [ ] `git diff --check origin/main...HEAD` 无输出
- [ ] `dotnet build -c Release` 0 错误
- [ ] `dotnet test -c Release --no-build` 122/122 PASS
- [ ] 只修改了允许的两个文件

---

## Git 提交信息

```
chore: clean Stage 01 merge gate whitespace
```

---

## Cursor 最终报告格式

```
GATE_FIX_COMPLETE

Task: STAGE-01-REREVIEW-GATE-FIX-01
Type: GATE_FIX
Status: DONE
Commit: <hash>
Build: Release 0 errors
Tests: 122/122 PASS
git diff --check: clean
git diff --check origin/main...HEAD: clean
Files Modified:
  - src/Anhei4Map.App/App.xaml
  - src/Anhei4Map.App/App.xaml.cs
```
