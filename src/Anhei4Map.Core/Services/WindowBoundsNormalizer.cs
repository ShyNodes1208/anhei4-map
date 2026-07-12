using Anhei4Map.Core.Models;

namespace Anhei4Map.Core.Services;

public record WorkArea(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;

    public int Bottom => Top + Height;
}

public static class WindowBoundsNormalizer
{
    private const int DefaultWidth = 640;
    private const int DefaultHeight = 360;
    private const int MinWidth = 200;
    private const int MinHeight = 150;
    private const int EdgePadding = 16;
    private const double VisibilityThreshold = 0.2;

    public static WindowPlacement Normalize(WindowPlacement saved, WorkArea[] workAreas)
    {
        if (workAreas.Length == 0)
        {
            return new WindowPlacement(0, EdgePadding, DefaultWidth, DefaultHeight, saved.Opacity);
        }

        var primary = workAreas[0];
        var left = saved.Left;
        var top = saved.Top;
        var width = saved.Width;
        var height = saved.Height;

        if (width < MinWidth)
        {
            width = DefaultWidth;
        }

        if (height < MinHeight)
        {
            height = DefaultHeight;
        }

        if (width > primary.Width)
        {
            width = primary.Width;
        }

        if (height > primary.Height)
        {
            height = primary.Height;
        }

        if (IsSufficientlyVisible(left, top, width, height, workAreas))
        {
            return new WindowPlacement(left, top, width, height, saved.Opacity);
        }

        return new WindowPlacement(
            primary.Right - DefaultWidth - EdgePadding,
            primary.Top + EdgePadding,
            DefaultWidth,
            DefaultHeight,
            saved.Opacity);
    }

    private static bool IsSufficientlyVisible(
        int left,
        int top,
        int width,
        int height,
        WorkArea[] workAreas)
    {
        var windowArea = (long)width * height;
        if (windowArea <= 0)
        {
            return false;
        }

        var minimumVisibleArea = windowArea * VisibilityThreshold;

        foreach (var area in workAreas)
        {
            if (GetIntersectionArea(left, top, width, height, area) >= minimumVisibleArea)
            {
                return true;
            }
        }

        return false;
    }

    private static long GetIntersectionArea(
        int left,
        int top,
        int width,
        int height,
        WorkArea area)
    {
        var intersectLeft = Math.Max(left, area.Left);
        var intersectTop = Math.Max(top, area.Top);
        var intersectRight = Math.Min(left + width, area.Right);
        var intersectBottom = Math.Min(top + height, area.Bottom);

        if (intersectRight <= intersectLeft || intersectBottom <= intersectTop)
        {
            return 0;
        }

        return (long)(intersectRight - intersectLeft) * (intersectBottom - intersectTop);
    }
}
