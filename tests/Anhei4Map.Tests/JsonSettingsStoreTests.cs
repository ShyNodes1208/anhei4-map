using System.Text.Json;
using Anhei4Map.Core.Models;
using Anhei4Map.Infrastructure.Services;

namespace Anhei4Map.Tests;

public class JsonSettingsStoreTests
{
    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Anhei4Map.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void CleanupTempDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string SettingsPath(string directory) => Path.Combine(directory, "settings.json");

    private static string BaseBakPath(string directory) => Path.Combine(directory, "settings.json.bak");

    private static string[] BackupFiles(string directory) =>
        Directory.GetFiles(directory, "settings.json.bak*");

    private static bool IsUniqueTimestampedBackup(string fileName)
    {
        const string prefix = "settings.json.bak.";
        if (!fileName.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var remainder = fileName[prefix.Length..];
        var separator = remainder.LastIndexOf('.');
        if (separator <= 0 || separator >= remainder.Length - 1)
        {
            return false;
        }

        var timestamp = remainder[..separator];
        var suffix = remainder[(separator + 1)..];
        return timestamp.EndsWith("Z", StringComparison.Ordinal)
            && suffix.Length == 32
            && suffix.All(static c => Uri.IsHexDigit(c));
    }

    [Fact]
    public async Task Save_FileExists()
    {
        var directory = CreateTempDirectory();

        try
        {
            var store = new JsonSettingsStore(directory);
            var settings = AppSettings.CreateDefaults();

            await store.SaveAsync(settings);

            Assert.True(File.Exists(Path.Combine(directory, "settings.json")));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task SaveThenLoad_RoundTrip()
    {
        var directory = CreateTempDirectory();

        try
        {
            var store = new JsonSettingsStore(directory);
            var defaults = AppSettings.CreateDefaults();
            var settings = defaults with
            {
                Placement = defaults.Placement with { Width = 800, Height = 450 },
                ZoomLevel = 1.5
            };

            await store.SaveAsync(settings);
            var loaded = await store.LoadAsync();

            Assert.Equal(settings.Placement.Width, loaded.Placement.Width);
            Assert.Equal(settings.Placement.Height, loaded.Placement.Height);
            Assert.Equal(settings.ZoomLevel, loaded.ZoomLevel);
            Assert.Equal(settings.InitialState, loaded.InitialState);
            Assert.Equal(settings.Hotkeys.Count, loaded.Hotkeys.Count);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Save_CreatesDirectoryIfMissing()
    {
        var directory = CreateTempDirectory();
        var nestedDirectory = Path.Combine(directory, "nested", "settings-dir");

        try
        {
            var store = new JsonSettingsStore(nestedDirectory);
            var settings = AppSettings.CreateDefaults();

            await store.SaveAsync(settings);

            Assert.True(Directory.Exists(nestedDirectory));
            Assert.True(File.Exists(Path.Combine(nestedDirectory, "settings.json")));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Load_ReturnsDefaultsWhenFileNotFound()
    {
        var directory = CreateTempDirectory();

        try
        {
            var store = new JsonSettingsStore(directory);
            var expected = AppSettings.CreateDefaults();

            var loaded = await store.LoadAsync();

            Assert.Equal(expected.Placement.Width, loaded.Placement.Width);
            Assert.Equal(expected.Placement.Height, loaded.Placement.Height);
            Assert.Equal(expected.ZoomLevel, loaded.ZoomLevel);
            Assert.Equal(expected.InitialState, loaded.InitialState);
            Assert.Equal(expected.Hotkeys.Count, loaded.Hotkeys.Count);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task SaveMultiple_UsesLatestValues()
    {
        var directory = CreateTempDirectory();

        try
        {
            var store = new JsonSettingsStore(directory);
            var defaults = AppSettings.CreateDefaults();
            var first = defaults with { ZoomLevel = 2.0 };
            var second = defaults with
            {
                Placement = defaults.Placement with { Width = 720, Height = 400 },
                ZoomLevel = 3.0
            };

            await store.SaveAsync(first);
            await store.SaveAsync(second);
            var loaded = await store.LoadAsync();

            Assert.Equal(720, loaded.Placement.Width);
            Assert.Equal(400, loaded.Placement.Height);
            Assert.Equal(3.0, loaded.ZoomLevel);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task SavedJson_UsesExpectedPropertyNames()
    {
        var directory = CreateTempDirectory();

        try
        {
            var store = new JsonSettingsStore(directory);
            var settings = AppSettings.CreateDefaults();

            await store.SaveAsync(settings);
            var json = await File.ReadAllTextAsync(Path.Combine(directory, "settings.json"));

            Assert.Contains("\"Hotkeys\"", json);
            Assert.Contains("\"Placement\"", json);
            Assert.Contains("\"ZoomLevel\"", json);
            Assert.Contains("\"InitialState\"", json);
            Assert.Contains("\"Width\"", json);
            Assert.Contains("\"Opacity\"", json);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Load_CorruptedJson_ReturnsDefaults()
    {
        var directory = CreateTempDirectory();

        try
        {
            var settingsPath = Path.Combine(directory, "settings.json");
            await File.WriteAllTextAsync(settingsPath, "{ not valid json }}}");

            var store = new JsonSettingsStore(directory);
            var expected = AppSettings.CreateDefaults();

            var loaded = await store.LoadAsync();

            Assert.Equal(expected.Placement.Width, loaded.Placement.Width);
            Assert.Equal(expected.Placement.Height, loaded.Placement.Height);
            Assert.Equal(expected.ZoomLevel, loaded.ZoomLevel);
            Assert.Equal(expected.InitialState, loaded.InitialState);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Load_CorruptedJson_RenamesToBak()
    {
        var directory = CreateTempDirectory();

        try
        {
            var settingsPath = Path.Combine(directory, "settings.json");
            await File.WriteAllTextAsync(settingsPath, "{ corrupted");

            var store = new JsonSettingsStore(directory);
            await store.LoadAsync();

            Assert.True(File.Exists(Path.Combine(directory, "settings.json.bak")));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Load_CorruptedJson_BakContentsMatchOriginal()
    {
        var directory = CreateTempDirectory();
        const string corruptedJson = "{ corrupted json content";

        try
        {
            var settingsPath = Path.Combine(directory, "settings.json");
            await File.WriteAllTextAsync(settingsPath, corruptedJson);

            var store = new JsonSettingsStore(directory);
            await store.LoadAsync();

            var bakContent = await File.ReadAllTextAsync(Path.Combine(directory, "settings.json.bak"));
            Assert.Equal(corruptedJson, bakContent);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Load_ExistingBak_NotOverwrittenByNewCorruption()
    {
        var directory = CreateTempDirectory();
        const string originalBakContent = "original bak content";
        const string corruptedJson = "{ new corruption";

        try
        {
            var bakPath = Path.Combine(directory, "settings.json.bak");
            var settingsPath = Path.Combine(directory, "settings.json");
            await File.WriteAllTextAsync(bakPath, originalBakContent);
            await File.WriteAllTextAsync(settingsPath, corruptedJson);

            var store = new JsonSettingsStore(directory);
            await store.LoadAsync();

            var preservedBak = await File.ReadAllTextAsync(bakPath);
            Assert.Equal(originalBakContent, preservedBak);

            var timestampedBaks = Directory.GetFiles(directory)
                .Where(path => Path.GetFileName(path).StartsWith("settings.json.bak.", StringComparison.Ordinal))
                .ToArray();
            Assert.Single(timestampedBaks);
            var newBakContent = await File.ReadAllTextAsync(timestampedBaks[0]);
            Assert.Equal(corruptedJson, newBakContent);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task SaveAtomic_Success_FormalFileCorrect()
    {
        var directory = CreateTempDirectory();

        try
        {
            var store = new JsonSettingsStore(directory);
            var defaults = AppSettings.CreateDefaults();
            var settings = defaults with { ZoomLevel = 2.5 };

            await store.SaveAsync(settings);

            var json = await File.ReadAllTextAsync(Path.Combine(directory, "settings.json"));
            Assert.Contains("\"ZoomLevel\": 2.5", json);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task SaveAtomic_Success_TmpCleaned()
    {
        var directory = CreateTempDirectory();

        try
        {
            var store = new JsonSettingsStore(directory);
            await store.SaveAsync(AppSettings.CreateDefaults());

            Assert.False(File.Exists(Path.Combine(directory, "settings.json.tmp")));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Load_ValidJson_DoesNotCreateBak()
    {
        var directory = CreateTempDirectory();

        try
        {
            var store = new JsonSettingsStore(directory);
            await store.SaveAsync(AppSettings.CreateDefaults());
            await store.LoadAsync();

            Assert.False(File.Exists(Path.Combine(directory, "settings.json.bak")));
            Assert.Empty(Directory.GetFiles(directory)
                .Where(path => Path.GetFileName(path).StartsWith("settings.json.bak.", StringComparison.Ordinal)));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Load_FileNotFound_ReturnsDefaults()
    {
        var directory = CreateTempDirectory();

        try
        {
            var store = new JsonSettingsStore(directory);
            var expected = AppSettings.CreateDefaults();

            var loaded = await store.LoadAsync();

            Assert.Equal(expected.Placement.Width, loaded.Placement.Width);
            Assert.Equal(expected.Placement.Height, loaded.Placement.Height);
            Assert.Equal(expected.ZoomLevel, loaded.ZoomLevel);
            Assert.Equal(expected.InitialState, loaded.InitialState);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Load_WidthZero_ReturnsDefaults()
    {
        var directory = CreateTempDirectory();

        try
        {
            var defaults = AppSettings.CreateDefaults();
            var invalidJson = JsonSerializer.Serialize(
                defaults with { Placement = defaults.Placement with { Width = 0 } });
            await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), invalidJson);

            var store = new JsonSettingsStore(directory);
            var loaded = await store.LoadAsync();

            Assert.Equal(640, loaded.Placement.Width);
            Assert.True(File.Exists(Path.Combine(directory, "settings.json.bak")));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Load_OpacityAboveOne_ReturnsDefaults()
    {
        var directory = CreateTempDirectory();

        try
        {
            var defaults = AppSettings.CreateDefaults();
            var invalidJson = JsonSerializer.Serialize(
                defaults with { Placement = defaults.Placement with { Opacity = 1.5 } });
            await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), invalidJson);

            var store = new JsonSettingsStore(directory);
            var loaded = await store.LoadAsync();

            Assert.Equal(0.9, loaded.Placement.Opacity);
            Assert.True(File.Exists(Path.Combine(directory, "settings.json.bak")));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Load_ZoomLevelOutOfRange_ReturnsDefaults()
    {
        var directory = CreateTempDirectory();

        try
        {
            var defaults = AppSettings.CreateDefaults();
            var invalidJson = JsonSerializer.Serialize(defaults with { ZoomLevel = 10.0 });
            await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), invalidJson);

            var store = new JsonSettingsStore(directory);
            var loaded = await store.LoadAsync();

            Assert.Equal(1.0, loaded.ZoomLevel);
            Assert.True(File.Exists(Path.Combine(directory, "settings.json.bak")));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Load_HeightBelowMin_ReturnsDefaults()
    {
        var directory = CreateTempDirectory();

        try
        {
            var defaults = AppSettings.CreateDefaults();
            var invalidJson = JsonSerializer.Serialize(
                defaults with { Placement = defaults.Placement with { Height = 100 } });
            await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), invalidJson);

            var store = new JsonSettingsStore(directory);
            var loaded = await store.LoadAsync();

            Assert.Equal(360, loaded.Placement.Height);
            Assert.True(File.Exists(Path.Combine(directory, "settings.json.bak")));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Backup_UsesBaseBakNameForFirstCorruption()
    {
        var directory = CreateTempDirectory();
        const string corruptedJson = "{ first corruption }";

        try
        {
            await File.WriteAllTextAsync(SettingsPath(directory), corruptedJson);
            var store = new JsonSettingsStore(directory);

            await store.LoadAsync();

            Assert.True(File.Exists(BaseBakPath(directory)));
            Assert.False(File.Exists(SettingsPath(directory)));
            Assert.Equal(corruptedJson, await File.ReadAllTextAsync(BaseBakPath(directory)));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Backup_UsesFullGuidSuffix()
    {
        var directory = CreateTempDirectory();

        try
        {
            await File.WriteAllTextAsync(BaseBakPath(directory), "existing");
            await File.WriteAllTextAsync(SettingsPath(directory), "{ corrupt }");
            var store = new JsonSettingsStore(directory);
            await store.LoadAsync();

            var additional = BackupFiles(directory)
                .Where(p => !string.Equals(p, BaseBakPath(directory), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.Single(additional);
            var fileName = Path.GetFileName(additional[0]);
            Assert.True(IsUniqueTimestampedBackup(fileName));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Backup_ExistingBaseBak_CreatesUniqueAdditionalBackup()
    {
        var directory = CreateTempDirectory();
        const string originalBakContent = "original bak content";
        const string corruptedJson = "{ new corruption }";

        try
        {
            await File.WriteAllTextAsync(BaseBakPath(directory), originalBakContent);
            await File.WriteAllTextAsync(SettingsPath(directory), corruptedJson);
            var store = new JsonSettingsStore(directory);

            await store.LoadAsync();

            Assert.Equal(originalBakContent, await File.ReadAllTextAsync(BaseBakPath(directory)));

            var additionalBackups = BackupFiles(directory)
                .Where(path => !string.Equals(path, BaseBakPath(directory), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.Single(additionalBackups);
            Assert.True(IsUniqueTimestampedBackup(Path.GetFileName(additionalBackups[0])));
            Assert.Equal(corruptedJson, await File.ReadAllTextAsync(additionalBackups[0]));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Backup_MultipleBackups_AreAllUnique()
    {
        var directory = CreateTempDirectory();

        try
        {
            var store = new JsonSettingsStore(directory);

            await File.WriteAllTextAsync(SettingsPath(directory), "{ corrupt A }");
            await store.LoadAsync();

            await File.WriteAllTextAsync(SettingsPath(directory), "{ corrupt B }");
            await store.LoadAsync();

            await File.WriteAllTextAsync(SettingsPath(directory), "{ corrupt C }");
            await store.LoadAsync();

            var bakFiles = BackupFiles(directory);
            Assert.True(bakFiles.Length >= 3);
            Assert.Equal(bakFiles.Distinct(StringComparer.OrdinalIgnoreCase).Count(), bakFiles.Length);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Backup_ExistingTimestampLikeBak_IsNotOverwritten()
    {
        var directory = CreateTempDirectory();
        const string existingBackupContent = "existing timestamp backup";
        const string corruptedJson = "{ latest corruption }";
        var existingBackupPath = Path.Combine(directory, "settings.json.bak.20260712T120000000Z.abc12345");

        try
        {
            await File.WriteAllTextAsync(existingBackupPath, existingBackupContent);
            await File.WriteAllTextAsync(BaseBakPath(directory), "base bak");
            await File.WriteAllTextAsync(SettingsPath(directory), corruptedJson);
            var store = new JsonSettingsStore(directory);

            await store.LoadAsync();

            Assert.Equal(existingBackupContent, await File.ReadAllTextAsync(existingBackupPath));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Backup_EveryBackupPreservesOriginalBytes()
    {
        var directory = CreateTempDirectory();
        const string firstCorruption = "{ corrupt A bytes }";
        const string secondCorruption = "{ corrupt B bytes }";
        const string thirdCorruption = "{ corrupt C bytes }";

        try
        {
            var store = new JsonSettingsStore(directory);

            await File.WriteAllTextAsync(SettingsPath(directory), firstCorruption);
            await store.LoadAsync();

            await File.WriteAllTextAsync(SettingsPath(directory), secondCorruption);
            await store.LoadAsync();

            await File.WriteAllTextAsync(SettingsPath(directory), thirdCorruption);
            await store.LoadAsync();

            var backupContents = BackupFiles(directory)
                .Select(path => File.ReadAllText(path))
                .ToArray();

            Assert.Contains(firstCorruption, backupContents);
            Assert.Contains(secondCorruption, backupContents);
            Assert.Contains(thirdCorruption, backupContents);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Backup_SemanticInvalidSettings_UsesUniqueBackupPolicy()
    {
        var directory = CreateTempDirectory();
        const string originalBakContent = "existing semantic bak";
        var defaults = AppSettings.CreateDefaults();
        var invalidJson = JsonSerializer.Serialize(
            defaults with { Placement = defaults.Placement with { Width = 0 } });

        try
        {
            await File.WriteAllTextAsync(BaseBakPath(directory), originalBakContent);
            await File.WriteAllTextAsync(SettingsPath(directory), invalidJson);
            var store = new JsonSettingsStore(directory);

            await store.LoadAsync();

            Assert.Equal(originalBakContent, await File.ReadAllTextAsync(BaseBakPath(directory)));

            var additionalBackups = BackupFiles(directory)
                .Where(path => !string.Equals(path, BaseBakPath(directory), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.Single(additionalBackups);
            Assert.True(IsUniqueTimestampedBackup(Path.GetFileName(additionalBackups[0])));
            Assert.Equal(invalidJson, await File.ReadAllTextAsync(additionalBackups[0]));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Backup_SyntaxCorrupted_UsesUniqueBackupPolicy()
    {
        var directory = CreateTempDirectory();
        const string originalBakContent = "existing syntax bak";
        const string corruptedJson = "{ syntax corruption }";

        try
        {
            await File.WriteAllTextAsync(BaseBakPath(directory), originalBakContent);
            await File.WriteAllTextAsync(SettingsPath(directory), corruptedJson);
            var store = new JsonSettingsStore(directory);

            await store.LoadAsync();

            Assert.Equal(originalBakContent, await File.ReadAllTextAsync(BaseBakPath(directory)));

            var additionalBackups = BackupFiles(directory)
                .Where(path => !string.Equals(path, BaseBakPath(directory), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.Single(additionalBackups);
            Assert.True(IsUniqueTimestampedBackup(Path.GetFileName(additionalBackups[0])));
            Assert.Equal(corruptedJson, await File.ReadAllTextAsync(additionalBackups[0]));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Backup_ExistingManyBackups_DoesNotDeleteOldBackups()
    {
        var directory = CreateTempDirectory();
        var existingBackups = Enumerable.Range(0, 5)
            .Select(i => Path.Combine(directory, $"settings.json.bak.20260712T12000000{i}Z.deadbeef"))
            .ToArray();

        try
        {
            foreach (var backupPath in existingBackups)
            {
                await File.WriteAllTextAsync(backupPath, $"old backup {Path.GetFileName(backupPath)}");
            }

            await File.WriteAllTextAsync(BaseBakPath(directory), "base bak");
            await File.WriteAllTextAsync(SettingsPath(directory), "{ new corruption }");
            var store = new JsonSettingsStore(directory);

            await store.LoadAsync();

            foreach (var backupPath in existingBackups)
            {
                Assert.True(File.Exists(backupPath));
            }

            Assert.True(BackupFiles(directory).Length >= existingBackups.Length + 1);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }
}
