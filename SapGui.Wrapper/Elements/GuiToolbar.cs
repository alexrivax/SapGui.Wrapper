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
    /// Presses a toolbar button by its ID (e.g. <c>"wnd[0]/tbar[1]/btn[8]"</c>).
    /// Using <c>session.findById("...").press()</c> is usually simpler.
    /// </summary>
    public void PressButtonById(string buttonId) =>
        Invoke("PressButton", buttonId);

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
