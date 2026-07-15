using System.Windows;
using System.Windows.Media.Imaging;

namespace Anhei4Map.App;

public partial class OverlayWindow : Window
{
    private const double MaxOverlayWidth = 600;
    private const double MaxOverlayHeight = 375;

    public OverlayWindow()
    {
        InitializeComponent();
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
