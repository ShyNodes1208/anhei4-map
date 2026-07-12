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
}
