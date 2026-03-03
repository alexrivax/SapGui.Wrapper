namespace SapGui.Wrapper;

/// <summary>
/// Wraps a SAP GUI tab strip control (GuiTabStrip).
/// Contains one or more <see cref="GuiTab"/> children.
/// </summary>
public class GuiTabStrip : GuiComponent
{
    internal GuiTabStrip(object raw) : base(raw) { }

    /// <summary>Number of tabs.</summary>
    public int TabCount
    {
        get
        {
            var ch = Invoke("Children");
            if (ch is null) return 0;
            return (int)(ch.GetType().InvokeMember("Count",
                BindingFlags.GetProperty, null, ch, null) ?? 0);
        }
    }

    /// <summary>Returns all tabs as typed wrappers.</summary>
    public IReadOnlyList<GuiTab> GetTabs()
    {
        var children = Invoke("Children");
        if (children is null) return Array.Empty<GuiTab>();

        var ct    = children.GetType();
        int count = (int)(ct.InvokeMember("Count",
            BindingFlags.GetProperty, null, children, null) ?? 0);

        var result = new List<GuiTab>(count);
        for (int i = 0; i < count; i++)
        {
            var raw = ct.InvokeMember("Item",
                BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                null, children, new object[] { i });
            if (raw is not null) result.Add(new GuiTab(raw));
        }
        return result;
    }

    /// <summary>Returns a tab by its zero-based index.</summary>
    public GuiTab GetTab(int index) => GetTabs()[index];

    /// <summary>Returns the first tab whose name contains <paramref name="nameFragment"/> (case-insensitive).</summary>
    public GuiTab? GetTabByName(string nameFragment) =>
        GetTabs().FirstOrDefault(t =>
            t.Text.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0);

    /// <summary>Selects a tab by its zero-based index.</summary>
    public void SelectTab(int index) => GetTab(index).Select();
}

/// <summary>
/// Wraps a single tab page inside a <see cref="GuiTabStrip"/>.
/// </summary>
public class GuiTab : GuiComponent
{
    internal GuiTab(object raw) : base(raw) { }

    /// <summary>Tab label text.</summary>
    public override string Text => GetString("Text");

    /// <summary>Activates (selects) this tab.</summary>
    public void Select() => Invoke("Select");

    /// <inheritdoc/>
    public override string ToString() => $"GuiTab [{Id}] \"{Text}\"";
}
