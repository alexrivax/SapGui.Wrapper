namespace SapGui.Wrapper;

/// <summary>
/// Configures and executes retry behaviour for SAP GUI operations.
/// <para>
/// Obtain an instance via <see cref="GuiSession.WithRetry"/>:
/// </para>
/// <code>
/// session.WithRetry(maxAttempts: 3, delayMs: 500).Run(() =>
/// {
///     session.WaitReady();
///     session.TextField("wnd[0]/usr/txtFoo").Text = "value";
/// });
/// </code>
/// <para>
/// Retries on <see cref="SapComponentNotFoundException"/> (slow screen loads) and
/// <see cref="TimeoutException"/> (session still busy). Never retries on
/// <see cref="SapGuiNotFoundException"/> — that is a fatal setup error.
/// </para>
/// </summary>
public sealed class RetryPolicy
{
    /// <summary>Maximum number of execution attempts (≥ 1). Default: 3.</summary>
    public int MaxAttempts { get; }

    /// <summary>Delay between attempts in milliseconds (≥ 0). Default: 500.</summary>
    public int DelayMs { get; }

    private readonly SapLogger _logger;

    /// <summary>Creates a retry policy with the given parameters.</summary>
    /// <param name="maxAttempts">Maximum number of attempts. Must be ≥ 1.</param>
    /// <param name="delayMs">Milliseconds to wait between attempts. Must be ≥ 0.</param>
    public RetryPolicy(int maxAttempts = 3, int delayMs = 500)
        : this(maxAttempts, delayMs, SapLogger.Null) { }

    /// <summary>Internal constructor used by <see cref="GuiSession.WithRetry"/> to propagate the session logger.</summary>
    internal RetryPolicy(int maxAttempts, int delayMs, SapLogger logger)
    {
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Must be at least 1.");
        if (delayMs < 0)     throw new ArgumentOutOfRangeException(nameof(delayMs),     "Must be non-negative.");
        MaxAttempts = maxAttempts;
        DelayMs     = delayMs;
        _logger     = logger;
    }

    /// <summary>
    /// Executes <paramref name="action"/> up to <see cref="MaxAttempts"/> times,
    /// sleeping <see cref="DelayMs"/> ms between attempts.
    /// </summary>
    /// <param name="action">The operation to execute and potentially retry.</param>
    /// <exception cref="SapGuiNotFoundException">Always rethrown immediately — fatal setup error.</exception>
    /// <exception cref="SapComponentNotFoundException">Thrown after all attempts are exhausted.</exception>
    /// <exception cref="TimeoutException">Thrown after all attempts are exhausted.</exception>
    public void Run(Action action)
    {
        Exception? last = null;
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (SapGuiNotFoundException) { throw; }
            catch (Exception ex) when (ex is SapComponentNotFoundException or TimeoutException)
            {
                last = ex;
                if (attempt < MaxAttempts)
                {
                    _logger.Warn($"Retry attempt {attempt}/{MaxAttempts} failed ({ex.GetType().Name}: {ex.Message}). Retrying in {DelayMs} ms.");
                    System.Threading.Thread.Sleep(DelayMs);
                }
            }
        }
        throw last!;
    }

    /// <summary>
    /// Executes <paramref name="func"/> up to <see cref="MaxAttempts"/> times and
    /// returns its result.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="func">The operation to execute and potentially retry.</param>
    /// <returns>The value returned by the first successful execution.</returns>
    /// <exception cref="SapGuiNotFoundException">Always rethrown immediately — fatal setup error.</exception>
    /// <exception cref="SapComponentNotFoundException">Thrown after all attempts are exhausted.</exception>
    /// <exception cref="TimeoutException">Thrown after all attempts are exhausted.</exception>
    public T Run<T>(Func<T> func)
    {
        Exception? last = null;
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return func();
            }
            catch (SapGuiNotFoundException) { throw; }
            catch (Exception ex) when (ex is SapComponentNotFoundException or TimeoutException)
            {
                last = ex;
                if (attempt < MaxAttempts)
                {
                    _logger.Warn($"Retry attempt {attempt}/{MaxAttempts} failed ({ex.GetType().Name}: {ex.Message}). Retrying in {DelayMs} ms.");
                    System.Threading.Thread.Sleep(DelayMs);
                }
            }
        }
        throw last!;
    }
}
