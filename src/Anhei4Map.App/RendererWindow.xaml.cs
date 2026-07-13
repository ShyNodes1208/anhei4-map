using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Anhei4Map.Core.Models;
using Anhei4Map.Core.Services;
using Microsoft.Web.WebView2.Core;

namespace Anhei4Map.App;

public partial class RendererWindow : Window
{
    private const string MapRegionScript = """
        (function() {
          const selectors = ['#map', '.leaflet-container', '[class*="map"]'];
          let best = null, bestArea = 0;
          for (const sel of selectors) {
            const el = document.querySelector(sel);
            if (!el) continue;
            const style = getComputedStyle(el);
            if (style.display === 'none' || style.visibility === 'hidden' || style.opacity === '0') continue;
            const r = el.getBoundingClientRect();
            if (r.width <= 0 || r.height <= 0) continue;
            const area = r.width * r.height;
            if (area > bestArea) { best = el; bestArea = area; }
          }
          if (!best) return null;
          const r = best.getBoundingClientRect();
          return JSON.stringify({ left: r.left, top: r.top, width: r.width, height: r.height, dpr: window.devicePixelRatio || 1 });
        })()
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private bool _webViewInitializationStarted;
    private bool _webViewInitialized;
    private bool _navigationCompletedSuccessfully;
    private bool _isClosed;

    public RendererWindow()
    {
        InitializeComponent();
        Left = -10000;
        Top = -10000;
        Loaded += OnLoaded;
        Closed += OnClosed;
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

    private void OnClosed(object? sender, EventArgs e)
    {
        _isClosed = true;

        if (webView.CoreWebView2 != null)
        {
            webView.CoreWebView2.NavigationStarting -= OnNavigationStarting;
            webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
            webView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
            webView.CoreWebView2.DownloadStarting -= OnDownloadStarting;
        }
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            if (_isClosed)
            {
                return;
            }

            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Anhei4Map",
                "WebView2");

            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder,
                options: null);

            if (_isClosed)
            {
                return;
            }

            await webView.EnsureCoreWebView2Async(env);

            if (_isClosed)
            {
                return;
            }

            ConfigureWebViewSettings();
            RegisterWebViewEvents();
            webView.CoreWebView2.Navigate("https://helltides.com/");

            _webViewInitialized = true;
        }
        catch (Exception ex)
        {
            if (!_isClosed)
            {
                MessageBox.Show(
                    $"WebView2 初始化失败，应用无法继续运行。\n\n错误：{ex.Message}",
                    "初始化失败 — Anhei4Map",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown();
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
        webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
        webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
        webView.CoreWebView2.DownloadStarting += OnDownloadStarting;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        _navigationCompletedSuccessfully = false;

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

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _navigationCompletedSuccessfully = true;
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

    public async Task<MapRegion?> TryGetMapRegionAsync()
    {
        try
        {
            if (!_webViewInitialized ||
                !_navigationCompletedSuccessfully ||
                webView.CoreWebView2 == null ||
                _isClosed)
            {
                return null;
            }

            var raw = await webView.CoreWebView2.ExecuteScriptAsync(MapRegionScript);
            if (string.IsNullOrWhiteSpace(raw) || raw == "null")
            {
                return null;
            }

            var json = JsonSerializer.Deserialize<string>(raw, JsonOptions);
            if (string.IsNullOrWhiteSpace(json) || json == "null")
            {
                return null;
            }

            var dto = JsonSerializer.Deserialize<MapRegionJson>(json, JsonOptions);
            if (dto == null)
            {
                return null;
            }

            var region = new MapRegion(dto.Left, dto.Top, dto.Width, dto.Height, dto.Dpr);
            return IsValidRegion(region) ? region : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsValidRegion(MapRegion region)
    {
        return IsFinite(region.Left) &&
               IsFinite(region.Top) &&
               IsFinite(region.Width) &&
               IsFinite(region.Height) &&
               IsFinite(region.DevicePixelRatio) &&
               region.Width > 0 &&
               region.Height > 0 &&
               region.Left >= 0 &&
               region.Top >= 0 &&
               region.DevicePixelRatio > 0;
    }

    private static bool IsFinite(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);

    private sealed record MapRegionJson(
        double Left,
        double Top,
        double Width,
        double Height,
        [property: JsonPropertyName("dpr")] double Dpr);
}
