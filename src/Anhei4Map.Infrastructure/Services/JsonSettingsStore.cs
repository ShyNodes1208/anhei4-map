using System.Text;
using System.Text.Json;
using Anhei4Map.Core.Models;

namespace Anhei4Map.Infrastructure.Services;

public class JsonSettingsStore
{
    private readonly string _filePath;
    private readonly string _tmpFilePath;
    private readonly string _bakFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonSettingsStore(string directory)
    {
        _filePath = Path.Combine(directory, "settings.json");
        _tmpFilePath = Path.Combine(directory, "settings.json.tmp");
        _bakFilePath = Path.Combine(directory, "settings.json.bak");
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_filePath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_tmpFilePath, json, Encoding.UTF8);
        File.Move(_tmpFilePath, _filePath, overwrite: true);
    }

    public async Task<AppSettings> LoadAsync()
    {
        CleanupResidualTmp();

        if (!File.Exists(_filePath))
        {
            return AppSettings.CreateDefaults();
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(_filePath, Encoding.UTF8);
        }
        catch (IOException)
        {
            throw;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null)
            {
                BackupCorruptedFile();
                return AppSettings.CreateDefaults();
            }

            if (!settings.Validate())
            {
                BackupCorruptedFile();
                return AppSettings.CreateDefaults();
            }

            return settings;
        }
        catch (JsonException)
        {
            BackupCorruptedFile();
            return AppSettings.CreateDefaults();
        }
    }

    private void CleanupResidualTmp()
    {
        if (File.Exists(_tmpFilePath))
        {
            File.Delete(_tmpFilePath);
        }
    }

    private void BackupCorruptedFile()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        if (!File.Exists(_bakFilePath))
        {
            File.Move(_filePath, _bakFilePath);
            return;
        }

        var directory = Path.GetDirectoryName(_filePath)!;
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'");
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var uniquePath = Path.Combine(directory, $"settings.json.bak.{timestamp}.{suffix}");
        File.Move(_filePath, uniquePath);
    }
}
