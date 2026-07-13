using System.Windows;
using System.Windows.Interop;

namespace Anhei4Map.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var win32 = new Win32Interop();

        var exStyle = win32.GetWindowLongPtr(hwnd, Win32Native.GWL_EXSTYLE);
        var newExStyle = new IntPtr(
            exStyle.ToInt64() | unchecked((long)Win32Native.WS_EX_TOOLWINDOW));
        win32.SetWindowLongPtr(hwnd, Win32Native.GWL_EXSTYLE, newExStyle);

        win32.SetWindowPos(
            hwnd,
            Win32Native.HWND_TOPMOST,
            0, 0, 0, 0,
            Win32Native.SWP_NOACTIVATE |
            Win32Native.SWP_NOMOVE |
            Win32Native.SWP_NOSIZE |
            Win32Native.SWP_SHOWWINDOW);
    }
}
