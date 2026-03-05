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

    /// <summary>
    /// Creates a fake with the given SAP type string so that
    /// <see cref="GuiSession.WrapComponent"/> routes it correctly.
    /// </summary>
    public static FakeComObject OfType(string sapType, string id = "wnd[0]/usr/test") =>
        new() { Type = sapType, Id = id };
}
