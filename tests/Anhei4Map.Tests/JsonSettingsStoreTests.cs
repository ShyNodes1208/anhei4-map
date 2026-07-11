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
}
