using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace Anhei4Map.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex;
    private RendererWindow? _rendererWindow;
    private OverlayWindow? _overlayWindow;
    private int _initialCaptureStarted;

    private DispatcherTimer? _hourlyRefreshTimer;
    private bool _refreshInProgress;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            _mutex = new Mutex(true, @"Global\Anhei4Map_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                _mutex.Dispose();
                _mutex = null;
                Shutdown();
                return;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or WaitHandleCannotBeOpenedException or IOException)
        {
            MessageBox.Show(
                $"无法创建应用程序互斥锁：{ex.Message}",
                "启动失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        string? version;
        try
        {
            version = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            ShowRuntimeMissingDialog();
            Shutdown();
            return;
        }
        catch (Exception ex) when (ex is not WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(
                $"WebView2 运行时检测失败：{ex.Message}",
                "启动失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            ShowRuntimeMissingDialog();
            Shutdown();
            return;
        }

        var diagnoseMapViewport = e.Args.Any(static arg =>
            string.Equals(arg, "--diagnose-map-viewport", StringComparison.OrdinalIgnoreCase));

        _rendererWindow = new RendererWindow(diagnoseMapViewport);
        _overlayWindow = new OverlayWindow();
        _rendererWindow.NavigationReady += OnRendererNavigationReady;
        _rendererWindow.Show();

        base.OnStartup(e);
    }

    private void OnRendererNavigationReady(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _initialCaptureStarted, 1) != 0)
        {
            return;
        }

        _ = StartInitialCaptureAsync();
    }

    private async Task StartInitialCaptureAsync()
    {
        const int maxAttempts = 10;
        const int warmupDelayMs = 3000;
        const int retryDelayMs = 1000;

        await Task.Delay(warmupDelayMs);

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var bitmap = await _rendererWindow!.CaptureAndCropMapAsync();
            if (bitmap != null)
            {
                _overlayWindow!.UpdateMapImage(bitmap);
                if (!_overlayWindow.IsVisible)
                {
                    _overlayWindow.Show();
                }

                ScheduleNextHourlyRefresh();
                return;
            }

            if (attempt < maxAttempts - 1)
            {
                await Task.Delay(retryDelayMs);
            }
        }
    }

    private void EnsureHourlyRefreshTimer()
    {
        if (_hourlyRefreshTimer != null)
        {
            return;
        }

        _hourlyRefreshTimer = new DispatcherTimer();
        _hourlyRefreshTimer.Tick += OnHourlyRefreshTick;
    }

    private void ScheduleNextHourlyRefresh()
    {
        EnsureHourlyRefreshTimer();

        _hourlyRefreshTimer!.Stop();

        var now = DateTime.Now;
        var next = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            1,
            0);

        if (next <= now)
        {
            next = next.AddHours(1);
        }

        var delay = next - now;
        var intervalMs = Math.Max(delay.TotalMilliseconds, 1);
        _hourlyRefreshTimer.Interval = TimeSpan.FromMilliseconds(intervalMs);
        _hourlyRefreshTimer.Start();
    }

    private async void OnHourlyRefreshTick(object? sender, EventArgs e)
    {
        _hourlyRefreshTimer?.Stop();

        if (_refreshInProgress)
        {
            ScheduleNextHourlyRefresh();
            return;
        }

        _refreshInProgress = true;
        try
        {
            if (_rendererWindow == null || _overlayWindow == null)
            {
                return;
            }

            var reloadOk = await _rendererWindow.ReloadPageAsync();
            if (!reloadOk)
            {
                return;
            }

            const int maxAttempts = 10;
            const int warmupDelayMs = 10000;
            const int retryDelayMs = 1000;

            await Task.Delay(warmupDelayMs);

            System.Windows.Media.Imaging.BitmapSource? bitmap = null;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                bitmap = await _rendererWindow.CaptureAndCropMapAsync();

                if (bitmap is not null)
                {
                    break;
                }

                if (attempt < maxAttempts - 1)
                {
                    await Task.Delay(retryDelayMs);
                }
            }

            if (bitmap is not null)
            {
                _overlayWindow.UpdateMapImage(bitmap);
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            _refreshInProgress = false;
            ScheduleNextHourlyRefresh();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_hourlyRefreshTimer != null)
        {
            _hourlyRefreshTimer.Stop();
            _hourlyRefreshTimer.Tick -= OnHourlyRefreshTick;
            _hourlyRefreshTimer = null;
        }

        try
        {
            _mutex?.ReleaseMutex();
        }
        catch
        {
            // 忽略——进程退出前释放尽力而为
        }

        _mutex?.Dispose();
        _mutex = null;

        base.OnExit(e);
    }

    private static void ShowRuntimeMissingDialog()
    {
        MessageBox.Show(
            "Microsoft Edge WebView2 Runtime 未安装。\n\n" +
            "请从以下链接下载 Evergreen Bootstrapper 后重试：\n\n" +
            "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
            "缺少必需组件 — Anhei4Map",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
