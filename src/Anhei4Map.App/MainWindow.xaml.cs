using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Anhei4Map.Core.Services;
using Microsoft.Web.WebView2.Core;

namespace Anhei4Map.App;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _webViewInitializationCts;
    private bool _webViewInitializationStarted;
#pragma warning disable CS0414
    private bool _webViewInitialized;
#pragma warning restore CS0414
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closing += OnClosing;
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

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_webViewInitializationStarted)
        {
            return;
        }

        _webViewInitializationStarted = true;
        await InitializeWebViewAsync();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _isClosing = true;

        var cts = Interlocked.Exchange(ref _webViewInitializationCts, null);
        if (cts != null)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private async Task InitializeWebViewAsync()
    {
        CancellationTokenSource? localCts = null;
        try
        {
            _webViewInitializationCts = new CancellationTokenSource();
            localCts = _webViewInitializationCts;

            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Anhei4Map",
                "WebView2");

            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder,
                options: null);
            localCts.Token.ThrowIfCancellationRequested();
            if (_isClosing)
            {
                return;
            }

            await webView.EnsureCoreWebView2Async(env);
            localCts.Token.ThrowIfCancellationRequested();
            if (_isClosing)
            {
                return;
            }

            ConfigureWebViewSettings();
            localCts.Token.ThrowIfCancellationRequested();
            if (_isClosing)
            {
                return;
            }

            RegisterWebViewEvents();
            localCts.Token.ThrowIfCancellationRequested();
            if (_isClosing)
            {
                return;
            }

            webView.CoreWebView2.Navigate("https://helltides.com/");

            _webViewInitialized = true;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (!_isClosing)
            {
                MessageBox.Show(
                    $"WebView2 初始化失败，应用无法继续运行。\n\n错误：{ex.Message}",
                    "初始化失败 — Anhei4Map",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }
        finally
        {
            if (localCts != null &&
                Interlocked.CompareExchange(ref _webViewInitializationCts, null, localCts) == localCts)
            {
                localCts.Dispose();
            }
        }
    }

    private void ConfigureWebViewSettings()
    {
        var settings = webView.CoreWebView2.Settings;
        settings.IsScriptEnabled = true;
        settings.IsWebMessageEnabled = false;
        settings.AreDefaultScriptDialogsEnabled = false;
        settings.IsStatusBarEnabled = false;
        settings.AreDevToolsEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsPinchZoomEnabled = false;
    }

    private void RegisterWebViewEvents()
    {
        webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
        webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
        webView.CoreWebView2.DownloadStarting += OnDownloadStarting;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Uri))
        {
            e.Cancel = true;
            return;
        }

        if (!DomainPolicy.IsAllowed(e.Uri))
        {
            e.Cancel = true;
        }
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        e.Cancel = true;
    }
}
