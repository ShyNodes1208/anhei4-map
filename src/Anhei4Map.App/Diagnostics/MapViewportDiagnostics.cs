using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace Anhei4Map.App.Diagnostics;

public static class MapViewportDiagnostics
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private static readonly string OutputDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Anhei4Map",
        "diagnostics",
        "latest");

    public static bool TryWrite(DiagnosticsReport report, byte[] fullPng, byte[] cropPng)
    {
        var jsonFinal = Path.Combine(OutputDirectory, "diagnostics.json");
        var fullFinal = Path.Combine(OutputDirectory, "capture-full.png");
        var cropFinal = Path.Combine(OutputDirectory, "capture-crop.png");
        var jsonTmp = jsonFinal + ".tmp";
        var fullTmp = fullFinal + ".tmp";
        var cropTmp = cropFinal + ".tmp";

        try
        {
            Directory.CreateDirectory(OutputDirectory);
            DeleteIfExists(jsonFinal, fullFinal, cropFinal, jsonTmp, fullTmp, cropTmp);

            File.WriteAllText(jsonTmp, JsonSerializer.Serialize(report, JsonOptions));
            File.WriteAllBytes(fullTmp, fullPng);
            File.WriteAllBytes(cropTmp, cropPng);

            File.Move(jsonTmp, jsonFinal, overwrite: true);
            File.Move(fullTmp, fullFinal, overwrite: true);
            File.Move(cropTmp, cropFinal, overwrite: true);

            return true;
        }
        catch
        {
            DeleteIfExists(jsonFinal, fullFinal, cropFinal, jsonTmp, fullTmp, cropTmp);
            return false;
        }
    }

    public static byte[] EncodePng(BitmapSource source)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static void DeleteIfExists(params string[] paths)
    {
        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }

    public sealed class DiagnosticsReport
    {
        public PageMetrics Page { get; init; } = new();
        public RendererMetrics Renderer { get; init; } = new();
        public CaptureMetrics Capture { get; init; } = new();
        public List<DomCandidate> Candidates { get; init; } = [];
        public DiagnosticResult Result { get; init; } = new();
    }

    public sealed class PageMetrics
    {
        public string Url { get; init; } = string.Empty;
        public string DocumentReadyState { get; init; } = string.Empty;
        public double WindowInnerWidth { get; init; }
        public double WindowInnerHeight { get; init; }
        public double DevicePixelRatio { get; init; }
        public double DocumentClientWidth { get; init; }
        public double DocumentClientHeight { get; init; }
        public double DocumentScrollWidth { get; init; }
        public double DocumentScrollHeight { get; init; }
    }

    public sealed class RendererMetrics
    {
        public double RendererWindowWidth { get; init; }
        public double RendererWindowHeight { get; init; }
        public double RendererWindowActualWidth { get; init; }
        public double RendererWindowActualHeight { get; init; }
        public double WebViewActualWidth { get; init; }
        public double WebViewActualHeight { get; init; }
        public double DpiScaleX { get; init; }
        public double DpiScaleY { get; init; }
        public double WebViewZoomFactor { get; init; }
    }

    public sealed class CaptureMetrics
    {
        public int CapturePixelWidth { get; init; }
        public int CapturePixelHeight { get; init; }
        public double ScaleX { get; init; }
        public double ScaleY { get; init; }
    }

    public sealed class DomCandidate
    {
        public string Selector { get; init; } = string.Empty;
        public int MatchIndex { get; init; }
        public string TagName { get; init; } = string.Empty;
        public string Id { get; init; } = string.Empty;
        public string ClassName { get; init; } = string.Empty;
        public string ParentTagName { get; init; } = string.Empty;
        public string ParentId { get; init; } = string.Empty;
        public string ParentClassName { get; init; } = string.Empty;
        public double Left { get; init; }
        public double Top { get; init; }
        public double Right { get; init; }
        public double Bottom { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }
        public double ClientWidth { get; init; }
        public double ClientHeight { get; init; }
        public double ScrollWidth { get; init; }
        public double ScrollHeight { get; init; }
        public string Display { get; init; } = string.Empty;
        public string Visibility { get; init; } = string.Empty;
        public string Position { get; init; } = string.Empty;
        public string Overflow { get; init; } = string.Empty;
        public string Transform { get; init; } = string.Empty;
        public double Area { get; init; }
        public int Rank { get; init; }
        public bool Selected { get; init; }
    }

    public sealed class DiagnosticResult
    {
        public DomCandidate? SelectedCandidate { get; init; }
        public MapRegionSnapshot ProductionMapRegion { get; init; } = new();
        public int CropLeft { get; init; }
        public int CropTop { get; init; }
        public int CropRight { get; init; }
        public int CropBottom { get; init; }
        public int CropWidth { get; init; }
        public int CropHeight { get; init; }
        public double FinalAspectRatio { get; init; }
    }

    public sealed class MapRegionSnapshot
    {
        public double Left { get; init; }
        public double Top { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }

        [JsonPropertyName("dpr")]
        public double Dpr { get; init; }
    }
}
