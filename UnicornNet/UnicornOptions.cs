namespace UnicornNet;

/// <summary>
///     Defines how exceptions thrown by user-provided hook callbacks should be handled.
/// </summary>
public enum CallbackExceptionHandling
{
    /// <summary>
    ///     Throw the exception immediately, stopping emulation.
    ///     This is the most strict mode and helps catch bugs early.
    /// </summary>
    Throw,

    /// <summary>
    ///     Log the exception using the configured logger and then re-throw it.
    ///     Emulation will stop, but the error will be logged first.
    ///     Requires a logger to be configured.
    /// </summary>
    LogAndThrow,

    /// <summary>
    ///     Log the exception using the configured logger and continue emulation.
    ///     The hook will return a safe default value.
    ///     Requires a logger to be configured.
    ///     Use with caution as this may hide bugs.
    /// </summary>
    LogAndContinue
}

/// <summary>
///     Configuration options for <see cref="Unicorn" /> engine behavior,
///     particularly around error handling and diagnostics.
/// </summary>
public sealed class UnicornOptions
{
    /// <summary>
    ///     Creates a new instance with default settings.
    /// </summary>
    public UnicornOptions()
    {
    }

    /// <summary>
    ///     Creates a new instance with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use for diagnostic messages.</param>
    public UnicornOptions(IUnicornLogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    ///     Gets the default options instance.
    /// </summary>
    public static UnicornOptions Default { get; } = new();

    /// <summary>
    ///     Gets or sets the logger for diagnostic messages.
    ///     When null, diagnostic logging is disabled.
    /// </summary>
    public IUnicornLogger? Logger { get; set; }

    /// <summary>
    ///     Gets or sets how exceptions thrown by user-provided hook callbacks are handled.
    ///     Default is <see cref="CallbackExceptionHandling.Throw" />.
    /// </summary>
    /// <remarks>
    ///     When set to <see cref="CallbackExceptionHandling.LogAndThrow" /> or
    ///     <see cref="CallbackExceptionHandling.LogAndContinue" />, a <see cref="Logger" /> must be configured.
    ///     Otherwise, the behavior will fall back to <see cref="CallbackExceptionHandling.Throw" />.
    /// </remarks>
    public CallbackExceptionHandling CallbackExceptionHandling { get; set; } = CallbackExceptionHandling.Throw;

    /// <summary>
    ///     Gets or sets whether to enable verbose diagnostic logging.
    ///     When true and a <see cref="Logger" /> is configured, additional debug information
    ///     will be logged about hook invocations, disposal, and internal state.
    ///     Default is false.
    /// </summary>
    public bool EnableVerboseDiagnostics { get; set; }
}