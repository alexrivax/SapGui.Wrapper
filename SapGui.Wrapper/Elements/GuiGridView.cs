namespace SapGui.Wrapper;

/// <summary>
/// Wraps an ALV Grid View control (GuiGridView).
/// This is the most common grid type in modern SAP transactions.
/// </summary>
public class GuiGridView : GuiComponent
{
    internal GuiGridView(object raw) : base(raw) { }

    // ── Dimensions ────────────────────────────────────────────────────────────

    /// <summary>Total number of data rows.</summary>
    public int RowCount => GetInt("RowCount");

    /// <summary>Index of the first row currently visible in the viewport (0-based).</summary>
    public int FirstVisibleRow => GetInt("FirstVisibleRow");

    /// <summary>Number of rows currently visible in the viewport.</summary>
    public int VisibleRowCount => GetInt("VisibleRowCount");

    /// <summary>Column names/keys available in this grid.</summary>
    public IReadOnlyList<string> ColumnNames
    {
        get
        {
            var cols = Invoke("ColumnOrder");
            if (cols is null) return Array.Empty<string>();

            var t     = cols.GetType();
            int count = (int)(t.InvokeMember("Count",
                                              BindingFlags.GetProperty,
                                              null, cols, null) ?? 0);
            var result = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                var name = (string?)t.InvokeMember("Item",
                                                    BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                                                    null, cols,
                                                    new object[] { i }) ?? string.Empty;
                result.Add(name);
            }
            return result;
        }
    }

    // ── Current cell ──────────────────────────────────────────────────────────

    /// <summary>Row index (0-based) of the currently focused cell.</summary>
    public int CurrentCellRow => GetInt("CurrentCellRow");

    /// <summary>Column key of the currently focused cell.</summary>
    public string CurrentCellColumn => GetString("CurrentCellColumn");

    /// <summary>
    /// Moves focus to the specified cell.
    /// Must be called before right-clicking, reading a tooltip, or reading a
    /// checkbox-type cell reliably.
    /// </summary>
    public void SetCurrentCell(int row, string columnName) =>
        Invoke("SetCurrentCell", row, columnName);

    // ── Cell access ───────────────────────────────────────────────────────────

    /// <summary>Returns the cell value at (row, columnName).</summary>
    public string GetCellValue(int row, string columnName)
    {
        try
        {
            return (string?)Invoke("GetCellValue", row, columnName) ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    /// <summary>Sets the cell value at (row, columnName).</summary>
    public void SetCellValue(int row, string columnName, string value) =>
        Invoke("ModifyCell", row, columnName, value);

    /// <summary>
    /// Reads all rows for the specified columns into a list of dictionaries.
    /// Scrolling is NOT performed automatically.
    /// </summary>
    public List<Dictionary<string, string>> GetRows(IEnumerable<string>? columns = null)
    {
        var cols   = columns?.ToList() ?? ColumnNames.ToList();
        var result = new List<Dictionary<string, string>>(RowCount);

        for (int r = 0; r < RowCount; r++)
        {
            var dict = new Dictionary<string, string>(cols.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var col in cols)
                dict[col] = GetCellValue(r, col);
            result.Add(dict);
        }
        return result;
    }

    /// <summary>
    /// Returns the tooltip text for the specified cell.
    /// </summary>
    public string GetCellTooltip(int row, string columnName)
    {
        try { return (string?)Invoke("GetCellTooltip", row, columnName) ?? string.Empty; }
        catch { return string.Empty; }
    }

    /// <summary>
    /// Returns the boolean value of a checkbox-type cell.
    /// </summary>
    public bool GetCellCheckBoxValue(int row, string columnName)
    {
        try { return (bool)(Invoke("GetCellCheckBoxValue", row, columnName) ?? false); }
        catch { return false; }
    }

    /// <summary>
    /// Returns the symbol/icon key for an icon-type column cell.
    /// </summary>
    public string GetSymbolsForCell(int row, string columnName)
    {
        try { return (string?)Invoke("GetSymbolsForCell", row, columnName) ?? string.Empty; }
        catch { return string.Empty; }
    }

    /// <summary>Confirms a cell edit by sending Enter to the grid.</summary>
    public void PressEnter() => Invoke("PressEnter");

    // ── Selection ─────────────────────────────────────────────────────────────

    /// <summary>Selects a single row (0-based).</summary>
    public void SelectRow(int row)                => Invoke("SetCurrentCell", row, "");

    /// <summary>Clicks a cell (e.g. to follow a tree-node or hyperlink).</summary>
    public void ClickCell(int row, string column) => Invoke("Click", row, column);

    /// <summary>Double-clicks a cell.</summary>
    public void DoubleClickCell(int row, string column) => Invoke("DoubleClick", row, column);

    // ── Toolbar / context menu ────────────────────────────────────────────────

    /// <summary>Presses a toolbar button by its function code string.</summary>
    public void PressToolbarButton(string functionCode) => Invoke("PressToolbarButton", functionCode);

    /// <summary>Selects all rows.</summary>
    public void SelectAll() => Invoke("SelectAll");

    /// <summary>
    /// Returns the list of currently selected row indices (0-based).
    /// </summary>
    public IReadOnlyList<int> SelectedRows
    {
        get
        {
            try
            {
                var sel = Invoke("SelectedRows");
                if (sel is null) return Array.Empty<int>();

                // SAP returns a comma-separated string like "0,1,3"
                if (sel is string s)
                    return s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(x => int.TryParse(x.Trim(), out var n) ? n : -1)
                             .Where(n => n >= 0)
                             .ToList();

                return Array.Empty<int>();
            }
            catch { return Array.Empty<int>(); }
        }
    }
}
