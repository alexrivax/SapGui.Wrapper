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
}
