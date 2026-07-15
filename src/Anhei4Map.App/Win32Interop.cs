using System.Runtime.InteropServices;
using Anhei4Map.Core.Interop;

namespace Anhei4Map.App;

public sealed class Win32Interop : IWin32Interop
{
    public IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
        {
            return Win32Native.SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }

        return new IntPtr(Win32Native.SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    public IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
        {
            return Win32Native.GetWindowLongPtr64(hWnd, nIndex);
        }

        return new IntPtr(Win32Native.GetWindowLong32(hWnd, nIndex));
    }

    public bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags)
    {
        return Win32Native.SetWindowPos(hWnd, hWndInsertAfter, x, y, width, height, flags);
    }

    public ScreenInfo[] GetMonitorWorkingAreas()
    {
        var areas = new List<ScreenInfo>();

        bool Callback(IntPtr hMonitor, IntPtr hdcMonitor, ref Win32Native.RECT lprcMonitor, IntPtr dwData)
        {
            var monitorInfo = new Win32Native.MONITORINFOEX
            {
                cbSize = Marshal.SizeOf<Win32Native.MONITORINFOEX>()
            };

            if (Win32Native.GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                areas.Add(new ScreenInfo(
                    monitorInfo.rcWork.Left,
                    monitorInfo.rcWork.Top,
                    monitorInfo.rcWork.Right - monitorInfo.rcWork.Left,
                    monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top));
            }

            return true;
        }

        var enumProc = new Win32Native.MonitorEnumProc(Callback);
        if (!Win32Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, enumProc, IntPtr.Zero))
        {
            return [];
        }

        return areas.ToArray();
    }

    public DpiInfo GetDpiForWindow(IntPtr hWnd)
    {
        var dpi = Win32Native.GetDpiForWindow(hWnd);
        if (dpi == 0)
        {
            return new DpiInfo(0.0f, 0.0f);
        }

        var scale = dpi / 96.0f;
        return new DpiInfo(scale, scale);
    }
}
