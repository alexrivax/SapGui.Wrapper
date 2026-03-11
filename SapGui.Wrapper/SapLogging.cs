namespace SapGui.Wrapper;

/// <summary>
/// Severity levels for <see cref="SapLogAction"/> delegates.
/// These mirror <see cref="Microsoft.Extensions.Logging.LogLevel"/> so that
/// custom delegates can map them to any logging framework without importing
/// <c>Microsoft.Extensions.Logging</c> in the consuming project.
/// </summary>
public enum SapLogLevel
{
    /// <summary>Detailed diagnostic information, e.g. every <c>FindById</c> call.</summary>
    Debug,
    /// <summary>Normal operational events, e.g. session open/close and transaction changes.</summary>
    Information,
    /// <summary>Something unexpected but recoverable, e.g. popup detected or retry attempt.</summary>
    Warning,
    /// <summary>An error has occurred and will propagate to the caller.</summary>
    Error,
}

/// <summary>
/// A lightweight logging delegate for integrating SapGui.Wrapper log output into
/// any logging framework, without requiring a dependency on
/// <c>Microsoft.Extensions.Logging</c> in the consuming project.
///
/// <para>
/// Pass an instance to <see cref="SapGuiClient.Attach(SapLogAction?, SapLogLevel, ILogger?)"/> or
/// <see cref="SapGuiClient.LaunchWithSso(string, bool, int, SapLogAction?, SapLogLevel, ILogger?)"/>.
/// </para>
///
/// <para>Examples:</para>
/// <code>
/// // Console output
/// SapGuiClient.Attach(logAction: (level, msg, ex) =>
///     Console.WriteLine($"[{level}] {msg}{(ex is null ? "" : " – " + ex.Message)}"));
///
/// // Serilog static Log class (no ILogger needed)
/// SapGuiClient.Attach(logAction: (level, msg, ex) =>
///     Serilog.Log.Write(ToSerilogLevel(level), ex, "{Message}", msg));
///
/// // UiPath Invoke Code activity (Log() is the built-in UiPath logging action)
/// SapGuiClient.Attach(logAction: (level, msg, _) => Log($"[SAP] {msg}"));
/// </code>
/// </summary>
/// <param name="level">Severity level of the message.</param>
/// <param name="message">Human-readable log message.</param>
/// <param name="exception">Associated exception, or <see langword="null"/>.</param>
public delegate void SapLogAction(SapLogLevel level, string message, Exception? exception);

/// <summary>
/// Internal logging bridge. Dispatches to an <see cref="ILogger"/>, a
/// <see cref="SapLogAction"/> delegate, or silently discards all messages when
/// neither is configured — zero overhead in the silent case.
/// </summary>
internal sealed class SapLogger
{
    private static readonly SapLogger _null = new();

    /// <summary>A no-op logger that silently discards all messages.</summary>
    internal static SapLogger Null => _null;

    private readonly ILogger? _iLogger;
    private readonly SapLogAction? _action;
    private readonly SapLogLevel _minLevel;

    private SapLogger() { _minLevel = SapLogLevel.Debug; }

    /// <summary>Wraps an <see cref="ILogger"/>. Min-level is controlled by the ILogger configuration.</summary>
    internal SapLogger(ILogger logger)
    {
        _iLogger  = logger;
        _minLevel = SapLogLevel.Debug;
    }

    /// <summary>Wraps a <see cref="SapLogAction"/> delegate with an optional minimum level filter.</summary>
    internal SapLogger(SapLogAction action, SapLogLevel minLevel = SapLogLevel.Debug)
    {
        _action   = action;
        _minLevel = minLevel;
    }

    internal void Log(SapLogLevel level, string message, Exception? ex = null)
    {
        if (level < _minLevel) return;
        if (_iLogger is not null)
            _iLogger.Log(ToMelLevel(level), ex, "{SapMessage}", message);
        else
            _action?.Invoke(level, message, ex);
    }

    internal void Debug(string msg)                       => Log(SapLogLevel.Debug,       msg);
    internal void Info(string msg)                        => Log(SapLogLevel.Information,  msg);
    internal void Warn(string msg, Exception? ex = null)  => Log(SapLogLevel.Warning,      msg, ex);
    internal void Error(string msg, Exception? ex = null) => Log(SapLogLevel.Error,        msg, ex);

    private static LogLevel ToMelLevel(SapLogLevel level) => level switch
    {
        SapLogLevel.Debug       => LogLevel.Debug,
        SapLogLevel.Information => LogLevel.Information,
        SapLogLevel.Warning     => LogLevel.Warning,
        SapLogLevel.Error       => LogLevel.Error,
        _                       => LogLevel.Information,
    };
}
