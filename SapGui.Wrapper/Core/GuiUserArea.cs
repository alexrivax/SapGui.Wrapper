namespace SapGui.Wrapper;

/// <summary>
/// Wraps the SAP GUI user area (<c>GuiUserArea</c>), which is the content
/// region of a dynpro screen — typically found at <c>wnd[0]/usr</c>.
/// <para>
/// Exposes <see cref="FindById"/> so that you can address child controls
/// using relative IDs instead of always qualifying from <c>wnd[0]/usr/…</c>.
/// </para>
/// </summary>
public class GuiUserArea : GuiComponent
{
    internal GuiUserArea(object raw) : base(raw) { }

    /// <summary>
    /// Finds a child component by its ID relative to this user area
    /// and returns a typed wrapper.
    /// </summary>
    /// <param name="relativeId">
    /// ID path relative to this container, e.g. <c>"txtRSYST-BNAME"</c>
    /// instead of the full <c>"wnd[0]/usr/txtRSYST-BNAME"</c>.
    /// </param>
    /// <exception cref="SapComponentNotFoundException">
    /// No component with that ID exists under this user area.
    /// </exception>
    public GuiComponent FindById(string relativeId)
    {
        object raw;
        try
        {
            raw = RawObject.GetType()
                           .InvokeMember("FindById",
                                         BindingFlags.InvokeMethod,
                                         null, RawObject,
                                         new object[] { relativeId })
                  ?? throw new SapComponentNotFoundException(relativeId);
        }
        catch (Exception ex) when (ex is not SapComponentNotFoundException)
        {
            throw new SapComponentNotFoundException(relativeId, ex);
        }

        return GuiSession.WrapComponent(raw);
    }

    /// <summary>
    /// Finds a child component by relative ID and casts it to
    /// <typeparamref name="T"/>.
    /// </summary>
    public T FindById<T>(string relativeId) where T : GuiComponent
    {
        var component = FindById(relativeId);
        if (component is T typed) return typed;
        throw new InvalidCastException(
            $"Component '{relativeId}' is of type '{component.TypeName}', not '{typeof(T).Name}'.");
    }
}
