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
固定焦点区域二次裁剪（红框地图区域）

## 下一执行者
Cursor

---

## Allowed Paths (1 file)
- src/Anhei4Map.App/RendererWindow.xaml.cs

## Forbidden
App.xaml.cs, OverlayWindow, MapViewportDiagnostics, MapRegion 模型, DomainPolicy, 其他 src/**, tests/**, *.csproj, *.sln. 不新增文件/依赖/配置/自动检测/拼接。

## 实现

在 `CaptureAndCropMapCoreAsync` 中首次 CroppedBitmap 创建后、Freeze 前插入：

```csharp
// 首次裁剪（现有逻辑，不变）
var cropped = new CroppedBitmap(bitmap, new Int32Rect(left, top, cropWidth, cropHeight));

// 二次裁剪：固定焦点区域（红框地图）
const double FOCUS_LEFT   = 0.2540;
const double FOCUS_TOP    = 0.0000;
const double FOCUS_RIGHT  = 0.6917;
const double FOCUS_BOTTOM = 0.8528;

var focusLeft   = (int)Math.Round(cropWidth  * FOCUS_LEFT);
var focusTop    = (int)Math.Round(cropHeight * FOCUS_TOP);
var focusRight  = (int)Math.Round(cropWidth  * FOCUS_RIGHT);
var focusBottom = (int)Math.Round(cropHeight * FOCUS_BOTTOM);

focusLeft   = Math.Max(0, focusLeft);
focusTop    = Math.Max(0, focusTop);
focusRight  = Math.Min(cropWidth,  focusRight);
focusBottom = Math.Min(cropHeight, focusBottom);

var focusWidth  = focusRight - focusLeft;
var focusHeight = focusBottom - focusTop;

if (focusWidth > 0 && focusHeight > 0)
{
    cropped = new CroppedBitmap(cropped,
        new Int32Rect(focusLeft, focusTop, focusWidth, focusHeight));
}

cropped.Freeze();
```

预期 975×720 输入 → 约 248,0,674,614 → 426×614 输出。去除底部 106px 黑色区域，横向收窄至红框地图区域。

## 禁止
DOM 查询、自动检测、颜色分析、Canvas 识别、第二套截图、拼接、新滚动。

## 验证
dotnet build -c Release && dotnet test -c Release --no-build && git diff --check

## 提交
fix: apply fixed focus-region crop to isolate red-box map area
