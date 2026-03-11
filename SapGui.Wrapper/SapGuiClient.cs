namespace SapGui.Wrapper;

/// <summary>
/// Top-level entry point for SAP GUI automation.
///
/// <para>Typical usage (C#):</para>
/// <code>
/// using var sap = SapGuiClient.Attach();
/// var session = sap.Session;
/// session.StartTransaction("SE16");
/// session.TextField("wnd[0]/usr/ctxtDATABROWSE-TABLENAME").Text = "MARA";
/// session.PressEnter();
/// var status = session.Statusbar();
/// Console.WriteLine(status.Text);
/// </code>
///
/// <para>Typical usage (VB.NET for UiPath Invoke Code activity):</para>
/// <code>
/// Dim sap As SapGuiClient = SapGuiClient.Attach()
/// Dim session As GuiSession = sap.Session
/// session.StartTransaction("MM60")
/// session.TextField("wnd[0]/usr/txtS_WERKS-LOW").Text = "1000"
/// session.PressEnter()
/// </code>
/// </summary>
public sealed class SapGuiClient : IDisposable
{
    private bool _disposed;
    private readonly SapLogger _logger;

    /// <summary>The underlying <see cref="GuiApplication"/> instance.</summary>
    public GuiApplication Application { get; }

    /// <summary>
    /// The first available session. For multi-session scenarios
    /// use <see cref="GetSession"/>.
    /// </summary>
    public GuiSession Session
    {
        get
        {
            var s = Application.GetFirstSession();
            s.Logger = _logger;
            return s;
        }
    }

    private SapGuiClient(GuiApplication app, SapLogger logger)
    {
        Application = app;
        _logger = logger;
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Attaches to the currently running SAP GUI application.
    /// Requires SAP GUI to be open and scripting to be enabled.
    /// </summary>
    /// <param name="logAction">
    ///   Optional delegate that receives log messages.
    ///   Use <c>(level, msg, ex) => Log($"[SAP/{level}] {msg}")</c> in UiPath, or map to any logging framework.
    ///   Leave <see langword="null"/> (default) for silent operation.
    /// </param>
    /// <param name="minLevel">
    ///   Minimum severity forwarded to <paramref name="logAction"/>.
    ///   Defaults to <see cref="SapLogLevel.Debug"/> (all messages).
    ///   Ignored when <paramref name="logger"/> is used — <see cref="ILogger"/> uses its own configured minimum level.
    /// </param>
    /// <param name="logger">
    ///   Optional <see cref="ILogger"/> (ASP.NET Core DI, Serilog, NLog via MEL adapter).
    ///   When both <paramref name="logAction"/> and <paramref name="logger"/> are supplied,
    ///   <paramref name="logAction"/> takes precedence.
    /// </param>
    /// <exception cref="SapGuiNotFoundException">
    ///   SAP GUI is not running or scripting is disabled.
    /// </exception>
    public static SapGuiClient Attach(
        SapLogAction? logAction = null,
        SapLogLevel minLevel = SapLogLevel.Debug,
        ILogger? logger = null)
        => AttachCore(BuildLogger(logAction, minLevel, logger));

    private static SapLogger BuildLogger(SapLogAction? logAction, SapLogLevel minLevel, ILogger? logger)
    {
        if (logAction is not null) return new SapLogger(logAction, minLevel);
        if (logger   is not null) return new SapLogger(logger);
        return SapLogger.Null;
    }

    private static SapGuiClient AttachCore(SapLogger logger)
    {
        GuiApplication app;
        try
        {
            app = GuiApplication.Attach();
        }
        catch (Exception ex)
        {
            logger.Error($"Attach failed: {ex.Message}", ex);
            throw;
        }
        var client = new SapGuiClient(app, logger);
        logger.Info($"SAP GUI attached (version {app.Version}).");
        return client;
    }

    /// <summary>
    /// Ensures SAP Logon is running, opens a connection to
    /// <paramref name="systemDescription"/> via SSO (no credential dialog),
    /// waits until a usable session is available, and returns the client.
    ///
    /// <para>
    /// This method is designed for Single Sign-On (SNC / Kerberos / etc.) environments.
    /// The target system entry must be configured in SAP Logon Pad and the system
    /// must allow SSO so that the connection completes without prompting for credentials.
    /// </para>
    ///
    /// <para>
    /// If a session to the same system is already open, SAP Logon shows a
    /// "License information for multiple logons" dialog that blocks the new
    /// connection from completing.  Use <paramref name="reuseExistingSession"/>
    /// to control this behaviour:
    /// <list type="bullet">
    ///   <item><description>
    ///     <see langword="true"/> — skip opening a new connection and return a
    ///     client wrapping the first existing session for that system.
    ///   </description></item>
    ///   <item><description>
    ///     <see langword="false"/> (default) — throw <see cref="InvalidOperationException"/>
    ///     if an existing session for that system is detected, so the caller can
    ///     decide what to do (attach, close, or abort).
    ///   </description></item>
    /// </list>
    /// </para>
    ///
    /// <code>
    /// // Reuse any already-open session for the system:
    /// using var sap = SapGuiClient.LaunchWithSso("PRD - Production", reuseExistingSession: true);
    /// var session = sap.Session;
    /// session.DismissPostLoginPopups();
    /// session.StartTransaction("MM60");
    /// </code>
    /// </summary>
    /// <param name="systemDescription">
    ///   System entry name exactly as it appears in SAP Logon Pad,
    ///   e.g. <c>"PRD - Production"</c>.
    /// </param>
    /// <param name="reuseExistingSession">
    ///   When <see langword="true"/>, an existing open session for
    ///   <paramref name="systemDescription"/> is reused instead of opening a new
    ///   connection. When <see langword="false"/> (default), an existing session
    ///   causes an <see cref="InvalidOperationException"/> to be thrown.
    /// </param>
    /// <param name="connectionTimeoutMs">
    ///   Total time (ms) to wait for SAP Logon to start and a ready session to appear.
    ///   Defaults to 30 seconds.
    /// </param>
    /// <param name="logAction">
    ///   Optional delegate for log messages (see <see cref="Attach(SapLogAction?, SapLogLevel, ILogger?)"/>).
    /// </param>
    /// <param name="minLevel">
    ///   Minimum severity forwarded to <paramref name="logAction"/>.
    ///   Default: <see cref="SapLogLevel.Debug"/> (all messages).
    /// </param>
    /// <param name="logger">
    ///   Optional <see cref="ILogger"/>. Ignored when <paramref name="logAction"/> is also supplied.
    /// </param>
    /// <exception cref="SapGuiNotFoundException">
    ///   <c>saplogon.exe</c> could not be started, or SAP GUI scripting is disabled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///   An existing session for <paramref name="systemDescription"/> is already open
    ///   and <paramref name="reuseExistingSession"/> is <see langword="false"/>, or
    ///   the connection could not be opened.
    /// </exception>
    /// <exception cref="TimeoutException">
    ///   SAP Logon started but no ready session appeared within
    ///   <paramref name="connectionTimeoutMs"/> ms.
    /// </exception>
    public static SapGuiClient LaunchWithSso(
        string systemDescription,
        bool reuseExistingSession = false,
        int connectionTimeoutMs = 30_000,
        SapLogAction? logAction = null,
        SapLogLevel minLevel = SapLogLevel.Debug,
        ILogger? logger = null) =>
        LaunchWithSsoCore(systemDescription, reuseExistingSession, connectionTimeoutMs, BuildLogger(logAction, minLevel, logger));

    private static SapGuiClient LaunchWithSsoCore(
        string systemDescription,
        bool reuseExistingSession,
        int connectionTimeoutMs,
        SapLogger logger)
    {
        // ── 1. Ensure saplogon.exe is running ─────────────────────────────────
        if (!IsProcessRunning("saplogon"))
        {
            logger.Info("LaunchWithSso: saplogon.exe not running — attempting to start it.");
            try
            {
                Process.Start(new ProcessStartInfo("saplogon.exe") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                var error = new SapGuiNotFoundException(
                    "Could not start saplogon.exe. " +
                    "Ensure SAP GUI is installed and saplogon.exe is on the PATH.", ex);
                logger.Error(error.Message, error);
                throw error;
            }
        }
        else
        {
            logger.Debug("LaunchWithSso: saplogon.exe is already running.");
        }

        // ── 2. Wait for SAP GUI to register in the Windows ROT ────────────────
        var deadline = DateTime.UtcNow.AddMilliseconds(connectionTimeoutMs);
        GuiApplication? app = null;

        while (DateTime.UtcNow < deadline)
        {
            try { app = GuiApplication.Attach(); break; }
            catch (SapGuiNotFoundException) { Thread.Sleep(500); }
        }

        if (app is null)
        {
            var error = new SapGuiNotFoundException(
                "SAP GUI started but did not register with the scripting engine within the " +
                $"timeout ({connectionTimeoutMs} ms). " +
                "Enable scripting via SAP GUI Options → Accessibility & Scripting → Scripting.");
            logger.Error(error.Message, error);
            throw error;
        }

        logger.Info($"LaunchWithSso: SAP GUI scripting attached (version {app.Version}).");
        var client = new SapGuiClient(app, logger);

        // ── 3. Check for an existing session for this system ──────────────────
        try
        {
            var existingConnection = FindConnectionByDescription(app, systemDescription);
            if (existingConnection is not null)
            {
                if (reuseExistingSession)
                {
                    logger.Info($"LaunchWithSso: reusing existing session for '{systemDescription}'.");
                    return client;
                }

                client.Dispose();
                var error = new InvalidOperationException(
                    $"A session for '{systemDescription}' is already open. " +
                    $"Pass reuseExistingSession: true to reuse it, or close the existing SAP window first.");
                logger.Error(error.Message, error);
                throw error;
            }
        }
        catch (InvalidOperationException) { throw; }
        catch { /* could not enumerate connections – proceed to open a new one */ }

        // ── 4. Open the SSO connection ────────────────────────────────────────
        GuiConnection connection;
        try
        {
            // sync: true blocks until the logon screen / session is ready.
            // For SSO systems the connection completes without a credential dialog.
            connection = app.OpenConnection(systemDescription, sync: true);
        }
        catch (Exception ex)
        {
            client.Dispose();
            var error = new InvalidOperationException(
                $"Failed to open SSO connection to '{systemDescription}'. " +
                "Verify the entry name in SAP Logon Pad and that SSO is configured.", ex);
            logger.Error(error.Message, error);
            throw error;
        }

        logger.Info($"LaunchWithSso: connection to '{systemDescription}' opened — waiting for session.");

        // ── 5. Wait until a non-busy session is available ─────────────────────
        var sessionDeadline = DateTime.UtcNow.AddMilliseconds(connectionTimeoutMs);
        while (DateTime.UtcNow < sessionDeadline)
        {
            try
            {
                var sessions = connection.GetSessions();
                if (sessions.Count > 0 && !sessions[0].IsBusy)
                {
                    logger.Info($"LaunchWithSso: session ready for '{systemDescription}'.");
                    return client;
                }
            }
            catch { /* session list not yet populated - keep polling */ }

            Thread.Sleep(300);
        }

        client.Dispose();
        var timeoutError = new TimeoutException(
            $"SAP opened a connection to '{systemDescription}' but no ready session appeared " +
            $"within {connectionTimeoutMs} ms.");
        logger.Error(timeoutError.Message, timeoutError);
        throw timeoutError;
    }

    /// <summary>
    /// Returns the first <see cref="GuiConnection"/> whose description contains
    /// <paramref name="systemDescription"/> (case-insensitive), or
    /// <see langword="null"/> if no matching connection is open.
    /// </summary>
    private static GuiConnection? FindConnectionByDescription(GuiApplication app, string systemDescription)
    {
        foreach (var conn in app.GetConnections())
        {
            if (conn.Host.IndexOf(systemDescription, StringComparison.OrdinalIgnoreCase) >= 0 ||
                systemDescription.IndexOf(conn.Host, StringComparison.OrdinalIgnoreCase) >= 0)
                return conn;
        }
        return null;
    }

    // ── Multi-connection / multi-session helpers ───────────────────────────────

    /// <summary>All active connections.</summary>
    public IReadOnlyList<GuiConnection> GetConnections() =>
        Application.GetConnections();

    /// <summary>
    /// Gets a session by connection index and session index (both 0-based).
    /// </summary>
    public GuiSession GetSession(int connectionIndex = 0, int sessionIndex = 0)
    {
        var s = Application.GetConnections()[connectionIndex].GetSession(sessionIndex);
        s.Logger = _logger;
        return s;
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    /// <summary>
    /// Releases the managed wrapper and the underlying COM reference to the
    /// <see cref="GuiApplication"/> object.
    ///
    /// <para>
    /// <b>Does NOT close SAP GUI or log off any sessions.</b>
    /// Sessions obtained before <c>Dispose</c> remain valid COM objects inside
    /// SAP GUI and can continue to be used.  If you also want to release a
    /// <see cref="GuiSession"/> reference, call <see cref="GuiSession.Dispose"/>
    /// on it explicitly (or use a <c>using</c> block).
    /// </para>
    ///
    /// <para>
    /// Safe to call multiple times.
    /// </para>
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Explicitly release the COM RCW for GuiApplication so the reference
        // is returned to COM immediately rather than waiting for the GC finalizer.
        // This prevents COM objects from outliving their intended scope and
        // eliminates the risk of leaving stale COM references in the process.
        try
        {
            if (Marshal.IsComObject(Application.RawObject))
                Marshal.ReleaseComObject(Application.RawObject);
        }
        catch
        {
            // Best-effort: ignore COM errors during teardown (e.g. SAP was
            // already closed externally before Dispose was called).
        }
    }

    // ── Health check ──────────────────────────────────────────────────────────

    /// <summary>
    /// Runs a set of pre-flight checks and returns a structured
    /// <see cref="HealthCheckResult"/>. <b>Never throws.</b>
    ///
    /// <para>Checks performed (in order):</para>
    /// <list type="number">
    ///   <item><description>SAP GUI process (<c>saplogon.exe</c>) is running.</description></item>
    ///   <item><description>Scripting API is accessible via the Windows ROT.</description></item>
    ///   <item><description>At least one active server connection exists.</description></item>
    ///   <item><description>At least one active session exists.</description></item>
    ///   <item><description>Session info (user, system, client) is readable — confirms the session is fully logged in.</description></item>
    /// </list>
    ///
    /// <para>Each finding is prefixed with <c>OK:</c>, <c>WARN:</c>, or <c>FAIL:</c>.</para>
    ///
    /// <code>
    /// var result = SapGuiClient.HealthCheck();
    /// if (!result.IsHealthy)
    ///     throw new InvalidOperationException(result.FailureSummary);
    ///
    /// foreach (var line in result.Findings)
    ///     Console.WriteLine(line);
    /// </code>
    /// </summary>
    public static HealthCheckResult HealthCheck()
    {
        var findings = new List<string>();

        // ── 1. SAP GUI process ────────────────────────────────────────────────
        if (!IsProcessRunning("saplogon"))
        {
            findings.Add("FAIL: saplogon.exe is not running. Start SAP Logon and log on before running automation.");
            return new HealthCheckResult(false, findings);
        }
        findings.Add("OK: saplogon.exe is running.");

        // ── 2. Scripting API / ROT access ─────────────────────────────────────
        // Obtain a temporary COM reference; release it in the finally block.
        GuiApplication? app = null;
        try
        {
            app = GuiApplication.Attach();
            findings.Add($"OK: SAP GUI scripting is enabled (version {app.Version}).");
        }
        catch (SapGuiNotFoundException ex)
        {
            findings.Add($"FAIL: Cannot attach to SAP GUI – {ex.Message} " +
                         "Enable scripting via SAP GUI Options → Accessibility & Scripting → Scripting.");
            return new HealthCheckResult(false, findings);
        }
        catch (Exception ex)
        {
            findings.Add($"FAIL: Unexpected error attaching to SAP GUI – {ex.Message}");
            return new HealthCheckResult(false, findings);
        }

        try
        {
            // ── 3. Active connections ─────────────────────────────────────────
            IReadOnlyList<GuiConnection> connections;
            try
            {
                connections = app.GetConnections();
            }
            catch (Exception ex)
            {
                findings.Add($"FAIL: Could not read connections – {ex.Message}");
                return new HealthCheckResult(false, findings);
            }

            if (connections.Count == 0)
            {
                findings.Add("FAIL: No active SAP GUI connections. Log on to a system first.");
                return new HealthCheckResult(false, findings);
            }
            findings.Add($"OK: {connections.Count} connection(s) found.");

            // ── 4 + 5. Active session + session info ──────────────────────────
            bool sessionFound = false;
            foreach (var conn in connections)
            {
                IReadOnlyList<GuiSession> sessions;
                try
                {
                    sessions = conn.GetSessions();
                }
                catch (Exception ex)
                {
                    findings.Add($"WARN: Could not enumerate sessions on '{conn.Host}' – {ex.Message}");
                    continue;
                }

                if (sessions.Count == 0)
                {
                    findings.Add($"WARN: Connection '{conn.Host}' has no open sessions.");
                    continue;
                }

                findings.Add($"OK: {sessions.Count} session(s) found on '{conn.Host}'.");
                sessionFound = true;

                // Read session info from the first session to confirm it is logged in.
                try
                {
                    var info = sessions[0].Info;
                    findings.Add($"OK: Session info readable – user='{info.User}' " +
                                 $"system='{info.SystemName}' client='{info.Client}'.");
                }
                catch (Exception ex)
                {
                    findings.Add($"WARN: Session exists but info is not readable – {ex.Message} " +
                                 "(session may still be loading).");
                }
                break; // one healthy connection is enough
            }

            if (!sessionFound)
            {
                findings.Add("FAIL: No usable sessions found across any connection. " +
                             "Ensure at least one SAP window is open and fully logged in.");
                return new HealthCheckResult(false, findings);
            }

            return new HealthCheckResult(true, findings);
        }
        finally
        {
            // Release the temporary COM reference obtained for the health check.
            // This does NOT close SAP GUI — it only decrements the COM reference count.
            try
            {
                if (Marshal.IsComObject(app.RawObject))
                    Marshal.ReleaseComObject(app.RawObject);
            }
            catch { /* best-effort */ }
        }
    }

    /// <summary>
    /// Runs <see cref="HealthCheck"/> and throws <see cref="InvalidOperationException"/>
    /// when any check fails. Prefer this in workflows that require a fully healthy SAP
    /// environment at startup and want a single fail-fast call.
    ///
    /// <code>
    /// // Fail fast at the top of the workflow
    /// SapGuiClient.EnsureHealthy();
    ///
    /// using var sap = SapGuiClient.Attach();
    /// sap.Session.StartTransaction("SE16");
    /// </code>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   One or more health checks failed. The message contains all <c>FAIL:</c> findings.
    /// </exception>
    public static void EnsureHealthy()
    {
        var result = HealthCheck();
        if (!result.IsHealthy)
            throw new InvalidOperationException(
                $"SAP GUI health check failed:{Environment.NewLine}{result.FailureSummary}");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static bool IsProcessRunning(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            try
            {
                return processes.Length > 0;
            }
            finally
            {
                foreach (var process in processes)
                    process.Dispose();
            }
        }
        catch { return false; }
    }
}

