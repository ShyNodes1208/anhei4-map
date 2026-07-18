using System.ComponentModel;
using System.Windows;

namespace Anhei4Map.App;

public partial class ControlWindow : Window
{
    private bool _isShuttingDown;

    public event EventHandler? RefreshRequested;

    public ControlWindow()
    {
        InitializeComponent();
    }

    public void SetRefreshEnabled(bool enabled)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetRefreshEnabled(enabled));
            return;
        }

        RefreshButton.IsEnabled = enabled;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        ShutdownApp();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isShuttingDown && !Application.Current.Dispatcher.HasShutdownStarted)
        {
            // 用户点击右上角 X → 触发完整退出
            ShutdownApp();
        }
        // Shutdown 期间允许窗口正常关闭

        base.OnClosing(e);
    }

    private void ShutdownApp()
    {
        if (_isShuttingDown || Application.Current.Dispatcher.HasShutdownStarted)
        {
            return;
        }

        _isShuttingDown = true;
        Application.Current.Shutdown();
    }
}
