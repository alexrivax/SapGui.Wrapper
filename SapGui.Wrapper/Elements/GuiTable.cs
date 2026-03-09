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
    public int ColumnCount
    {
        get
        {
            try
            {
                var cols = Invoke("Columns");
                if (cols is null) return 0;
                return (int)(cols.GetType()
                                 .InvokeMember("Count",
                                               BindingFlags.GetProperty,
                                               null, cols, null) ?? 0);
            }
            catch { return 0; }
        }
    }

    /// <summary>Index of the first visible row in the current scroll position (0-based).</summary>
    public int FirstVisibleRow => GetInt("FirstVisibleRow");

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
    /// The position is clamped to <c>RowCount − VisibleRowCount</c> (SAP's scrollbar maximum).
    /// </summary>
    public void ScrollToRow(int row)
    {
        int max = Math.Max(0, RowCount - VisibleRowCount);
        int pos = Math.Max(0, Math.Min(row, max));
        try { SetProperty("FirstVisibleRow", pos); }
        catch { }
    }

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
    /// Reads all currently rendered (visible) rows into a list of string arrays.
    /// Only the rows in the current viewport are returned; SAP does not populate
    /// off-screen rows in the COM tree.
    /// </summary>
    public List<string[]> GetVisibleRows()
    {
        var rows = new List<string[]>();
        int cols  = ColumnCount;
        int start = FirstVisibleRow;
        int end   = Math.Min(start + VisibleRowCount, RowCount);
        for (int r = start; r < end; r++)
        {
            var row = new string[cols];
            for (int c = 0; c < cols; c++)
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
            return Invoke("GetCell", row, column);
        }
        catch { return null; }
    }
}
