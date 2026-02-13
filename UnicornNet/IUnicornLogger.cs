using System;

namespace UnicornNet;

/// <summary>
///     Defines the severity level for diagnostic log messages.
/// </summary>
public enum UnicornLogLevel
{
    /// <summary>
    ///     Verbose debugging information.
    /// </summary>
    Debug = 0,

    /// <summary>
    ///     Informational messages about normal operations.
    /// </summary>
    Info = 1,

    /// <summary>
    ///     Warning messages about potential issues that don't prevent operation.
    /// </summary>
    Warning = 2,

    /// <summary>
    ///     Error messages about failures that impact functionality.
    /// </summary>
    Error = 3
}

/// <summary>
///     Interface for diagnostic logging in Unicorn.NET.
///     Implement this interface to receive diagnostic information about internal operations,
///     hook callback failures, and other events that might otherwise be silent.
/// </summary>
public interface IUnicornLogger
{
    /// <summary>
    ///     Logs a diagnostic message at the specified level.
    /// </summary>
    /// <param name="level">The severity level of the message.</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">Optional exception associated with this log entry.</param>
    void Log(UnicornLogLevel level, string message, Exception? exception = null);
}

/// <summary>
///     Extension methods for convenient logging.
/// </summary>
public static class UnicornLoggerExtensions
{
    /// <summary>
    ///     Logs a debug-level message.
    /// </summary>
    public static void LogDebug(this IUnicornLogger logger, string message)
    {
        logger.Log(UnicornLogLevel.Debug, message);
    }

    /// <summary>
    ///     Logs an info-level message.
    /// </summary>
    public static void LogInfo(this IUnicornLogger logger, string message)
    {
        logger.Log(UnicornLogLevel.Info, message);
    }

    /// <summary>
    ///     Logs a warning-level message.
    /// </summary>
    public static void LogWarning(this IUnicornLogger logger, string message, Exception? exception = null)
    {
        logger.Log(UnicornLogLevel.Warning, message, exception);
    }

    /// <summary>
    ///     Logs an error-level message.
    /// </summary>
    public static void LogError(this IUnicornLogger logger, string message, Exception? exception = null)
    {
        logger.Log(UnicornLogLevel.Error, message, exception);
    }
}