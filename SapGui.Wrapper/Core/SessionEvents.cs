namespace SapGui.Wrapper;

// ── Event argument types ──────────────────────────────────────────────────────

/// <summary>
/// Arguments supplied when <see cref="GuiSession.Change"/> fires.
/// Carries a snapshot of the screen after a server round-trip completes.
/// </summary>
public sealed class SessionChangeEventArgs : EventArgs
{
    /// <summary>Status bar text after the round-trip.</summary>
    public string Text          { get; }

    /// <summary>SAP function code that triggered the round-trip (e.g. "BACK", "EXEC").
    /// Populated when using the COM event sink; empty when using the polling fallback.
    /// </summary>
    public string FunctionCode  { get; }

    /// <summary>
    /// Status bar message type character: <c>S</c> success, <c>W</c> warning,
    /// <c>E</c> error, <c>A</c> abend, or empty for information.
    /// </summary>
    public string MessageType   { get; }

    internal SessionChangeEventArgs(string text, string functionCode, string messageType)
    {
        Text         = text;
        FunctionCode = functionCode;
        MessageType  = messageType;
    }
}

/// <summary>
/// Arguments supplied when <see cref="GuiSession.StartRequest"/> fires.
/// Indicates that SAP GUI is beginning a server round-trip.
/// </summary>
public sealed class StartRequestEventArgs : EventArgs
{
    /// <summary>
    /// Supplemental text at the moment the request started.
    /// Populated when using the COM event sink; empty in the polling fallback.
    /// </summary>
    public string Text { get; }

    internal StartRequestEventArgs(string text) => Text = text;
}

/// <summary>
/// Arguments supplied when <see cref="GuiSession.EndRequest"/> fires.
/// Indicates that a server round-trip has completed.
/// </summary>
public sealed class EndRequestEventArgs : EventArgs
{
    /// <summary>Status bar text after the round-trip.</summary>
    public string Text { get; }

    /// <summary>SAP function code that ended the round-trip.
    /// Populated when using the COM event sink; empty in the polling fallback.
    /// </summary>
    public string FunctionCode { get; }

    /// <summary>Status bar message type character (<c>S</c>, <c>W</c>, <c>E</c>, <c>A</c>).</summary>
    public string MessageType { get; }

    internal EndRequestEventArgs(string text, string functionCode, string messageType)
    {
        Text         = text;
        FunctionCode = functionCode;
        MessageType  = messageType;
    }
}

/// <summary>
/// Arguments supplied when <see cref="GuiSession.AbapRuntimeError"/> fires.
/// </summary>
public sealed class AbapRuntimeErrorEventArgs : EventArgs
{
    /// <summary>Status bar or window title text at the time of the error.</summary>
    public string Message { get; }

    internal AbapRuntimeErrorEventArgs(string message) => Message = message;
}

// ── Internal session monitor ──────────────────────────────────────────────────

/// <summary>
/// Background polling monitor attached to a <see cref="GuiSession"/> that
/// raises .NET events when the session state changes.
/// <para>
/// Used as a fallback when the COM event sink (<c>GuiSessionComSink</c>) cannot
/// connect to the session's <c>IConnectionPointContainer</c>.
/// <c>StartRequest</c> and <c>EndRequest</c> are approximated by
/// detecting <c>IsBusy</c> transitions; <c>FunctionCode</c> is always empty.
/// </para>
/// </summary>
internal sealed class SessionEventMonitor : IDisposable
{
    private readonly GuiSession          _session;
    private readonly CancellationTokenSource _cts = new();
    private readonly Thread              _thread;

    private bool   _wasBusy;
    private bool   _sessionAlive = true;

    // ── Constructor ───────────────────────────────────────────────────────────

    internal SessionEventMonitor(GuiSession session, int pollMs)
    {
        _session   = session;
        _wasBusy  = session.IsBusy;

        _thread = new Thread(() => Run(pollMs, _cts.Token))
        {
            IsBackground = true,
            Name         = "SapGuiSessionMonitor",
        };
        _thread.Start();
    }

    // ── Internal loop ─────────────────────────────────────────────────────────

    private void Run(int pollMs, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { Thread.Sleep(pollMs); }
            catch (ThreadInterruptedException) { return; }

            if (ct.IsCancellationRequested) return;

            try
            {
                bool nowBusy = _session.IsBusy;

                // Idle → Busy: server round-trip is starting
                if (!_wasBusy && nowBusy)
                {
                    _session.RaiseStartRequest(new StartRequestEventArgs(string.Empty));
                }

                // Busy → Idle: round-trip just completed
                if (_wasBusy && !nowBusy)
                {
                    string sbText    = string.Empty;
                    string sbMsgType = string.Empty;
                    try
                    {
                        var sb = _session.Statusbar();
                        sbText    = sb.Text;
                        sbMsgType = sb.MessageType;
                    }
                    catch { /* status bar inaccessible – leave empty */ }

                    _session.RaiseEndRequest(
                        new EndRequestEventArgs(sbText, string.Empty, sbMsgType));

                    _session.RaiseChange(
                        new SessionChangeEventArgs(
                            text:         sbText,
                            functionCode: string.Empty,   // not available via polling
                            messageType:  sbMsgType));

                    if (sbMsgType == "A")
                        _session.RaiseAbapRuntimeError(new AbapRuntimeErrorEventArgs(sbText));
                }

                _wasBusy = nowBusy;
            }
            catch
            {
                // Any exception accessing the session means it was destroyed
                if (_sessionAlive)
                {
                    _sessionAlive = false;
                    _session.RaiseDestroy();
                }
                return;
            }
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        _cts.Cancel();
        _thread.Interrupt();
    }
}
