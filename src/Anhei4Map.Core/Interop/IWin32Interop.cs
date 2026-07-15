namespace Anhei4Map.Core.Interop;

public interface IWin32Interop
{
    IntPtr SetWindowLongPtr(
        IntPtr hWnd,
        int nIndex,
        IntPtr dwNewLong);

    IntPtr GetWindowLongPtr(
        IntPtr hWnd,
        int nIndex);

    bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    ScreenInfo[] GetMonitorWorkingAreas();

    DpiInfo GetDpiForWindow(IntPtr hWnd);
}
