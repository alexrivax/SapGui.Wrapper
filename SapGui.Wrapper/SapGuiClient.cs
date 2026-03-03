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

    /// <summary>The underlying <see cref="GuiApplication"/> instance.</summary>
    public GuiApplication Application { get; }

    /// <summary>
    /// The first available session. For multi-session scenarios
    /// use <see cref="GetSession"/>.
    /// </summary>
    public GuiSession Session => Application.GetFirstSession();

    private SapGuiClient(GuiApplication app)
    {
        Application = app;
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Attaches to the currently running SAP GUI application.
    /// Requires SAP GUI to be open and scripting to be enabled.
    /// </summary>
    /// <exception cref="SapGuiNotFoundException">
    ///   SAP GUI is not running or scripting is disabled.
    /// </exception>
    public static SapGuiClient Attach() =>
        new(GuiApplication.Attach());

    // ── Multi-connection / multi-session helpers ───────────────────────────────

    /// <summary>All active connections.</summary>
    public IReadOnlyList<GuiConnection> GetConnections() =>
        Application.GetConnections();

    /// <summary>
    /// Gets a session by connection index and session index (both 0-based).
    /// </summary>
    public GuiSession GetSession(int connectionIndex = 0, int sessionIndex = 0) =>
        Application.GetConnections()[connectionIndex].GetSession(sessionIndex);

    // ── IDisposable ───────────────────────────────────────────────────────────

    /// <summary>
    /// Releasing the managed wrapper. Does NOT close SAP GUI itself.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // COM objects will be released by the GC; nothing extra needed.
            _disposed = true;
        }
    }
}
