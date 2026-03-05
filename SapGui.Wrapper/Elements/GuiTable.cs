namespace SapGui.Wrapper;

/// <summary>
/// Wraps a classic ABAP-rendered SAP GUI table control (GuiTable).
/// For ALV grids use <see cref="GuiGridView"/> instead.
/// </summary>
public class GuiTable : GuiComponent
{
    internal GuiTable(object raw) : base(raw) { }

    /// <summary>Total row count (may include off-screen rows).</summary>
    public int RowCount     => GetInt("RowCount");

    /// <summary>Number of columns.</summary>
    public int ColumnCount  => GetInt("ColumnCount");

    /// <summary>Index of the first visible row in the current scroll position (0-based).</summary>
    public int FirstVisibleRow => GetInt("VerticalScrollbar.Position");

    /// <summary>Number of rows currently visible in the table's viewport.</summary>
    public int VisibleRowCount => GetInt("VisibleRowCount");

    /// <summary>
    /// Key (column name) of the currently focused cell.
    /// Returns an empty string when no cell is focused.
    /// </summary>
    public string CurrentCellColumn
    {
        get
        {
            try
            {
                var cc = Invoke("CurrentCell");
                if (cc is null) return string.Empty;
                return (string?)cc.GetType()
                                  .InvokeMember("ColumnId",
                                                BindingFlags.GetProperty,
                                                null, cc, null) ?? string.Empty;
            }
            catch { return string.Empty; }
        }
    }

    /// <summary>
    /// Row index (0-based) of the currently focused cell.
    /// Returns -1 when no cell is focused.
    /// </summary>
    public int CurrentCellRow
    {
        get
        {
            try
            {
                var cc = Invoke("CurrentCell");
                if (cc is null) return -1;
                return (int)(cc.GetType()
                               .InvokeMember("Row",
                                             BindingFlags.GetProperty,
                                             null, cc, null) ?? -1);
            }
            catch { return -1; }
        }
    }

    /// <summary>
    /// Scrolls the table so that <paramref name="row"/> is the first visible row.
    /// </summary>
    public void ScrollToRow(int row) =>
        SetProperty("VerticalScrollbar.Position", row);

    /// <summary>Current visible top row (0-based). Alias for <see cref="FirstVisibleRow"/>.</summary>
    [Obsolete("Use FirstVisibleRow instead.")]
    public int VerticalScrollbarPosition => FirstVisibleRow;

    // ── Cell access ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the raw text value of a cell.
    /// Row is 0-based. Column is 0-based.
    /// </summary>
    public string GetCellValue(int row, int column)
    {
        var cell = GetCellRaw(row, column);
        if (cell is null) return string.Empty;

        return (string?)cell.GetType()
                             .InvokeMember("Text",
                                           BindingFlags.GetProperty,
                                           null, cell, null) ?? string.Empty;
    }

    /// <summary>Sets a cell text value (if the cell is editable).</summary>
    public void SetCellValue(int row, int column, string value)
    {
        var cell = GetCellRaw(row, column);
        if (cell is null) return;

        cell.GetType().InvokeMember("Text",
                                    BindingFlags.SetProperty,
                                    null, cell,
                                    new object[] { value });
    }

    /// <summary>
    /// Reads all visible rows into a list of string arrays.
    /// Scrolling is NOT performed; you will only get the currently rendered rows.
    /// </summary>
    public List<string[]> GetVisibleRows()
    {
        var rows = new List<string[]>();
        for (int r = 0; r < RowCount; r++)
        {
            var row = new string[ColumnCount];
            for (int c = 0; c < ColumnCount; c++)
                row[c] = GetCellValue(r, c);
            rows.Add(row);
        }
        return rows;
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    /// <summary>Selects a row (0-based).</summary>
    public void SelectRow(int row) => Invoke("SetCurrentCell", row, "");

    // ── Internal ──────────────────────────────────────────────────────────────

    private object? GetCellRaw(int row, int column)
    {
        try
        {
            var rows = Invoke("Rows");
            if (rows is null) return null;

            var rowObj = rows.GetType()
                             .InvokeMember("Item",
                                           BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                                           null, rows,
                                           new object[] { row });
            if (rowObj is null) return null;

            var cells = rowObj.GetType()
                              .InvokeMember("Item",
                                            BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                                            null, rowObj,
                                            new object[] { column });
            return cells;
        }
        catch { return null; }
    }
}
