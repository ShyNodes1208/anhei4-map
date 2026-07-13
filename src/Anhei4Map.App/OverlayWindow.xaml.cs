using System.Windows;
using System.Windows.Media.Imaging;

namespace Anhei4Map.App;

public partial class OverlayWindow : Window
{
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

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateMapImage(bitmap));
            return;
        }

        MapImage.Source = bitmap;
    }
}
