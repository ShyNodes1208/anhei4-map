using System.Text;
using System.Text.Json;
using Anhei4Map.Core.Models;

namespace Anhei4Map.Infrastructure.Services;

public class JsonSettingsStore
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonSettingsStore(string directory)
    {
        _filePath = Path.Combine(directory, "settings.json");
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_filePath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json, Encoding.UTF8);
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return AppSettings.CreateDefaults();
        }

        var json = await File.ReadAllTextAsync(_filePath, Encoding.UTF8);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
               ?? AppSettings.CreateDefaults();
    }
}
