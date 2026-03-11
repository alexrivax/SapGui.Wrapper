namespace SapGui.Wrapper;

/// <summary>
/// Wraps a SAP GUI application toolbar (GuiToolbar).
/// This is the row of icon buttons at the top of a screen.
/// </summary>
public class GuiToolbar : GuiComponent
{
    internal GuiToolbar(object raw) : base(raw) { }

    /// <summary>Number of buttons in the toolbar.</summary>
    public int ButtonCount
    {
        get
        {
            var ch = Invoke("Children");
            if (ch is null) return 0;
            return (int)(ch.GetType().InvokeMember("Count",
                BindingFlags.GetProperty, null, ch, null) ?? 0);
        }
    }

    /// <summary>
    /// Presses a toolbar button by its zero-based index.
    /// </summary>
    public void PressButton(int index)
    {
        var children = Invoke("Children");
        if (children is null) return;
        var ct  = children.GetType();
        var btn = ct.InvokeMember("Item",
            BindingFlags.GetProperty | BindingFlags.InvokeMethod,
            null, children, new object[] { index });
        btn?.GetType().InvokeMember("Press",
            BindingFlags.InvokeMethod, null, btn, null);
    }

    /// <summary>
    /// Presses a toolbar button by its SAP function code string
    /// (e.g. <c>"SAVE"</c>, <c>"BACK"</c>, <c>"EXEC"</c>).
    /// The function code is visible in the SAP Script Recorder output.
    /// To press a button by its component path, use
    /// <c>session.FindById("wnd[0]/tbar[1]/btn[8]").Press()</c> instead.
    /// </summary>
    public void PressButtonByFunctionCode(string functionCode) =>
        Invoke("PressButton", functionCode);

    /// <summary>Returns the tooltip text of a button by zero-based index.</summary>
    public string GetButtonTooltip(int index)
    {
        var children = Invoke("Children");
        if (children is null) return string.Empty;
        var ct  = children.GetType();
        var btn = ct.InvokeMember("Item",
            BindingFlags.GetProperty | BindingFlags.InvokeMethod,
            null, children, new object[] { index });
        if (btn is null) return string.Empty;
        return (string?)btn.GetType().InvokeMember("Tooltip",
            BindingFlags.GetProperty, null, btn, null) ?? string.Empty;
    }
}
