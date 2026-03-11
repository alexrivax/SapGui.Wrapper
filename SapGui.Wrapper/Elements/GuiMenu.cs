namespace SapGui.Wrapper;

/// <summary>
/// Wraps the SAP GUI menu bar (GuiMenubar).
/// To navigate to a menu item, use <see cref="SelectItem(GuiSession, string)"/>
/// or call <c>session.Menu("wnd[0]/mbar/menu[0]/menu[1]").Select()</c> directly.
/// </summary>
public class GuiMenubar : GuiComponent
{
    internal GuiMenubar(object raw) : base(raw) { }

    /// <summary>Number of top-level menu entries.</summary>
    public int Count
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
    /// Selects a menu item by navigating to it via its full ID path and calling
    /// <see cref="GuiMenu.Select"/>.
    /// </summary>
    /// <param name="session">The active SAP session.</param>
    /// <param name="itemId">Full SAP component ID path, e.g.
    /// <c>"wnd[0]/mbar/menu[0]/menu[1]"</c>.</param>
    public void SelectItem(GuiSession session, string itemId) =>
        session.Menu(itemId).Select();
}

/// <summary>
/// Wraps a single SAP GUI menu or sub-menu entry (GuiMenu / GuiSubMenu).
/// </summary>
public class GuiMenu : GuiComponent
{
    internal GuiMenu(object raw) : base(raw) { }

    /// <summary>Menu item label text.</summary>
    public override string Text => GetString("Text");

    /// <summary>
    /// Selects / clicks this menu item.
    /// </summary>
    public void Select() => Invoke("Select");

    /// <summary>Returns all child menu items of this sub-menu.</summary>
    public IReadOnlyList<GuiMenu> GetChildren()
    {
        var children = Invoke("Children");
        if (children is null) return Array.Empty<GuiMenu>();

        var ct    = children.GetType();
        int count = (int)(ct.InvokeMember("Count",
            BindingFlags.GetProperty, null, children, null) ?? 0);

        var result = new List<GuiMenu>(count);
        for (int i = 0; i < count; i++)
        {
            var raw = ct.InvokeMember("Item",
                BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                null, children, new object[] { i });
            if (raw is not null) result.Add(new GuiMenu(raw));
        }
        return result;
    }

    /// <inheritdoc/>
    public override string ToString() => $"GuiMenu [{Id}] \"{Text}\"";
}

/// <summary>
/// Wraps a SAP GUI context menu (GuiContextMenu) that appears on right-click.
/// </summary>
public class GuiContextMenu : GuiComponent
{
    internal GuiContextMenu(object raw) : base(raw) { }

    /// <summary>
    /// Selects a context menu item by its function code string.
    /// The function code is visible in the SAP Script Recorder output.
    /// </summary>
    public void SelectByFunctionCode(string functionCode) =>
        Invoke("SelectByName", functionCode);

    /// <summary>Returns all item texts in the context menu.</summary>
    public IReadOnlyList<string> GetItemTexts()
    {
        var children = Invoke("Children");
        if (children is null) return Array.Empty<string>();

        var ct    = children.GetType();
        int count = (int)(ct.InvokeMember("Count",
            BindingFlags.GetProperty, null, children, null) ?? 0);

        var result = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            var raw = ct.InvokeMember("Item",
                BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                null, children, new object[] { i });
            if (raw is null) continue;
            var text = (string?)raw.GetType().InvokeMember("Text",
                BindingFlags.GetProperty, null, raw, null) ?? string.Empty;
            result.Add(text);
        }
        return result;
    }

    /// <summary>Closes the context menu without selecting anything.</summary>
    public void Close() => Invoke("Close");
}
