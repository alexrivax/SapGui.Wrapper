namespace SapGui.Wrapper;

/// <summary>
/// Convenience helper with static, dependency-free methods designed for use
/// in UiPath <b>Invoke Code</b> and <b>Code Activity</b> activities.
///
/// <para>All methods perform a fresh <c>Attach()</c> on every call, which is
/// safe for UiPath workflows where the activity runs in a single-threaded
/// activity context without keeping state between activities.</para>
///
/// <para>Example (UiPath Invoke Code – VB.NET):</para>
/// <code>
/// ' Set a text field
/// SapGuiHelper.SetText("wnd[0]/usr/txtRSYST-BNAME", "myuser")
///
/// ' Read a text field
/// Dim val As String = SapGuiHelper.GetText("wnd[0]/usr/txtRSYST-BNAME")
///
/// ' Press Enter
/// SapGuiHelper.PressEnter()
///
/// ' Start a transaction
/// SapGuiHelper.StartTransaction("MM60")
/// </code>
/// </summary>
public static class SapGuiHelper
{
    // ── Connection shortcut ───────────────────────────────────────────────────

    /// <summary>
    /// Attaches to the running SAP GUI and returns the specified session.
    /// The <see cref="SapGuiClient"/> is disposed after the call; this is safe
    /// because the underlying COM session object remains alive inside SAP GUI.
    /// </summary>
    public static GuiSession GetSession(int connection = 0, int session = 0)
    {
        using var client = SapGuiClient.Attach();
        return client.GetSession(connection, session);
    }

    // ── Text fields ───────────────────────────────────────────────────────────

    /// <summary>Sets the <c>Text</c> property of a text field.</summary>
    public static void SetText(string fieldId, string value,
                               int connection = 0, int session = 0)
    {
        GetSession(connection, session).TextField(fieldId).Text = value;
    }

    /// <summary>Reads the <c>Text</c> property of a text field.</summary>
    public static string GetText(string fieldId,
                                 int connection = 0, int session = 0) =>
        GetSession(connection, session).TextField(fieldId).Text;

    // ── Buttons ───────────────────────────────────────────────────────────────

    /// <summary>Presses a button by its ID.</summary>
    public static void PressButton(string buttonId,
                                   int connection = 0, int session = 0) =>
        GetSession(connection, session).Button(buttonId).Press();

    // ── VKey shortcuts ────────────────────────────────────────────────────────

    /// <summary>Sends Enter (VKey 0) to the main window.</summary>
    public static void PressEnter(int connection = 0, int session = 0) =>
        GetSession(connection, session).PressEnter();

    /// <summary>Sends Back/F3 (VKey 3) to the main window.</summary>
    public static void PressBack(int connection = 0, int session = 0) =>
        GetSession(connection, session).PressBack();

    /// <summary>Sends Save/Ctrl+S (VKey 11) to the main window.</summary>
    public static void PressSave(int connection = 0, int session = 0) =>
        GetSession(connection, session).SendVKey(11);

    /// <summary>Sends a virtual key to the main window.</summary>
    public static void SendVKey(int vKey, int connection = 0, int session = 0) =>
        GetSession(connection, session).SendVKey(vKey);

    // ── Transactions ──────────────────────────────────────────────────────────

    /// <summary>
    /// Enters a transaction code (types it in the command field and presses Enter).
    /// </summary>
    public static void StartTransaction(string tCode,
                                        int connection = 0, int session = 0) =>
        GetSession(connection, session).StartTransaction(tCode);

    // ── Status bar ────────────────────────────────────────────────────────────

    /// <summary>Returns the status bar message from the main window.</summary>
    public static string GetStatusMessage(int connection = 0, int session = 0) =>
        GetSession(connection, session).Statusbar().Text;

    /// <summary>Returns true if the last action produced an error message.</summary>
    public static bool HasError(int connection = 0, int session = 0) =>
        GetSession(connection, session).Statusbar().IsError;

    // ── Check boxes / radio buttons ───────────────────────────────────────────

    /// <summary>Sets the state of a check box.</summary>
    public static void SetCheckBox(string id, bool selected,
                                   int connection = 0, int session = 0) =>
        GetSession(connection, session).CheckBox(id).Selected = selected;

    /// <summary>Sets the key of a combo box.</summary>
    public static void SetComboBox(string id, string key,
                                   int connection = 0, int session = 0) =>
        GetSession(connection, session).ComboBox(id).Key = key;

    // ── Lab read ──────────────────────────────────────────────────────────────

    /// <summary>Returns the text of a label element.</summary>
    public static string GetLabel(string id, int connection = 0, int session = 0) =>
        GetSession(connection, session).Label(id).Text;

    // ── Generic FindById ──────────────────────────────────────────────────────

    /// <summary>
    /// Finds any component by ID and returns it as a <see cref="GuiComponent"/>.
    /// Cast to the specific type when you need type-specific members.
    /// </summary>
    public static GuiComponent FindById(string id, int connection = 0, int session = 0) =>
        GetSession(connection, session).FindById(id);

    // ── Session info ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns information about the current session (system, client, user, TCode).
    /// </summary>
    public static GuiSessionInfo GetSessionInfo(int connection = 0, int session = 0) =>
        GetSession(connection, session).Info;
}
