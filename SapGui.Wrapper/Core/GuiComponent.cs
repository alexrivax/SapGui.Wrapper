namespace SapGui.Wrapper;

/// <summary>
/// Base class for all SAP GUI scripting objects.
/// Wraps the raw COM object and provides safe property access via late binding.
/// </summary>
public class GuiComponent
{
    /// <summary>The raw underlying COM object. Use only if you need access to
    /// a property not yet exposed by this wrapper.</summary>
    public object RawObject { get; }

    internal GuiComponent(object rawComObject)
    {
        RawObject = rawComObject ?? throw new ArgumentNullException(nameof(rawComObject));
    }

    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>e.g. "wnd[0]/usr/txtRSYST-BNAME"</summary>
    public string Id       => GetString("Id");

    /// <summary>e.g. "GuiTextField"</summary>
    public string TypeName => GetString("Type");

    /// <summary>Parsed enum version of <see cref="TypeName"/>.</summary>
    public SapComponentType ComponentType =>
        SapComponentTypeHelper.FromString(TypeName);

    /// <summary>Screen tooltip / short description.</summary>
    public string Tooltip  => GetString("Tooltip");

    /// <summary>Accessibility name.</summary>
    public string Name     => GetString("Name");

    // ── Visibility / state ───────────────────────────────────────────────────

    /// <summary>Whether this component accepts user input.</summary>
    public bool Changeable  => GetBool("Changeable");

    /// <summary>Whether the component value has been changed since the last server round-trip.</summary>
    public bool IsModified  => GetBool("Modified");

    // ── Universal properties (available on every component) ──────────────────
    // These mirror the most common properties the SAP Script Recorder accesses
    // directly on the raw object returned by session.findById(...).
    // Typed subclasses inherit these, so FindById(id).Text works without casting.

    /// <summary>
    /// Gets or sets the <c>Text</c> property of this component.
    /// Works on text fields, buttons, labels, combo boxes, status bars, etc.
    /// Equivalent to VBA: <c>session.findById("...").Text = "value"</c>
    /// </summary>
    public virtual string Text
    {
        get => GetString("Text");
        set => SetProperty("Text", value);
    }

    /// <summary>
    /// Clicks / activates this component (calls the COM <c>Press</c> method).
    /// Works on buttons and toolbar entries.
    /// Equivalent to VBA: <c>session.findById("...").Press()</c>
    /// </summary>
    public virtual void Press() => Invoke("Press");

    /// <summary>Moves keyboard focus to this component.</summary>
    public virtual void SetFocus() => Invoke("SetFocus");

    // ── Helpers ───────────────────────────────────────────────────────────────
    /// <summary>Reads a string property from the underlying COM object via late binding.</summary>
    /// <param name="property">COM property name, e.g. <c>"Text"</c>.</param>
    protected string GetString(string property)
    {
        try
        {
            return (string)(RawObject.GetType()
                                     .InvokeMember(property,
                                                   BindingFlags.GetProperty,
                                                   null, RawObject, null) ?? string.Empty);
        }
        catch { return string.Empty; }
    }

    /// <summary>Reads a boolean property from the underlying COM object via late binding.</summary>
    /// <param name="property">COM property name, e.g. <c>"Changeable"</c>.</param>
    protected bool GetBool(string property)
    {
        try
        {
            return (bool)(RawObject.GetType()
                                   .InvokeMember(property,
                                                 BindingFlags.GetProperty,
                                                 null, RawObject, null) ?? false);
        }
        catch { return false; }
    }

    /// <summary>Reads an integer property from the underlying COM object via late binding.</summary>
    /// <param name="property">COM property name, e.g. <c>"RowCount"</c>.</param>
    protected int GetInt(string property)
    {
        try
        {
            return (int)(RawObject.GetType()
                                  .InvokeMember(property,
                                                BindingFlags.GetProperty,
                                                null, RawObject, null) ?? 0);
        }
        catch { return 0; }
    }

    /// <summary>Sets a property on the underlying COM object via late binding.</summary>
    /// <param name="property">COM property name.</param>
    /// <param name="value">New value.</param>
    protected void SetProperty(string property, object value)
    {
        RawObject.GetType()
                 .InvokeMember(property,
                               BindingFlags.SetProperty,
                               null, RawObject,
                               new[] { value });
    }

    /// <summary>Invokes a method on the underlying COM object via late binding.</summary>
    /// <param name="method">COM method name, e.g. <c>"Press"</c>.</param>
    /// <param name="args">Arguments to pass to the method.</param>
    protected object? Invoke(string method, params object[] args)
    {
        return RawObject.GetType()
                        .InvokeMember(method,
                                      BindingFlags.InvokeMethod,
                                      null, RawObject, args);
    }

    /// <summary>
    /// Returns the raw COM child collection object (GuiComponentCollection).
    /// Use <see cref="GuiSession.FindById"/> for most navigation scenarios.
    /// </summary>
    protected object? GetChildrenRaw() => Invoke("Children");

    /// <summary>
    /// Returns a child by its integer index from the component's Children collection.
    /// </summary>
    protected object? GetChildAtRaw(int index)
    {
        var children = GetChildrenRaw();
        if (children is null) return null;
        return children.GetType()
                       .InvokeMember("Item",
                                     BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                                     null, children,
                                     new object[] { index });
    }

    /// <inheritdoc/>
    public override string ToString() => $"{TypeName} [{Id}]";
}
