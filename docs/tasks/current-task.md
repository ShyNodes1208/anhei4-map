# 当前任务

## 阶段
STAGE-04

## 任务编号
STAGE-04-TASK-04-DISMISS-BOTTOM-COVER

## 类型
FIX

## 状态
READY

## 组件
关闭/隐藏底部固定覆盖层

## 下一执行者
Cursor

---

## Allowed Path (1 file)
- src/Anhei4Map.App/RendererWindow.xaml.cs

## Forbidden
OverlayWindow, App.xaml.cs, MapViewportDiagnostics, 其他 src/**, tests/**, *.csproj. New Files: NONE. New Dependencies: NONE.

---

## 实现

新增一个私有方法：

```csharp
private async Task<bool> TryDismissBottomCoverAsync()
```

### 覆盖层识别边界（冻结）

在现有 WebView DOM 中查找满足以下**全部条件**的元素：

1. `getComputedStyle(el).position === "fixed" || "sticky"`
2. `rect.bottom >= window.innerHeight - 120`（与视口底部相交 120px 以内）
3. `rect.width >= window.innerWidth * 0.6`（覆盖视口大部分宽度）
4. `rect.height >= 60 && rect.height <= 200`（底部横条范围）
5. 元素 tagName 不是 `#map`、不包含 `leaflet-` class、不在已知的 Leaflet 控件面板中

### 处理策略

对找到的第一个匹配元素：
1. 优先查找其内部的可点击关闭按钮（`button, [role=button], [aria-label*=close], .close, [class*=dismiss]`）
2. 找到关闭按钮 → 执行 `closeButton.click()`
3. 无关闭按钮 → 对该容器执行 `el.style.display = "none"`

### 约束

- 每个 capture cycle 最多执行一次
- 不使用固定鼠标屏幕坐标
- 不建立通用广告拦截系统
- 不扫描或隐藏所有 fixed 元素
- 不隐藏 #map、Leaflet 控件、Zone Select、Map Filter、OverlayWindow
- 失败 → 返回 false，不崩溃，不循环，不影响现有流程

### 执行顺序

```
MAP_REGION 初查 → NAV CHECK
→ SCROLL_INTO_VIEW → NAV CHECK
→ TryDismissBottomCoverAsync (一次)
→ NAV CHECK
→ IsMapVisualReadyAsync (现有有界等待)
→ MAP_REGION 重查 → NAV CHECK
→ CapturePreview → NAV CHECK
→ CroppedBitmap (完整 975×720) → Freeze
→ Overlay (600×375, 等比例 ~508×375)
```

## 验证
```powershell
dotnet build -c Release && dotnet test -c Release --no-build && git diff --check
```

## 提交
```
fix: dismiss bottom fixed cover before map capture
```
