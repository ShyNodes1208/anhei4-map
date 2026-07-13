using System.Globalization;
using System.Reflection;
using System.Text;
using Anhei4Map.Core.Logging;
using Anhei4Map.Infrastructure.Logging;

namespace Anhei4Map.Tests;

public class FileLoggerTests
{
    private const long DefaultMaxFileSizeBytes = 10L * 1024 * 1024;
    private const int SmallThresholdBytes = 256;

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

    private static string RotatedLogPath(string directory, int index) =>
        index switch
        {
            1 => Path.Combine(directory, "app.1.log"),
            2 => Path.Combine(directory, "app.2.log"),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    private static FileLogger CreateLogger(
        string directory,
        TimeProvider? timeProvider = null,
        long maxFileSizeBytes = DefaultMaxFileSizeBytes) =>
        new(directory, timeProvider ?? new FixedTimeProvider(FixedUtcTime), maxFileSizeBytes);

    private static string FormatEntry(
        DateTimeOffset utcTime,
        LogLevel level,
        string source,
        string message,
        Exception? ex = null)
    {
        var timestamp = utcTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture) + "Z";
        var levelText = level switch
        {
            LogLevel.Info => "INFO",
            LogLevel.Warn => "WARN",
            LogLevel.Error => "ERROR",
            _ => "INFO"
        };

        var body = string.IsNullOrEmpty(message)
            ? "(empty)"
            : message
                .Replace("\r\n", " ", StringComparison.Ordinal)
                .Replace('\r', ' ')
                .Replace('\n', ' ');

        if (ex is not null)
        {
            body += $" {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
        }

        return $"[{timestamp}] [{levelText}] [{source}] {body}";
    }

    private static int EntryByteCount(string entry) =>
        Encoding.UTF8.GetByteCount(entry + Environment.NewLine);

    private static void SetLogFileSize(string directory, int byteCount)
    {
        var path = LogFilePath(directory);
        Directory.CreateDirectory(directory);
        if (byteCount == 0)
        {
            File.WriteAllText(path, string.Empty);
            return;
        }

        File.WriteAllBytes(path, new byte[byteCount]);
    }

    private static string BuildMessageForTargetEntryBytes(
        int targetEntryBytes,
        string source = "size",
        LogLevel level = LogLevel.Info,
        DateTimeOffset? utcTime = null)
    {
        utcTime ??= FixedUtcTime;
        var padding = string.Empty;

        while (EntryByteCount(FormatEntry(utcTime.Value, level, source, padding)) < targetEntryBytes)
        {
            padding += "x";
        }

        while (EntryByteCount(FormatEntry(utcTime.Value, level, source, padding)) > targetEntryBytes)
        {
            if (padding.Length == 0)
            {
                throw new InvalidOperationException($"Cannot build message for {targetEntryBytes} entry bytes.");
            }

            padding = padding[..^1];
        }

        if (EntryByteCount(FormatEntry(utcTime.Value, level, source, padding)) != targetEntryBytes)
        {
            throw new InvalidOperationException($"Failed to build message for {targetEntryBytes} entry bytes.");
        }

        return padding;
    }

    private static int CountLogFiles(string directory) =>
        Directory.GetFiles(directory, "app*.log").Length;

    private static int ActiveLogByteCount(string directory) =>
        (int)new FileInfo(LogFilePath(directory)).Length;

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
        const string message = "数据库连接成功";

        try
        {
            var logger = CreateLogger(directory);

            var expectedLine = $"[2026-07-12T10:20:30.123Z] [INFO] [i18n] {message}";
            var expectedText = expectedLine + Environment.NewLine;

            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(expectedText);
            var expectedBytes = preamble.Concat(contentBytes).ToArray();

            logger.Log(LogLevel.Info, "i18n", message);

            var actualBytes = File.ReadAllBytes(LogFilePath(directory));

            Assert.Equal(expectedBytes, actualBytes);

            Assert.True(
                actualBytes.Length >= 3 &&
                actualBytes[0] == 0xEF &&
                actualBytes[1] == 0xBB &&
                actualBytes[2] == 0xBF,
                "File must contain UTF-8 BOM (EF BB BF)");

            var decodedContent = Encoding.UTF8.GetString(actualBytes);
            Assert.Contains(message, decodedContent);
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

    [Fact]
    public void DefaultMaxFileSize_IsTenMegabytes()
    {
        var directory = CreateTempDirectory();

        try
        {
            var logger = CreateLogger(directory);
            var field = typeof(FileLogger).GetField("_maxFileSizeBytes", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(field);
            Assert.Equal(10L * 1024 * 1024, field.GetValue(logger));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Append_WhenProjectedSizeBelowLimit_DoesNotRotate()
    {
        var directory = CreateTempDirectory();
        const int threshold = SmallThresholdBytes;
        const int currentSize = 100;
        const int entryBytes = 100;

        try
        {
            SetLogFileSize(directory, currentSize);
            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            var message = BuildMessageForTargetEntryBytes(entryBytes);

            logger.Log(LogLevel.Info, "size", message);

            Assert.True(File.Exists(LogFilePath(directory)));
            Assert.False(File.Exists(RotatedLogPath(directory, 1)));
            Assert.Equal(currentSize + entryBytes, new FileInfo(LogFilePath(directory)).Length);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Append_WhenProjectedSizeEqualsLimit_DoesNotRotate()
    {
        var directory = CreateTempDirectory();
        const int threshold = SmallThresholdBytes;
        const int currentSize = 200;
        const int entryBytes = 56;

        try
        {
            SetLogFileSize(directory, currentSize);
            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            var message = BuildMessageForTargetEntryBytes(entryBytes);

            logger.Log(LogLevel.Info, "size", message);

            Assert.False(File.Exists(RotatedLogPath(directory, 1)));
            Assert.Equal(threshold, new FileInfo(LogFilePath(directory)).Length);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Append_WhenProjectedSizeExceedsLimit_RotatesBeforeWriting()
    {
        var directory = CreateTempDirectory();
        const int threshold = SmallThresholdBytes;
        const int currentSize = 200;
        const int entryBytes = 57;

        try
        {
            SetLogFileSize(directory, currentSize);
            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            var message = BuildMessageForTargetEntryBytes(entryBytes);

            logger.Log(LogLevel.Info, "size", message);

            Assert.True(File.Exists(RotatedLogPath(directory, 1)));
            Assert.Equal(currentSize, new FileInfo(RotatedLogPath(directory, 1)).Length);
            Assert.Contains(message, File.ReadAllText(LogFilePath(directory), Encoding.UTF8));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Rotation_PreservesOldContent()
    {
        var directory = CreateTempDirectory();
        const int threshold = SmallThresholdBytes;
        const int currentSize = 200;
        const int entryBytes = 57;

        try
        {
            var originalBytes = Enumerable.Range(0, currentSize).Select(i => (byte)(i % 256)).ToArray();
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(LogFilePath(directory), originalBytes);

            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            var message = BuildMessageForTargetEntryBytes(entryBytes);
            logger.Log(LogLevel.Info, "size", message);

            var rotatedBytes = File.ReadAllBytes(RotatedLogPath(directory, 1));
            Assert.Equal(originalBytes, rotatedBytes);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Rotation_NewActiveLogContainsOnlyNewEntry()
    {
        var directory = CreateTempDirectory();
        const int threshold = SmallThresholdBytes;
        const int currentSize = 200;
        const int entryBytes = 57;

        try
        {
            SetLogFileSize(directory, currentSize);
            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            var message = BuildMessageForTargetEntryBytes(entryBytes);
            logger.Log(LogLevel.Info, "size", message);

            var activeContent = File.ReadAllText(LogFilePath(directory), Encoding.UTF8);
            Assert.Contains(message, activeContent);
            Assert.DoesNotContain(new string('x', currentSize), activeContent);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Rotation_ShiftsExistingFilesWithoutOverwriting()
    {
        var directory = CreateTempDirectory();
        const int threshold = SmallThresholdBytes;
        const int currentSize = 200;
        const int entryBytes = 57;

        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(LogFilePath(directory), new string('a', currentSize));
            File.WriteAllText(RotatedLogPath(directory, 1), "first-history");
            File.WriteAllText(RotatedLogPath(directory, 2), "second-history");

            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            var message = BuildMessageForTargetEntryBytes(entryBytes);
            logger.Log(LogLevel.Info, "size", message);

            Assert.Equal("first-history", File.ReadAllText(RotatedLogPath(directory, 2)));
            Assert.Equal(new string('a', currentSize), File.ReadAllText(RotatedLogPath(directory, 1)));
            Assert.Contains(message, File.ReadAllText(LogFilePath(directory)));
            Assert.Equal(3, CountLogFiles(directory));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void EmptyFile_OversizedEntry_WritesWithoutRotatingEmptyFile()
    {
        var directory = CreateTempDirectory();
        const int threshold = SmallThresholdBytes;
        const int entryBytes = threshold + 50;

        try
        {
            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            var message = BuildMessageForTargetEntryBytes(entryBytes);
            logger.Log(LogLevel.Info, "size", message);

            Assert.False(File.Exists(RotatedLogPath(directory, 1)));
            Assert.True(ActiveLogByteCount(directory) > threshold);
            Assert.Contains(message, File.ReadAllText(LogFilePath(directory), Encoding.UTF8));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void ExistingContent_OversizedEntry_RotatesOnceThenWritesCompleteEntry()
    {
        var directory = CreateTempDirectory();
        const int threshold = SmallThresholdBytes;
        const int currentSize = 50;
        const int entryBytes = threshold + 50;

        try
        {
            SetLogFileSize(directory, currentSize);
            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            var message = BuildMessageForTargetEntryBytes(entryBytes);
            logger.Log(LogLevel.Info, "size", message);

            Assert.True(File.Exists(RotatedLogPath(directory, 1)));
            Assert.Equal(currentSize, new FileInfo(RotatedLogPath(directory, 1)).Length);
            Assert.True(ActiveLogByteCount(directory) > threshold);
            Assert.Contains(message, File.ReadAllText(LogFilePath(directory), Encoding.UTF8));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void WriteAfterOversizedEntry_RotatesOversizedActiveFileOnce()
    {
        var directory = CreateTempDirectory();
        const int threshold = SmallThresholdBytes;
        const int oversizedEntryBytes = threshold + 50;
        const int followUpEntryBytes = 80;

        try
        {
            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            var oversizedMessage = BuildMessageForTargetEntryBytes(oversizedEntryBytes);
            logger.Log(LogLevel.Info, "size", oversizedMessage);

            Assert.False(File.Exists(RotatedLogPath(directory, 1)));
            Assert.True(new FileInfo(LogFilePath(directory)).Length > threshold);

            var followUpMessage = BuildMessageForTargetEntryBytes(followUpEntryBytes, source: "next");
            logger.Log(LogLevel.Info, "next", followUpMessage);

            Assert.True(File.Exists(RotatedLogPath(directory, 1)));
            Assert.Contains(oversizedMessage, File.ReadAllText(RotatedLogPath(directory, 1), Encoding.UTF8));
            Assert.Contains(followUpMessage, File.ReadAllText(LogFilePath(directory), Encoding.UTF8));
            Assert.True(ActiveLogByteCount(directory) > 0);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Rotation_UsesUtf8ByteCount_NotCharacterCount()
    {
        var directory = CreateTempDirectory();
        const int threshold = 200;
        const string chineseMessage = "日志日志日志日志";
        const string source = "utf8";

        try
        {
            var entryBytes = EntryByteCount(FormatEntry(FixedUtcTime, LogLevel.Info, source, chineseMessage));
            var currentSize = threshold - entryBytes + 1;
            SetLogFileSize(directory, currentSize);

            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            logger.Log(LogLevel.Info, source, chineseMessage);

            Assert.True(File.Exists(RotatedLogPath(directory, 1)));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public async Task ConcurrentWritesNearLimit_DoNotLoseEntriesOrDeadlock()
    {
        var directory = CreateTempDirectory();
        const int threshold = 512;
        const int writeCount = 20;

        try
        {
            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            var tasks = Enumerable.Range(0, writeCount)
                .Select(i => Task.Run(() => logger.Log(LogLevel.Info, "concurrent", $"near-limit-{i:D2}")))
                .ToArray();

            await Task.WhenAll(tasks);

            var lines = await File.ReadAllLinesAsync(LogFilePath(directory));
            var allContent = await File.ReadAllTextAsync(LogFilePath(directory));
            var rotatedContent = File.Exists(RotatedLogPath(directory, 1))
                ? await File.ReadAllTextAsync(RotatedLogPath(directory, 1))
                : string.Empty;
            var secondRotatedContent = File.Exists(RotatedLogPath(directory, 2))
                ? await File.ReadAllTextAsync(RotatedLogPath(directory, 2))
                : string.Empty;

            var combined = allContent + rotatedContent + secondRotatedContent;
            Assert.Equal(writeCount, combined.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);

            foreach (var i in Enumerable.Range(0, writeCount))
            {
                Assert.Contains($"near-limit-{i:D2}", combined, StringComparison.Ordinal);
            }

            Assert.InRange(CountLogFiles(directory), 1, 3);
            Assert.InRange(lines.Length, 1, writeCount);
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void RotationFailure_PropagatesException()
    {
        var directory = CreateTempDirectory();
        const int threshold = SmallThresholdBytes;
        const int currentSize = 200;
        const int entryBytes = 57;

        try
        {
            SetLogFileSize(directory, currentSize);
            File.WriteAllText(RotatedLogPath(directory, 1), "history-one");
            File.WriteAllText(RotatedLogPath(directory, 2), "history-two");

            using var lockStream = new FileStream(
                RotatedLogPath(directory, 2),
                FileMode.Open,
                FileAccess.Read,
                FileShare.None);

            var logger = CreateLogger(directory, maxFileSizeBytes: threshold);
            var message = BuildMessageForTargetEntryBytes(entryBytes);

            Assert.ThrowsAny<IOException>(() => logger.Log(LogLevel.Info, "size", message));
        }
        finally
        {
            CleanupTempDirectory(directory);
        }
    }

    [Fact]
    public void Constructor_ThrowsOnNonPositiveMaxFileSize()
    {
        var directory = CreateTempDirectory();

        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateLogger(directory, maxFileSizeBytes: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateLogger(directory, maxFileSizeBytes: -1));
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
