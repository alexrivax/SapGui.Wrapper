using SapGui.Wrapper.Com;

namespace SapGui.Wrapper;

/// <summary>
/// Entry point. Represents the running SAP GUI application.
/// Equivalent to the VBA <c>GuiApplication</c> object obtained via
/// <c>SapROTWr.CSapROTWrapper.GetROTEntry("SAPGUI").GetScriptingEngine()</c>.
/// </summary>
public class GuiApplication : GuiComponent
{
    private GuiApplication(object raw) : base(raw) { }

    // ── Factory methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Attaches to the currently running SAP GUI instance.
    /// SAP GUI must be open and scripting must be enabled.
    /// </summary>
    /// <exception cref="SapGuiNotFoundException"/>
    public static GuiApplication Attach() =>
        new(SapRot.GetGuiApplication());

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>SAP GUI version string, e.g. "750" or "760".</summary>
    public string Version => GetString("Version");

    /// <summary>Number of active server connections.</summary>
    public int ConnectionCount => GetInt("Children.Count");

    /// <summary>
    /// Returns the currently focused (active) <see cref="GuiSession"/>.
    /// Equivalent to the VBA <c>application.ActiveSession</c> shortcut.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when SAP reports no active session.</exception>
    public GuiSession ActiveSession
    {
        get
        {
            var raw = Invoke("ActiveSession")
                      ?? throw new InvalidOperationException("No active SAP GUI session found.");
            return new GuiSession(raw);
        }
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all active server connections.
    /// </summary>
    public IReadOnlyList<GuiConnection> GetConnections()
    {
        var children = Invoke("Children")
            ?? throw new InvalidOperationException("Cannot read SAP GUI connections.");

        var childrenType = children.GetType();
        int count = (int)(childrenType.InvokeMember("Count",
                                                     BindingFlags.GetProperty,
                                                     null, children, null) ?? 0);
        var result = new List<GuiConnection>(count);
        for (int i = 0; i < count; i++)
        {
            var raw = childrenType.InvokeMember("Item",
                                                 BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                                                 null, children,
                                                 new object[] { i });
            if (raw is not null) result.Add(new GuiConnection(raw));
        }
        return result;
    }

    /// <summary>
    /// Gets the first available connection (convenience helper).
    /// </summary>
    public GuiConnection GetFirstConnection()
    {
        var connections = GetConnections();
        if (connections.Count == 0)
            throw new InvalidOperationException("No active SAP GUI connections found.");
        return connections[0];
    }

    /// <summary>
    /// Gets the first session of the first connection (convenience helper).
    /// Equivalent to the VBA one-liner:
    /// <c>application.Children(0).Children(0)</c>
    /// </summary>
    public GuiSession GetFirstSession() =>
        GetFirstConnection().GetFirstSession();

    /// <summary>
    /// Opens a connection to an SAP system described by <paramref name="description"/>
    /// (the system entry name as configured in the SAP Logon Pad).
    /// Equivalent to the VBA <c>application.OpenConnection(description)</c>.
    /// <para>
    /// After calling this method, use <see cref="GetConnections"/> or
    /// <see cref="ActiveSession"/> to obtain a usable <see cref="GuiSession"/>.
    /// </para>
    /// </summary>
    /// <param name="description">The SAP Logon Pad system entry name, e.g. <c>"PRD - Production"</c>.</param>
    /// <param name="sync">
    /// When <see langword="true"/> (default), the call blocks until the logon
    /// screen is ready. Pass <see langword="false"/> for asynchronous open.
    /// </param>
    /// <returns>The newly opened <see cref="GuiConnection"/>.</returns>
    public GuiConnection OpenConnection(string description, bool sync = true)
    {
        var raw = Invoke("OpenConnection", description, sync)
                  ?? throw new InvalidOperationException(
                       $"OpenConnection returned null for '{description}'.");
        return new GuiConnection(raw);
    }
}
