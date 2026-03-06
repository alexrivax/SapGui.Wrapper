namespace SapGui.Wrapper;

/// <summary>
/// Generic typed wrapper for SAP GUI shell controls (<c>GuiShell</c>).
/// Shell controls are a family of ActiveX-based SAP controls — including
/// ALV grids, trees, calendars and others — that all report <c>Type = "GuiShell"</c>
/// at the base level.  More specific shells (e.g. <c>GuiGridView</c>,
/// <c>GuiTree</c>) are already wrapped by their own classes; this wrapper
/// acts as the typed fallback for any shell variant that is not yet
/// individually wrapped, giving you a named type instead of the bare
/// <see cref="GuiComponent"/> base.
/// <para>
/// For shell types that are not specifically wrapped, access properties and
/// methods via <see cref="GuiComponent.RawObject"/> or
/// <c>session.findById(id)</c> which returns <see langword="dynamic"/>.
/// </para>
/// </summary>
public class GuiShell : GuiComponent
{
    internal GuiShell(object raw) : base(raw) { }

    /// <summary>
    /// The shell sub-type string as reported by SAP
    /// (e.g. <c>"GridView"</c>, <c>"TreeView"</c>, <c>"Chart"</c>).
    /// Use this to distinguish specific shell variants at runtime.
    /// </summary>
    public string SubType => GetString("SubType");

    /// <inheritdoc/>
    public override string ToString() => $"Shell [{Id}] SubType=\"{SubType}\"";
}
