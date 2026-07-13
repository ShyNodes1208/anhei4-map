namespace Anhei4Map.Core.Models;

public enum HotkeyCommand
{
    Unknown,
    ToggleLock,
    ToggleHide,
    ZoomIn,
    ZoomOut,
    OpacityUp,
    OpacityDown,
    ResetPosition,
    Refresh
}

public enum WindowState
{
    Hidden,
    Locked,
    Edit
}

public record HotkeyBinding(int Id, uint Modifiers, uint Key, HotkeyCommand Command);

public record WindowPlacement(int Left, int Top, int Width, int Height, double Opacity);

public record AppSettings(
    List<HotkeyBinding> Hotkeys,
    WindowPlacement Placement,
    double ZoomLevel,
    WindowState InitialState)
{
    private const uint ModCtrlShift = 0x0002 | 0x0004;

    public static AppSettings CreateDefaults() => new(
        Hotkeys:
        [
            new HotkeyBinding(1, ModCtrlShift, 0x4D, HotkeyCommand.ToggleLock),
            new HotkeyBinding(2, ModCtrlShift, 0x48, HotkeyCommand.ToggleHide),
            new HotkeyBinding(3, ModCtrlShift, 0xBB, HotkeyCommand.ZoomIn),
            new HotkeyBinding(4, ModCtrlShift, 0xBD, HotkeyCommand.ZoomOut),
            new HotkeyBinding(5, ModCtrlShift, 0x26, HotkeyCommand.OpacityUp),
            new HotkeyBinding(6, ModCtrlShift, 0x28, HotkeyCommand.OpacityDown),
            new HotkeyBinding(7, ModCtrlShift, 0x52, HotkeyCommand.ResetPosition),
            new HotkeyBinding(8, ModCtrlShift, 0x74, HotkeyCommand.Refresh)
        ],
        Placement: new WindowPlacement(-1, -1, 640, 360, 0.9),
        ZoomLevel: 1.0,
        InitialState: WindowState.Locked);

    public bool Validate()
    {
        if (Placement.Width < 200)
        {
            return false;
        }

        if (Placement.Height < 150)
        {
            return false;
        }

        if (Placement.Opacity < 0.1 || Placement.Opacity > 1.0)
        {
            return false;
        }

        if (ZoomLevel < 0.25 || ZoomLevel > 5.0)
        {
            return false;
        }

        return true;
    }
}
