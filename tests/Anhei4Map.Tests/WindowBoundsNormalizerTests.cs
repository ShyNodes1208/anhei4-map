using Anhei4Map.Core.Models;
using Anhei4Map.Core.Services;

namespace Anhei4Map.Tests;

public class WindowBoundsNormalizerTests
{
    private static WorkArea PrimaryMonitor() => new(0, 0, 1920, 1080);

    private static WorkArea LeftMonitor() => new(-1920, 0, 1920, 1080);

    [Fact]
    public void ValidBounds_Unchanged()
    {
        var saved = new WindowPlacement(100, 100, 640, 360, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(100, result.Left);
        Assert.Equal(100, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
        Assert.Equal(0.9, result.Opacity);
    }

    [Fact]
    public void FullyOffscreenRight_Resets()
    {
        var saved = new WindowPlacement(2500, 100, 640, 360, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(1264, result.Left);
        Assert.Equal(16, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void FullyOffscreenLeft_Resets()
    {
        var saved = new WindowPlacement(-3000, 100, 640, 360, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(1264, result.Left);
        Assert.Equal(16, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void FullyOffscreenAbove_Resets()
    {
        var saved = new WindowPlacement(100, -500, 640, 360, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(1264, result.Left);
        Assert.Equal(16, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void FullyOffscreenBelow_Resets()
    {
        var saved = new WindowPlacement(100, 2000, 640, 360, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(1264, result.Left);
        Assert.Equal(16, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void PartiallyVisibleAboveThreshold_Unchanged()
    {
        var saved = new WindowPlacement(1600, 100, 640, 360, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(1600, result.Left);
        Assert.Equal(100, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void BelowVisibilityThreshold_Resets()
    {
        var saved = new WindowPlacement(1840, 100, 640, 360, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(1264, result.Left);
        Assert.Equal(16, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void ValidOnNegativeOriginMonitor_Unchanged()
    {
        var saved = new WindowPlacement(-100, 100, 640, 360, 0.9);
        var workAreas = new[] { PrimaryMonitor(), LeftMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(-100, result.Left);
        Assert.Equal(100, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void WidthBelowMin_ClampedToDefault()
    {
        var saved = new WindowPlacement(100, 100, 50, 360, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void HeightBelowMin_ClampedToDefault()
    {
        var saved = new WindowPlacement(100, 100, 640, 100, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void WidthExceedsWorkArea_Clamped()
    {
        var saved = new WindowPlacement(0, 100, 5000, 360, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(0, result.Left);
        Assert.Equal(100, result.Top);
        Assert.Equal(1920, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void HeightExceedsWorkArea_Clamped()
    {
        var saved = new WindowPlacement(100, 0, 640, 2000, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(100, result.Left);
        Assert.Equal(0, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(1080, result.Height);
    }

    [Fact]
    public void EmptyWorkAreaList_Fallback()
    {
        var saved = new WindowPlacement(500, 500, 640, 360, 0.9);
        var workAreas = Array.Empty<WorkArea>();

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(0, result.Left);
        Assert.Equal(16, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
        Assert.Equal(0.9, result.Opacity);
    }

    [Fact]
    public void ResetPositionsIncludeEdgePadding()
    {
        var saved = new WindowPlacement(2500, 100, 640, 360, 0.9);
        var workAreas = new[] { PrimaryMonitor() };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(PrimaryMonitor().Left + PrimaryMonitor().Width - 640 - 16, result.Left);
        Assert.Equal(PrimaryMonitor().Top + 16, result.Top);
    }

    [Fact]
    public void LargerSecondaryScreen_DoesNotClampToSmallerPrimary()
    {
        var workAreas = new[]
        {
            new WorkArea(0, 0, 1920, 1080),
            new WorkArea(1920, 0, 2560, 1440)
        };
        var saved = new WindowPlacement(2000, 100, 2500, 800, 0.9);

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(2000, result.Left);
        Assert.Equal(100, result.Top);
        Assert.Equal(2500, result.Width);
        Assert.Equal(800, result.Height);
    }

    [Fact]
    public void OversizedWindowOnSecondary_ClampsToSecondaryWorkArea()
    {
        var workAreas = new[]
        {
            new WorkArea(0, 0, 1920, 1080),
            new WorkArea(1920, 0, 2560, 1440)
        };
        var saved = new WindowPlacement(2000, 100, 3000, 1500, 0.9);

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(2000, result.Left);
        Assert.Equal(100, result.Top);
        Assert.Equal(2560, result.Width);
        Assert.Equal(1440, result.Height);
    }

    [Fact]
    public void NegativeCoordinateSecondary_IsSelectedByIntersection()
    {
        var workAreas = new[]
        {
            new WorkArea(0, 0, 1920, 1080),
            new WorkArea(-2560, 0, 2560, 1440)
        };
        var saved = new WindowPlacement(-2000, 100, 2500, 800, 0.9);

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(-2000, result.Left);
        Assert.Equal(100, result.Top);
        Assert.Equal(2500, result.Width);
        Assert.Equal(800, result.Height);
    }

    [Fact]
    public void WindowSpanningTwoScreens_SelectsLargestIntersection()
    {
        var workAreas = new[]
        {
            new WorkArea(0, 0, 1920, 1080),
            new WorkArea(1920, 0, 2560, 1440)
        };
        var saved = new WindowPlacement(1500, 100, 3500, 900, 0.9);

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(1500, result.Left);
        Assert.Equal(100, result.Top);
        Assert.Equal(2560, result.Width);
        Assert.Equal(900, result.Height);
    }

    [Fact]
    public void EqualIntersection_UsesStableTieBreak()
    {
        var workAreas = new[]
        {
            new WorkArea(0, 0, 1000, 1000),
            new WorkArea(1000, 0, 2000, 1000)
        };
        var saved = new WindowPlacement(250, 0, 1500, 800, 0.9);

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(250, result.Left);
        Assert.Equal(0, result.Top);
        Assert.Equal(1000, result.Width);
        Assert.Equal(800, result.Height);
    }

    [Fact]
    public void FullyOffscreen_UsesPrimaryFallback()
    {
        var workAreas = new[]
        {
            new WorkArea(0, 0, 1920, 1080),
            new WorkArea(1920, 0, 2560, 1440)
        };
        var saved = new WindowPlacement(5000, 100, 640, 360, 0.9);

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(1920 - 640 - 16, result.Left);
        Assert.Equal(16, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void EmptyWorkAreas_UsesExistingSafeFallback()
    {
        var saved = new WindowPlacement(500, 500, 640, 360, 0.9);
        var workAreas = Array.Empty<WorkArea>();

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(0, result.Left);
        Assert.Equal(16, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
        Assert.Equal(0.9, result.Opacity);
    }

    [Fact]
    public void ExistingValidPrimaryWindow_RemainsUnchanged()
    {
        var saved = new WindowPlacement(100, 100, 640, 360, 0.9);
        var workAreas = new[]
        {
            new WorkArea(0, 0, 1920, 1080),
            new WorkArea(1920, 0, 2560, 1440)
        };

        var result = WindowBoundsNormalizer.Normalize(saved, workAreas);

        Assert.Equal(100, result.Left);
        Assert.Equal(100, result.Top);
        Assert.Equal(640, result.Width);
        Assert.Equal(360, result.Height);
        Assert.Equal(0.9, result.Opacity);
    }

    [Fact]
    public void ExistingPartialVisibilityThresholdTests_RemainPassing()
    {
        var workAreas = new[] { PrimaryMonitor() };

        var aboveThreshold = new WindowPlacement(1600, 100, 640, 360, 0.9);
        var aboveResult = WindowBoundsNormalizer.Normalize(aboveThreshold, workAreas);
        Assert.Equal(1600, aboveResult.Left);
        Assert.Equal(100, aboveResult.Top);
        Assert.Equal(640, aboveResult.Width);
        Assert.Equal(360, aboveResult.Height);

        var belowThreshold = new WindowPlacement(1840, 100, 640, 360, 0.9);
        var belowResult = WindowBoundsNormalizer.Normalize(belowThreshold, workAreas);
        Assert.Equal(PrimaryMonitor().Width - 640 - 16, belowResult.Left);
        Assert.Equal(16, belowResult.Top);
        Assert.Equal(640, belowResult.Width);
        Assert.Equal(360, belowResult.Height);
    }

    [Fact]
    public void WidthAndHeightAreClampedIndependently()
    {
        var workAreas = new[]
        {
            new WorkArea(0, 0, 1920, 1080),
            new WorkArea(1920, 0, 2560, 1440)
        };

        var widthOnly = new WindowPlacement(2000, 100, 3000, 800, 0.9);
        var widthOnlyResult = WindowBoundsNormalizer.Normalize(widthOnly, workAreas);
        Assert.Equal(2560, widthOnlyResult.Width);
        Assert.Equal(800, widthOnlyResult.Height);

        var heightOnly = new WindowPlacement(2000, 100, 2500, 1500, 0.9);
        var heightOnlyResult = WindowBoundsNormalizer.Normalize(heightOnly, workAreas);
        Assert.Equal(2500, heightOnlyResult.Width);
        Assert.Equal(1440, heightOnlyResult.Height);
    }
}
