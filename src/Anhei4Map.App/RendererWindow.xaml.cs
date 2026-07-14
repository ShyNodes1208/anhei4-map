using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Anhei4Map.App.Diagnostics;
using Anhei4Map.Core.Models;
using Anhei4Map.Core.Services;
using Microsoft.Web.WebView2.Core;

namespace Anhei4Map.App;

public partial class RendererWindow : Window
{
    private const string MapRegionScript = """
        (function() {
          const selectors = ['#map', '.leaflet-container', '[class*="map"]'];
          let best = null, bestArea = 0, bestSelector = '', bestMatchIndex = -1;
          for (const sel of selectors) {
            const el = document.querySelector(sel);
            if (!el) continue;
            const style = getComputedStyle(el);
            if (style.display === 'none' || style.visibility === 'hidden' || style.opacity === '0') continue;
            const r = el.getBoundingClientRect();
            if (r.width <= 0 || r.height <= 0) continue;
            const area = r.width * r.height;
            if (area > bestArea) {
              best = el;
              bestArea = area;
              bestSelector = sel;
              const nodes = document.querySelectorAll(sel);
              bestMatchIndex = Array.prototype.indexOf.call(nodes, el);
            }
          }
          if (!best) return null;
          const r = best.getBoundingClientRect();
          return JSON.stringify({
            left: r.left,
            top: r.top,
            width: r.width,
            height: r.height,
            dpr: window.devicePixelRatio || 1,
            selector: bestSelector,
            matchIndex: bestMatchIndex
          });
        })()
        """;

    private const string DiagnosticMetricsScript = """
        (function() {
          function sanitizeUrl(url) {
            try {
              var u = new URL(url);
              return u.origin + u.pathname;
            } catch (e) {
              return '';
            }
          }

          function isVisible(el) {
            var style = getComputedStyle(el);
            if (style.display === 'none' || style.visibility === 'hidden' || parseFloat(style.opacity) <= 0) return false;
            var r = el.getBoundingClientRect();
            return r.width > 0 && r.height > 0;
          }

          function readCandidate(el, selector, matchIndex) {
            var style = getComputedStyle(el);
            var r = el.getBoundingClientRect();
            var parent = el.parentElement;
            return {
              selector: selector,
              matchIndex: matchIndex,
              tagName: el.tagName || '',
              id: el.id || '',
              className: (el.className && typeof el.className === 'string') ? el.className : '',
              parentTagName: parent ? (parent.tagName || '') : '',
              parentId: parent ? (parent.id || '') : '',
              parentClassName: parent && parent.className && typeof parent.className === 'string' ? parent.className : '',
              left: r.left,
              top: r.top,
              right: r.right,
              bottom: r.bottom,
              width: r.width,
              height: r.height,
              clientWidth: el.clientWidth,
              clientHeight: el.clientHeight,
              scrollWidth: el.scrollWidth,
              scrollHeight: el.scrollHeight,
              display: style.display,
              visibility: style.visibility,
              position: style.position,
              overflow: style.overflow,
              transform: style.transform,
              area: r.width * r.height,
              rank: 0,
              selected: false
            };
          }

          var page = {
            url: sanitizeUrl(location.href),
            documentReadyState: document.readyState,
            windowInnerWidth: window.innerWidth,
            windowInnerHeight: window.innerHeight,
            devicePixelRatio: window.devicePixelRatio || 1,
            documentClientWidth: document.documentElement.clientWidth,
            documentClientHeight: document.documentElement.clientHeight,
            documentScrollWidth: document.documentElement.scrollWidth,
            documentScrollHeight: document.documentElement.scrollHeight
          };

          var selectors = ['#map', '.leaflet-container', '[class*="map"]'];
          var seen = new Set();
          var candidates = [];

          for (var s = 0; s < selectors.length; s++) {
            var sel = selectors[s];
            var nodes = document.querySelectorAll(sel);
            for (var i = 0; i < nodes.length; i++) {
              var el = nodes[i];
              if (seen.has(el)) continue;
              seen.add(el);
              if (!isVisible(el)) continue;
              candidates.push(readCandidate(el, sel, i));
              if (candidates.length >= 50) break;
            }
            if (candidates.length >= 50) break;
          }

          candidates.sort(function(a, b) { return b.area - a.area; });
          for (var j = 0; j < candidates.length; j++) {
            candidates[j].rank = j + 1;
          }

          return JSON.stringify({ page: page, candidates: candidates });
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
    private readonly bool _diagnosticModeEnabled;
    private int _diagnosticsExecuted;
    private long _navigationGeneration;

    public event EventHandler? NavigationReady;

    public RendererWindow(bool diagnoseMapViewport = false)
    {
        _diagnosticModeEnabled = diagnoseMapViewport;
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
        Interlocked.Increment(ref _navigationGeneration);
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

            if (!_isClosed)
            {
                RaiseNavigationReady();
            }
        }
    }

    private void RaiseNavigationReady()
    {
        try
        {
            NavigationReady?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
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
        var result = await TryGetProductionMapRegionAsync();
        return result?.Region;
    }

    private async Task<ProductionMapRegionResult?> TryGetProductionMapRegionAsync()
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
            if (!IsValidRegion(region))
            {
                return null;
            }

            return new ProductionMapRegionResult(
                region,
                dto.Selector ?? string.Empty,
                dto.MatchIndex);
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

    private bool IsDiagnosticNavigationValid(long diagnosticNavigationGeneration) =>
        !_isClosed &&
        _navigationCompletedSuccessfully &&
        webView.CoreWebView2 != null &&
        Interlocked.Read(ref _navigationGeneration) == diagnosticNavigationGeneration;

    private async Task<bool> IsMapVisualReadyAsync()
    {
        if (!Dispatcher.CheckAccess())
        {
            return await await Dispatcher.InvokeAsync(IsMapVisualReadyCoreAsync);
        }

        return await IsMapVisualReadyCoreAsync();
    }

    private async Task<bool> IsMapVisualReadyCoreAsync()
    {
        if (!_webViewInitialized ||
            !_navigationCompletedSuccessfully ||
            _isClosed ||
            webView.CoreWebView2 == null)
        {
            return false;
        }

        const string script = """
            (function() {
              const selectors = ['#map', '.leaflet-container', '[class*="map"]'];
              let best = null, bestArea = 0;

              function isVisible(el) {
                const style = getComputedStyle(el);
                if (style.display === 'none' || style.visibility === 'hidden' || parseFloat(style.opacity) <= 0) return false;
                const r = el.getBoundingClientRect();
                return r.width > 0 && r.height > 0;
              }

              function isMarkerOrControl(el) {
                let node = el;
                while (node && node !== document.body) {
                  const cls = (node.className && typeof node.className === 'string') ? node.className : '';
                  if (/\b(marker|icon|control|popup|tooltip)\b/i.test(cls)) return true;
                  node = node.parentElement;
                }
                return false;
              }

              for (const sel of selectors) {
                const el = document.querySelector(sel);
                if (!el || !isVisible(el)) continue;
                const r = el.getBoundingClientRect();
                const area = r.width * r.height;
                if (area > bestArea) { best = el; bestArea = area; }
              }
              if (!best) return false;

              const imgs = best.querySelectorAll('img');
              for (const img of imgs) {
                if (!img.complete || img.naturalWidth <= 0 || img.naturalHeight <= 0) continue;
                if (!isVisible(img)) continue;
                if (isMarkerOrControl(img)) continue;
                const r = img.getBoundingClientRect();
                const cls = (img.className && typeof img.className === 'string') ? img.className : '';
                const parentCls = img.parentElement && img.parentElement.className ? String(img.parentElement.className) : '';
                const hasTileClass = /\btile\b/i.test(cls) || /\btile\b/i.test(parentCls);
                const largeNatural = img.naturalWidth >= 128 && img.naturalHeight >= 128;
                const largeDisplay = r.width >= 128 && r.height >= 128;
                if (hasTileClass || largeNatural || largeDisplay) return true;
              }

              const canvases = best.querySelectorAll('canvas');
              for (const c of canvases) {
                if (c.width <= 0 || c.height <= 0) continue;
                const style = getComputedStyle(c);
                if (style.display === 'none' || style.visibility === 'hidden' || parseFloat(style.opacity) <= 0) continue;
                const cr = c.getBoundingClientRect();
                if (cr.width > 0 && cr.height > 0) return true;
              }

              function hasLargeBackground(el) {
                if (!isVisible(el)) return false;
                const r = el.getBoundingClientRect();
                if (r.width < 128 || r.height < 128) return false;
                const bg = getComputedStyle(el).backgroundImage;
                return bg && bg !== 'none';
              }

              if (hasLargeBackground(best)) return true;
              for (const el of best.querySelectorAll('*')) {
                if (hasLargeBackground(el)) return true;
              }

              return false;
            })()
            """;

        try
        {
            var result = await webView.CoreWebView2.ExecuteScriptAsync(script);
            return string.Equals(result, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public async Task<BitmapSource?> CaptureAndCropMapAsync()
    {
        if (!Dispatcher.CheckAccess())
        {
            return await await Dispatcher.InvokeAsync(CaptureAndCropMapCoreAsync);
        }

        return await CaptureAndCropMapCoreAsync();
    }

    private async Task<BitmapSource?> CaptureAndCropMapCoreAsync()
    {
        try
        {
            if (!_webViewInitialized ||
                !_navigationCompletedSuccessfully ||
                _isClosed ||
                webView.CoreWebView2 == null ||
                webView.ActualWidth <= 0 ||
                webView.ActualHeight <= 0)
            {
                return null;
            }

            var runDiagnostics = false;
            long diagnosticNavigationGeneration = 0;

            if (_diagnosticModeEnabled &&
                Interlocked.CompareExchange(ref _diagnosticsExecuted, 1, 0) == 0)
            {
                runDiagnostics = true;
                diagnosticNavigationGeneration = Interlocked.Read(ref _navigationGeneration);
            }

            var productionRegion = await TryGetProductionMapRegionAsync();
            if (productionRegion == null)
            {
                return null;
            }

            if (runDiagnostics && !IsDiagnosticNavigationValid(diagnosticNavigationGeneration))
            {
                runDiagnostics = false;
            }

            var region = productionRegion.Region;

            if (!await IsMapVisualReadyAsync())
            {
                return null;
            }

            using var stream = new MemoryStream();
            try
            {
                await webView.CoreWebView2.CapturePreviewAsync(
                    CoreWebView2CapturePreviewImageFormat.Png,
                    stream);
            }
            catch
            {
                return null;
            }

            if (runDiagnostics && !IsDiagnosticNavigationValid(diagnosticNavigationGeneration))
            {
                runDiagnostics = false;
            }

            stream.Position = 0;
            var fullPngBytes = stream.ToArray();
            stream.Position = 0;

            BitmapDecoder decoder;
            try
            {
                decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);
            }
            catch
            {
                return null;
            }

            if (decoder.Frames.Count == 0)
            {
                return null;
            }

            var bitmap = decoder.Frames[0];
            var scaleX = bitmap.PixelWidth / webView.ActualWidth;
            var scaleY = bitmap.PixelHeight / webView.ActualHeight;

            if (!IsFinite(scaleX) || !IsFinite(scaleY))
            {
                return null;
            }

            var left = (int)Math.Floor(region.Left * scaleX);
            var top = (int)Math.Floor(region.Top * scaleY);
            var right = (int)Math.Ceiling((region.Left + region.Width) * scaleX);
            var bottom = (int)Math.Ceiling((region.Top + region.Height) * scaleY);

            left = Clamp(left, 0, bitmap.PixelWidth);
            top = Clamp(top, 0, bitmap.PixelHeight);
            right = Clamp(right, 0, bitmap.PixelWidth);
            bottom = Clamp(bottom, 0, bitmap.PixelHeight);

            var cropWidth = right - left;
            var cropHeight = bottom - top;

            if (cropWidth <= 0 ||
                cropHeight <= 0 ||
                left >= bitmap.PixelWidth ||
                top >= bitmap.PixelHeight)
            {
                return null;
            }

            try
            {
                var cropped = new CroppedBitmap(bitmap, new Int32Rect(left, top, cropWidth, cropHeight));
                cropped.Freeze();

                if (runDiagnostics &&
                    IsDiagnosticNavigationValid(diagnosticNavigationGeneration))
                {
                    _ = RunMapViewportDiagnosticsAsync(
                        diagnosticNavigationGeneration,
                        productionRegion,
                        bitmap,
                        cropped,
                        scaleX,
                        scaleY,
                        left,
                        top,
                        right,
                        bottom,
                        cropWidth,
                        cropHeight,
                        fullPngBytes);
                }

                return cropped;
            }
            catch
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private static int Clamp(int value, int min, int max) =>
        Math.Max(min, Math.Min(value, max));

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

    private async Task RunMapViewportDiagnosticsAsync(
        long diagnosticNavigationGeneration,
        ProductionMapRegionResult productionRegion,
        BitmapSource bitmap,
        BitmapSource frozenCroppedBitmap,
        double scaleX,
        double scaleY,
        int left,
        int top,
        int right,
        int bottom,
        int cropWidth,
        int cropHeight,
        byte[] fullPngBytes)
    {
        try
        {
            if (!IsDiagnosticNavigationValid(diagnosticNavigationGeneration))
            {
                return;
            }

            if (webView.CoreWebView2 == null)
            {
                return;
            }

            var raw = await webView.CoreWebView2.ExecuteScriptAsync(DiagnosticMetricsScript);
            if (!IsDiagnosticNavigationValid(diagnosticNavigationGeneration))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(raw) || raw == "null")
            {
                return;
            }

            var json = JsonSerializer.Deserialize<string>(raw, JsonOptions);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var metrics = JsonSerializer.Deserialize<DiagnosticMetricsJson>(json, JsonOptions);
            if (metrics?.Page == null)
            {
                return;
            }

            var dpiScaleX = 1.0;
            var dpiScaleY = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }

            var productionSelector = productionRegion.Selector;
            var productionMatchIndex = productionRegion.MatchIndex;
            MapViewportDiagnostics.DomCandidate? selectedCandidate = null;
            var candidates = new List<MapViewportDiagnostics.DomCandidate>();

            foreach (var c in metrics.Candidates ?? [])
            {
                var selected = string.Equals(c.Selector, productionSelector, StringComparison.Ordinal) &&
                               c.MatchIndex == productionMatchIndex;
                var candidate = ToDomCandidate(c, selected);
                candidates.Add(candidate);
                if (selected)
                {
                    selectedCandidate = candidate;
                }
            }

            var finalAspectRatio = cropHeight > 0 ? (double)cropWidth / cropHeight : 0;
            var region = productionRegion.Region;

            var report = new MapViewportDiagnostics.DiagnosticsReport
            {
                Page = new MapViewportDiagnostics.PageMetrics
                {
                    Url = metrics.Page.Url ?? string.Empty,
                    DocumentReadyState = metrics.Page.DocumentReadyState ?? string.Empty,
                    WindowInnerWidth = metrics.Page.WindowInnerWidth,
                    WindowInnerHeight = metrics.Page.WindowInnerHeight,
                    DevicePixelRatio = metrics.Page.DevicePixelRatio,
                    DocumentClientWidth = metrics.Page.DocumentClientWidth,
                    DocumentClientHeight = metrics.Page.DocumentClientHeight,
                    DocumentScrollWidth = metrics.Page.DocumentScrollWidth,
                    DocumentScrollHeight = metrics.Page.DocumentScrollHeight
                },
                Renderer = new MapViewportDiagnostics.RendererMetrics
                {
                    RendererWindowWidth = Width,
                    RendererWindowHeight = Height,
                    RendererWindowActualWidth = ActualWidth,
                    RendererWindowActualHeight = ActualHeight,
                    WebViewActualWidth = webView.ActualWidth,
                    WebViewActualHeight = webView.ActualHeight,
                    DpiScaleX = dpiScaleX,
                    DpiScaleY = dpiScaleY,
                    WebViewZoomFactor = webView.ZoomFactor
                },
                Capture = new MapViewportDiagnostics.CaptureMetrics
                {
                    CapturePixelWidth = bitmap.PixelWidth,
                    CapturePixelHeight = bitmap.PixelHeight,
                    ScaleX = scaleX,
                    ScaleY = scaleY
                },
                Candidates = candidates,
                Result = new MapViewportDiagnostics.DiagnosticResult
                {
                    SelectedCandidate = selectedCandidate,
                    ProductionMapRegion = new MapViewportDiagnostics.MapRegionSnapshot
                    {
                        Left = region.Left,
                        Top = region.Top,
                        Width = region.Width,
                        Height = region.Height,
                        Dpr = region.DevicePixelRatio
                    },
                    CropLeft = left,
                    CropTop = top,
                    CropRight = right,
                    CropBottom = bottom,
                    CropWidth = cropWidth,
                    CropHeight = cropHeight,
                    FinalAspectRatio = finalAspectRatio
                }
            };

            var fullBytes = fullPngBytes;
            var frozenCrop = frozenCroppedBitmap;

            await Task.Run(() =>
            {
                try
                {
                    var cropBytes = MapViewportDiagnostics.EncodePng(frozenCrop);
                    MapViewportDiagnostics.TryWrite(report, fullBytes, cropBytes);
                }
                catch
                {
                }
            });
        }
        catch
        {
        }
    }

    private static MapViewportDiagnostics.DomCandidate ToDomCandidate(
        DiagnosticCandidateJson c,
        bool selected) =>
        new()
        {
            Selector = c.Selector ?? string.Empty,
            MatchIndex = c.MatchIndex,
            TagName = c.TagName ?? string.Empty,
            Id = c.Id ?? string.Empty,
            ClassName = c.ClassName ?? string.Empty,
            ParentTagName = c.ParentTagName ?? string.Empty,
            ParentId = c.ParentId ?? string.Empty,
            ParentClassName = c.ParentClassName ?? string.Empty,
            Left = c.Left,
            Top = c.Top,
            Right = c.Right,
            Bottom = c.Bottom,
            Width = c.Width,
            Height = c.Height,
            ClientWidth = c.ClientWidth,
            ClientHeight = c.ClientHeight,
            ScrollWidth = c.ScrollWidth,
            ScrollHeight = c.ScrollHeight,
            Display = c.Display ?? string.Empty,
            Visibility = c.Visibility ?? string.Empty,
            Position = c.Position ?? string.Empty,
            Overflow = c.Overflow ?? string.Empty,
            Transform = c.Transform ?? string.Empty,
            Area = c.Area,
            Rank = c.Rank,
            Selected = selected
        };

    private sealed record ProductionMapRegionResult(
        MapRegion Region,
        string Selector,
        int MatchIndex);

    private sealed record MapRegionJson(
        double Left,
        double Top,
        double Width,
        double Height,
        [property: JsonPropertyName("dpr")] double Dpr,
        string? Selector,
        int MatchIndex);

    private sealed record DiagnosticMetricsJson(
        DiagnosticPageJson? Page,
        List<DiagnosticCandidateJson>? Candidates);

    private sealed record DiagnosticPageJson(
        string? Url,
        string? DocumentReadyState,
        double WindowInnerWidth,
        double WindowInnerHeight,
        double DevicePixelRatio,
        double DocumentClientWidth,
        double DocumentClientHeight,
        double DocumentScrollWidth,
        double DocumentScrollHeight);

    private sealed record DiagnosticCandidateJson(
        string? Selector,
        int MatchIndex,
        string? TagName,
        string? Id,
        string? ClassName,
        string? ParentTagName,
        string? ParentId,
        string? ParentClassName,
        double Left,
        double Top,
        double Right,
        double Bottom,
        double Width,
        double Height,
        double ClientWidth,
        double ClientHeight,
        double ScrollWidth,
        double ScrollHeight,
        string? Display,
        string? Visibility,
        string? Position,
        string? Overflow,
        string? Transform,
        double Area,
        int Rank,
        bool Selected);
}
