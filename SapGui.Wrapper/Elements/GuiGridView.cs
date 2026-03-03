namespace SapGui.Wrapper;

/// <summary>
/// Wraps an ALV Grid View control (GuiGridView).
/// This is the most common grid type in modern SAP transactions.
/// </summary>
public class GuiGridView : GuiComponent
{
    internal GuiGridView(object raw) : base(raw) { }

    // ── Dimensions ────────────────────────────────────────────────────────────

    /// <summary>Number of data rows.</summary>
    public int RowCount => GetInt("RowCount");

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
}
