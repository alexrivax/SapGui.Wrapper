namespace SapGui.Wrapper;

/// <summary>
/// Lightweight wrapper around a SAP GUI scrollbar COM object
/// (<c>GuiScrollbar</c>). Returned by <see cref="GuiScrollContainer.VerticalScrollbar"/>
/// and <see cref="GuiScrollContainer.HorizontalScrollbar"/>.
/// </summary>
public sealed class GuiScrollbar
{
    private readonly object _raw;

    internal GuiScrollbar(object raw) { _raw = raw; }

    private int GetInt(string prop)
    {
        try
        {
            return (int)(_raw.GetType()
                             .InvokeMember(prop,
                                           BindingFlags.GetProperty,
                                           null, _raw, null) ?? 0);
        }
        catch { return 0; }
    }

    /// <summary>Minimum scroll position (usually 0).</summary>
    public int Minimum  => GetInt("Minimum");

    /// <summary>Maximum scroll position.</summary>
    public int Maximum  => GetInt("Maximum");

    /// <summary>Number of units visible in one page.</summary>
    public int PageSize => GetInt("PageSize");

    /// <summary>Current scroll position. Set to scroll programmatically.</summary>
    public int Position
    {
        get => GetInt("Position");
        set => _raw.GetType()
                   .InvokeMember("Position",
                                 BindingFlags.SetProperty,
                                 null, _raw,
                                 new object[] { value });
    }
}

/// <summary>
/// Wraps a SAP GUI scroll container (<c>GuiScrollContainer</c>).
/// Scroll containers appear around screen areas that exceed the available
/// window space and expose vertical and/or horizontal scrollbars.
/// </summary>
public class GuiScrollContainer : GuiComponent
{
    internal GuiScrollContainer(object raw) : base(raw) { }

    private object GetScrollbarRaw(string property)
    {
        return RawObject.GetType()
                        .InvokeMember(property,
                                      BindingFlags.GetProperty,
                                      null, RawObject, null)
               ?? throw new InvalidOperationException($"Scrollbar '{property}' is not available on this control.");
    }

    /// <summary>
    /// The vertical scrollbar for this container.
    /// Use <see cref="GuiScrollbar.Position"/> to read or set the scroll offset,
    /// and <see cref="ScrollToTop"/> as a convenience.
    /// </summary>
    public GuiScrollbar VerticalScrollbar   => new(GetScrollbarRaw("VerticalScrollbar"));

    /// <summary>
    /// The horizontal scrollbar for this container.
    /// Use <see cref="GuiScrollbar.Position"/> to read or set the scroll offset.
    /// </summary>
    public GuiScrollbar HorizontalScrollbar => new(GetScrollbarRaw("HorizontalScrollbar"));

    /// <summary>
    /// Scrolls the container to the very top by setting the vertical
    /// scroll position to its minimum value.
    /// </summary>
    public void ScrollToTop()
    {
        var sb = VerticalScrollbar;
        sb.Position = sb.Minimum;
    }
}
