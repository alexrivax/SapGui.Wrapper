namespace SapGui.Wrapper;

/// <summary>
/// Represents a single server connection inside SAP GUI.
/// A connection can have multiple sessions (windows).
/// </summary>
public class GuiConnection : GuiComponent
{
    internal GuiConnection(object raw) : base(raw) { }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Application server hostname.</summary>
    public string Host        => GetString("Description");

    /// <summary>System number.</summary>
    public string SystemName  => GetString("SystemName");

    /// <summary>Whether scripting has been disabled by the server policy.</summary>
    public bool DisabledByServer => GetBool("DisabledByServer");

    /// <summary>Number of open sessions on this connection.</summary>
    public int SessionCount
    {
        get
        {
            var ch = Invoke("Children");
            if (ch is null) return 0;
            return (int)(ch.GetType().InvokeMember("Count",
                                                    BindingFlags.GetProperty,
                                                    null, ch, null) ?? 0);
        }
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    /// <summary>Returns all sessions on this connection.</summary>
    public IReadOnlyList<GuiSession> GetSessions()
    {
        var children = Invoke("Children")
            ?? throw new InvalidOperationException("Cannot read SAP GUI sessions.");

        var ct    = children.GetType();
        int count = (int)(ct.InvokeMember("Count",
                                          BindingFlags.GetProperty,
                                          null, children, null) ?? 0);

        var result = new List<GuiSession>(count);
        for (int i = 0; i < count; i++)
        {
            var raw = ct.InvokeMember("Item",
                                       BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                                       null, children,
                                       new object[] { i });
            if (raw is not null) result.Add(new GuiSession(raw));
        }
        return result;
    }

    /// <summary>Gets the first session on this connection.</summary>
    public GuiSession GetFirstSession()
    {
        var sessions = GetSessions();
        if (sessions.Count == 0)
            throw new InvalidOperationException("No active sessions on this connection.");
        return sessions[0];
    }

    /// <summary>Gets a specific session by zero-based index.</summary>
    public GuiSession GetSession(int index) => GetSessions()[index];
}
