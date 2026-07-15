# 当前任务

## 阶段
STAGE-04

## 任务编号
STAGE-04-TASK-04-FOCUS-CROP

## 类型
FIX

## 状态
READY

## 组件
固定焦点区域二次裁剪

## Branch
feature/04-map-viewport-redesign

## Base
3a8d20bf46d05cba50f9b8120ac1424644c095a7 (Stage 04 complete, scroll fix working)

---

## 问题

滚动后生产裁剪结果为 975×720（#map 完整容器），包含底部黑色空白区域和左右多余边缘。用户需要的是网站红框内的中间有效地图区域。

## 修复策略

在现有滚动→重新查询→CapturePreview→裁剪流程上，对 975×720 的第一次裁剪结果增加一次固定焦点区域二次裁剪。

## Allowed Paths (1 file)
- src/Anhei4Map.App/RendererWindow.xaml.cs

## Forbidden
- App.xaml.cs, OverlayWindow, MapViewportDiagnostics, MapRegion 模型, DomainPolicy
- 其他 src/**, tests/**, docs/**, *.csproj, *.sln
- 不新增文件, 不新增依赖, 不新增图片拼接, 不新增自动检测系统

## 实现

在 `CaptureAndCropMapCoreAsync` 中首次 CroppedBitmap 创建后，增加一次固定焦点区域二次裁剪：

```csharp
// 首次裁剪（现有逻辑，不变）
var cropped = new CroppedBitmap(bitmap, new Int32Rect(left, top, cropWidth, cropHeight));

// 新增：固定焦点区域二次裁剪
var focusLeft   = (int)(cropWidth  * FOCUS_LEFT_RATIO);   // 左边界比例，如 0.10
var focusTop    = (int)(cropHeight * FOCUS_TOP_RATIO);    // 上边界比例，如 0.00
var focusRight  = (int)(cropWidth  * FOCUS_RIGHT_RATIO);  // 右边界比例，如 0.90
var focusBottom = (int)(cropHeight * FOCUS_BOTTOM_RATIO); // 下边界比例，如 0.75

var focusWidth  = focusRight - focusLeft;
var focusHeight = focusBottom - focusTop;

if (focusWidth > 0 && focusHeight > 0)
{
    cropped = new CroppedBitmap(cropped, new Int32Rect(focusLeft, focusTop, focusWidth, focusHeight));
}
cropped.Freeze();
```

## 比例参数（需要用户根据红框实际位置调整）

当前默认：
- FOCUS_LEFT_RATIO = 0.10 （去掉左侧约 10%）
- FOCUS_TOP_RATIO = 0.00 （从顶部开始）
- FOCUS_RIGHT_RATIO = 0.90 （取到右侧 90%）
- FOCUS_BOTTOM_RATIO = 0.75 （去掉底部约 25% 黑色区域）

**请提供实际红框坐标或调整上述比例。**

## 禁止
- DOM 查询红框位置（网站可能变化，当前只是固定裁剪）
- 自动边缘检测
- 颜色分析
- Canvas 内容识别
- 第二套截图、拼接、滚动

## 验证
dotnet build -c Release && dotnet test -c Release --no-build && git diff --check

## 提交
fix: apply fixed focus-region crop to remove black area and trim sides
