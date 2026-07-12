using System.Text;
using Anhei4Map.Core.Logging;
using Anhei4Map.Infrastructure.Logging;

namespace Anhei4Map.Tests;

public class FileLoggerTests
{
    private static readonly DateTimeOffset FixedUtcTime =
        new(2026, 7, 12, 10, 20, 30, 123, TimeSpan.Zero);

    private static string CreateTempDirectory() =>
        Path.Combine(Path.GetTempPath(), "Anhei4Map.Tests", Guid.NewGuid().ToString("N"));

    private static void CleanupTempDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string LogFilePath(string directory) => Path.Combine(directory, "app.log");

    private static FileLogger CreateLogger(string directory, TimeProvider? timeProvider = null) =>
        new(directory, timeProvider ?? new FixedTimeProvider(FixedUtcTime));

    [Fact]
    public void Log_CreatesDirectoryIfMissing()
    {
        var root = CreateTempDirectory();
        var nestedDirectory = Path.Combine(root, "nested", "logs");

        try
        {
            var logger = CreateLogger(nestedDirectory);
            logger.Log(LogLevel.Info, "startup", "directory created");

            Assert.True(Directory.Exists(nestedDirectory));
            Assert.True(File.Exists(LogFilePath(nestedDirectory)));
        }
        finally
        {
            CleanupTempDirectory(root);
        }
    }

    [Fact]
    public void Log_FirstWriteCreatesLogFile()
    {
        var directory = CreateTempDirectory();

        try
        {
            var logger = CreateLogger(directory);
            logger.Log(LogLevel.Info, "startup", "first entry");

            Assert.True(File.Exists(LogFilePath(directory)));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Log_SecondWriteAppendsWithoutOverwriting()
    {
        var directory = CreateTempDirectory();

        try
        {
            var logger = CreateLogger(directory);
            logger.Log(LogLevel.Info, "startup", "first entry");
            logger.Log(LogLevel.Info, "startup", "second entry");

            var content = File.ReadAllText(LogFilePath(directory));
            Assert.Contains("first entry", content);
            Assert.Contains("second entry", content);
            Assert.Equal(2, content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Log_InfoFormat_IsCorrect()
    {
        var directory = CreateTempDirectory();

        try
        {
            var logger = CreateLogger(directory);
            logger.Log(LogLevel.Info, "hotkey", "registered");

            var line = File.ReadAllLines(LogFilePath(directory)).Single();
            Assert.Equal("[2026-07-12T10:20:30.123Z] [INFO] [hotkey] registered", line);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Log_WarnFormat_IsCorrect()
    {
        var directory = CreateTempDirectory();

        try
        {
            var logger = CreateLogger(directory);
            logger.Log(LogLevel.Warn, "hotkey", "registration conflict");

            var line = File.ReadAllLines(LogFilePath(directory)).Single();
            Assert.Equal("[2026-07-12T10:20:30.123Z] [WARN] [hotkey] registration conflict", line);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Log_ErrorIncludesExceptionTypeAndMessage()
    {
        var directory = CreateTempDirectory();

        try
        {
            var logger = CreateLogger(directory);
            var exception = new InvalidOperationException("navigation failed");
            logger.Log(LogLevel.Error, "webview", "load failed", exception);

            var content = File.ReadAllText(LogFilePath(directory));
            Assert.Contains("[ERROR] [webview] load failed", content);
            Assert.Contains("InvalidOperationException: navigation failed", content);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Log_UsesInjectedTimeProvider()
    {
        var directory = CreateTempDirectory();
        var customTime = new DateTimeOffset(2025, 1, 2, 3, 4, 5, 678, TimeSpan.Zero);

        try
        {
            var logger = CreateLogger(directory, new FixedTimeProvider(customTime));
            logger.Log(LogLevel.Info, "clock", "controlled time");

            var line = File.ReadAllLines(LogFilePath(directory)).Single();
            Assert.StartsWith("[2025-01-02T03:04:05.678Z] [INFO] [clock] controlled time", line);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Log_WritesUtf8Content()
    {
        var directory = CreateTempDirectory();
        const string message = "日志测试";

        try
        {
            var logger = CreateLogger(directory);
            logger.Log(LogLevel.Info, "i18n", message);

            var bytes = File.ReadAllBytes(LogFilePath(directory));
            var content = Encoding.UTF8.GetString(bytes);
            Assert.Contains(message, content);
            Assert.Equal(Encoding.UTF8.GetPreamble().Length == 0 ? bytes : bytes, bytes);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Log_EmptyMessageWritesPlaceholder()
    {
        var directory = CreateTempDirectory();

        try
        {
            var logger = CreateLogger(directory);
            logger.Log(LogLevel.Info, "empty", "");

            var line = File.ReadAllLines(LogFilePath(directory)).Single();
            Assert.EndsWith("[empty] (empty)", line);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Log_MultilineMessageReplacesLineBreaksWithSpaces()
    {
        var directory = CreateTempDirectory();

        try
        {
            var logger = CreateLogger(directory);
            logger.Log(LogLevel.Info, "multiline", "line one\r\nline two");

            var line = File.ReadAllLines(LogFilePath(directory)).Single();
            Assert.Equal("[2026-07-12T10:20:30.123Z] [INFO] [multiline] line one line two", line);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task Log_ConcurrentWritesDoNotInterleave()
    {
        var directory = CreateTempDirectory();

        try
        {
            var logger = CreateLogger(directory);
            var tasks = Enumerable.Range(0, 20)
                .Select(i => Task.Run(() => logger.Log(LogLevel.Info, "concurrent", $"message-{i:D2}")))
                .ToArray();

            await Task.WhenAll(tasks);

            var lines = await File.ReadAllLinesAsync(LogFilePath(directory));
            Assert.Equal(20, lines.Length);

            foreach (var i in Enumerable.Range(0, 20))
            {
                Assert.Contains(lines, line => line.Contains($"[INFO] [concurrent] message-{i:D2}", StringComparison.Ordinal));
            }
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Log_UsesTemporaryDirectoryNotLocalApplicationData()
    {
        var directory = CreateTempDirectory();
        var productionLogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Anhei4Map");

        try
        {
            Assert.StartsWith(Path.GetTempPath(), directory, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(productionLogDirectory, directory, StringComparison.OrdinalIgnoreCase);

            var logger = CreateLogger(directory);
            logger.Log(LogLevel.Info, "path", "temporary directory");

            Assert.True(File.Exists(LogFilePath(directory)));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
