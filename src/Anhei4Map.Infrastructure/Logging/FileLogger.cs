using System.Globalization;
using System.Text;
using Anhei4Map.Core.Logging;

namespace Anhei4Map.Infrastructure.Logging;

public sealed class FileLogger : IAppLogger
{
    private readonly string _logFilePath;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public FileLogger(string directory, TimeProvider? timeProvider = null)
    {
        _logFilePath = Path.Combine(directory, "app.log");
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public void Log(LogLevel level, string source, string message, Exception? ex = null)
    {
        var entry = FormatEntry(level, source, message, ex);

        _writeLock.Wait();
        try
        {
            var directory = Path.GetDirectoryName(_logFilePath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(_logFilePath, entry + Environment.NewLine, Encoding.UTF8);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private string FormatEntry(LogLevel level, string source, string message, Exception? ex)
    {
        var timestamp = _timeProvider.GetUtcNow()
            .ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture) + "Z";
        var levelText = level switch
        {
            LogLevel.Info => "INFO",
            LogLevel.Warn => "WARN",
            LogLevel.Error => "ERROR",
            _ => "INFO"
        };

        var body = SanitizeMessage(message);
        if (ex is not null)
        {
            body += $" {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
        }

        return $"[{timestamp}] [{levelText}] [{source}] {body}";
    }

    private static string SanitizeMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return "(empty)";
        }

        return message
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ');
    }
}
