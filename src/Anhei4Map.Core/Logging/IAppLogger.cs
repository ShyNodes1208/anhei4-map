namespace Anhei4Map.Core.Logging;

public enum LogLevel
{
    Info,
    Warn,
    Error
}

public interface IAppLogger
{
    void Log(LogLevel level, string source, string message, Exception? ex = null);
}
