# 当前任务

## 阶段
STAGE-03-CROPPED-MAP-OVERLAY

## 任务编号
STAGE-03-ACCEPTANCE-ROLLBACK-01

## 类型
ROLLBACK

## 状态
READY

## 组件
回滚 RendererWindow 到 FIX-03 可运行状态

## 原因
FIX-04 (d4bdeba) 和 FIX-05 (49c8d08) 的视口准备逻辑导致 OverlayWindow 不再显示

## 下一执行者
Cursor

---

## 允许修改的精确路径

- `src/Anhei4Map.App/RendererWindow.xaml`
- `src/Anhei4Map.App/RendererWindow.xaml.cs`

## 禁止修改范围

- `App.xaml.cs`
- `OverlayWindow.xaml` / `OverlayWindow.xaml.cs`
- `src/Anhei4Map.Core/**`、`src/Anhei4Map.Infrastructure/**`
- `tests/**`、`docs/design/**`、`*.csproj`

---

## 回滚目标

将两个文件恢复到提交 `90071dfd8647d4599333c0c54bdcb7e0062f8d5d` 中的版本。

核对方式：
```powershell
git diff 90071dfd8647d4599333c0c54bdcb7e0062f8d5d -- src/Anhei4Map.App/RendererWindow.xaml src/Anhei4Map.App/RendererWindow.xaml.cs
```

回滚完成后上述 diff 无输出。

---

## 移除内容

FIX-04 和 FIX-05 引入的所有变更必须移除：
- RendererWindow Height=800
- PrepareMapViewportAsync 方法
- 地图容器强制 `position=fixed; width=100vw; height=100vh`
- html/body 样式修改（margin/padding/overflow）
- 双 requestAnimationFrame
- window resize 事件
- 90% 视口验证
- `_navigationGeneration` 字段
- 导航代次检查
- 3 次视口准备重试
- 视口准备失败回退逻辑

---

## 保留内容

90071df 中已存在的功能必须保留：
- 地图视觉就绪检查 (IsMapVisualReadyAsync)
- 3000ms 预热 + 10 次截图重试
- CapturePreviewAsync + PNG
- DOM 地图区域裁剪 + scaleX/scaleY
- 完整地图返回（无正方形二次裁剪）
- BitmapDecoder OnLoad + Freeze
- Overlay 动态比例 (max 400×250)
- 所有现有事件和导航白名单

---

## 验证命令

```powershell
git diff 90071dfd8647d4599333c0c54bdcb7e0062f8d5d -- src/Anhei4Map.App/RendererWindow.xaml src/Anhei4Map.App/RendererWindow.xaml.cs
dotnet build -c Release
dotnet test -c Release --no-build
```

## 验证标准

- git diff against 90071df 无输出（两个文件完美恢复）
- `dotnet build -c Release` 0 errors, 0 warnings
- `dotnet test -c Release --no-build` 160/160 PASS

---

## Git 提交信息

```
revert: restore RendererWindow to last working overlay state (90071df)

Roll back viewport preparation changes from FIX-04 and FIX-05 which
caused regression: OverlayWindow stopped appearing. Restore to the
last manually verified working state with full DOM map capture and
proportional 400x250 overlay display.
```

## Cursor 最终报告格式

```
ROLLBACK_COMPLETE

Task: STAGE-03-ACCEPTANCE-ROLLBACK-01
Type: ROLLBACK | Status: DONE
Commit: <hash>
Restored to: 90071dfd8647d4599333c0c54bdcb7e0062f8d5d
Files Restored:
  - src/Anhei4Map.App/RendererWindow.xaml
  - src/Anhei4Map.App/RendererWindow.xaml.cs
Diff against 90071df: CLEAN
Build: Release 0 errors 0 warnings | Tests: 160/160 PASS
```
