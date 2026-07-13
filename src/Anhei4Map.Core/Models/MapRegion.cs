namespace Anhei4Map.Core.Models;

public sealed record MapRegion(
    double Left,
    double Top,
    double Width,
    double Height,
    double DevicePixelRatio);
