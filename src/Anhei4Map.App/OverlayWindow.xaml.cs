using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Anhei4Map.App;

public partial class OverlayWindow : Window
{
    private const double MaxOverlayWidth = 600;
    private const double MaxOverlayHeight = 375;

    public OverlayWindow()
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
            exStyle.ToInt64() |
            unchecked((long)Win32Native.WS_EX_TRANSPARENT) |
            unchecked((long)Win32Native.WS_EX_NOACTIVATE));
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

    public void UpdateMapImage(BitmapSource? bitmap)
    {
        if (bitmap == null)
        {
            return;
        }

        if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateMapImage(bitmap));
            return;
        }

        var scale = Math.Min(
            MaxOverlayWidth / bitmap.PixelWidth,
            MaxOverlayHeight / bitmap.PixelHeight);

        if (!double.IsFinite(scale) || scale <= 0)
        {
            return;
        }

        var displayWidth = bitmap.PixelWidth * scale;
        var displayHeight = bitmap.PixelHeight * scale;

        if (!double.IsFinite(displayWidth) ||
            displayWidth <= 0 ||
            !double.IsFinite(displayHeight) ||
            displayHeight <= 0 ||
            displayWidth > MaxOverlayWidth ||
            displayHeight > MaxOverlayHeight)
        {
            return;
        }

        Width = displayWidth;
        Height = displayHeight;
        MapImage.Source = bitmap;
    }
}
