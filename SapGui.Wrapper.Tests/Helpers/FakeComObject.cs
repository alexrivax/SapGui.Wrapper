namespace SapGui.Wrapper.Tests.Helpers;

/// <summary>
/// Minimal stand-in for a SAP COM component object.
/// Because <see cref="GuiComponent"/> uses late-binding via
/// <c>Type.InvokeMember</c>, any ordinary .NET object whose public
/// properties match the expected SAP property names works as a fake.
/// </summary>
internal sealed class FakeComObject
{
    // ── Component identity ────────────────────────────────────────────────────
    public string Type       { get; set; } = string.Empty;
    public string Id         { get; set; } = "wnd[0]/usr/test";
    public string Text       { get; set; } = string.Empty;
    public string Tooltip    { get; set; } = string.Empty;
    public string Name       { get; set; } = string.Empty;
    public bool   Changeable { get; set; } = true;
    public bool   Modified   { get; set; } = false;

    // ── Session info fields (used by FakeInfoObject) ──────────────────────────
    public string SystemName        { get; set; } = "TST";
    public string Client            { get; set; } = "800";
    public string User              { get; set; } = "TESTUSER";
    public string Language          { get; set; } = "EN";
    public string Transaction       { get; set; } = "SE16";
    public string Program           { get; set; } = "RSPO0010";
    public string ScreenNumber      { get; set; } = "0100";
    public string ApplicationServer { get; set; } = "server.example.com";

    // ── Status bar / message fields ───────────────────────────────────────────
    public string MessageType { get; set; } = string.Empty;

    // ── Combo box / selection fields ──────────────────────────────────────────
    public string Key   { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    // ── CheckBox / RadioButton ────────────────────────────────────────────────
    public bool Selected { get; set; } = false;

    // ── TextField ─────────────────────────────────────────────────────────────
    public int  MaxLength  { get; set; } = 100;
    public bool IsReadOnly { get; set; } = false;

    // ── GuiGridView ───────────────────────────────────────────────────────────
    public int    RowCount          { get; set; } = 0;
    public int    FirstVisibleRow   { get; set; } = 0;
    public int    VisibleRowCount   { get; set; } = 0;
    public int    CurrentCellRow    { get; set; } = 0;
    public string CurrentCellColumn { get; set; } = string.Empty;
    public string SelectedRows      { get; set; } = string.Empty;

    // ── GuiTable ──────────────────────────────────────────────────────────────
    public int ColumnCount { get; set; } = 0;

    // ── GuiMessageWindow buttons ──────────────────────────────────────────────
    private readonly List<FakeButtonObj> _buttons = new();

    /// <summary>
    /// Records the last VKey index sent via <c>SendVKey</c>
    /// (e.g. <c>ClickOk</c> sends <c>0</c>).
    /// </summary>
    public int? LastSentVKey { get; private set; }

    /// <summary>Adds a fake button child and returns <c>this</c> for fluent chaining.</summary>
    public FakeComObject WithButton(string text)
    {
        _buttons.Add(new FakeButtonObj { Text = text });
        return this;
    }

    /// <summary>
    /// Returns all fake button children as a <see cref="FakeChildrenCollection"/>.
    /// Called via <c>Invoke("Children")</c> (BindingFlags.InvokeMethod) by
    /// <see cref="GuiMessageWindow.GetButtons"/>.
    /// </summary>
    public FakeChildrenCollection Children() => new(_buttons);

    /// <summary>Records a SendVKey call so tests can assert the VKey that was sent.</summary>
    public void SendVKey(int vKey) => LastSentVKey = vKey;

    /// <summary>
    /// Creates a fake with the given SAP type string so that
    /// <see cref="GuiSession.WrapComponent"/> routes it correctly.
    /// </summary>
    public static FakeComObject OfType(string sapType, string id = "wnd[0]/usr/test") =>
        new() { Type = sapType, Id = id };
}

/// <summary>
/// Fake SAP children collection returned by <see cref="FakeComObject.Children"/>.
/// Implements the <c>Count</c>/<c>Item</c> protocol expected by
/// <see cref="GuiMessageWindow.GetButtons"/>.
/// </summary>
internal sealed class FakeChildrenCollection
{
    private readonly List<FakeButtonObj> _items;

    internal FakeChildrenCollection(IEnumerable<FakeButtonObj> items) =>
        _items = items.ToList();

    /// <summary>Number of children (read via BindingFlags.GetProperty).</summary>
    public int Count => _items.Count;

    /// <summary>Child by index (invoked via BindingFlags.InvokeMethod).</summary>
    public FakeButtonObj Item(int index) => _items[index];
}

/// <summary>
/// Fake SAP button object used as a stand-in for <c>GuiButton</c> raw COM objects
/// inside <see cref="FakeChildrenCollection"/>.
/// </summary>
internal sealed class FakeButtonObj
{
    public string Type       { get; set; } = "GuiButton";
    public string Text       { get; set; } = string.Empty;

    /// <summary>Set to <see langword="true"/> when <see cref="Press"/> is called.</summary>
    public bool WasPressed   { get; private set; }

    /// <summary>Records that this button was pressed.</summary>
    public void Press()      => WasPressed = true;
}
