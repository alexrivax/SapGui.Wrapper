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

    /// <summary>SAP function code that triggered the round-trip (e.g. "BACK", "EXEC").</summary>
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
/// Polling is used instead of COM event sinks because the latter requires
/// importing the SAP GUI type library; see Priority 4 in the project TODO
/// for the full COM event-sink implementation plan.
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

                // Round-trip just completed: was busy, now idle → fire Change
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

                    var args = new SessionChangeEventArgs(
                        text:         sbText,
                        functionCode: string.Empty,   // requires true COM event sink
                        messageType:  sbMsgType);

                    _session.RaiseChange(args);

                    // AbapRuntimeError: message type 'A' = Abend
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

    // ── IDisposable ───────────────────────────────────────────────────────────    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        _cts.Cancel();
        _thread.Interrupt();
    }
}
