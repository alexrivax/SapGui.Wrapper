using System.Text;
using System.Text.Json;

namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Complete point-in-time snapshot of the active SAP screen.
/// Serialisable to JSON and to a compact plain-text context block for LLM consumption.
/// </summary>
public sealed class SapScreenSnapshot
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Active transaction code, e.g. <c>MM60</c>.</summary>
    public string         Transaction { get; init; } = string.Empty;

    /// <summary>Main window title bar text.</summary>
    public string         WindowTitle { get; init; } = string.Empty;

    /// <summary>SAP system ID (SID), e.g. <c>PRD</c>.</summary>
    public string         SystemName  { get; init; } = string.Empty;

    /// <summary>Logged-on user name.</summary>
    public string         UserName    { get; init; } = string.Empty;

    /// <summary>SAP client number, e.g. <c>100</c>.</summary>
    public string         Client      { get; init; } = string.Empty;

    /// <summary>Detected screen type.</summary>
    public SapScreenType  ScreenType  { get; init; }

    /// <summary>UTC timestamp of the scan.</summary>
    public DateTimeOffset CapturedAt  { get; init; }

    // ── Status ────────────────────────────────────────────────────────────────

    /// <summary>Status bar state at the time of the scan.</summary>
    public SapStatusbarSnapshot Statusbar  { get; init; } = new();

    /// <summary>Whether the status bar is showing an error.</summary>
    public bool HasError   => Statusbar.IsError;

    /// <summary>Whether the status bar is showing a warning.</summary>
    public bool HasWarning => Statusbar.IsWarning;

    // ── Interactive elements ──────────────────────────────────────────────────

    /// <summary>All input fields and labels detected on the screen.</summary>
    public IReadOnlyList<SapFieldSnapshot>    Fields    { get; init; } = Array.Empty<SapFieldSnapshot>();

    /// <summary>All push buttons and toolbar buttons detected on the screen.</summary>
    public IReadOnlyList<SapButtonSnapshot>   Buttons   { get; init; } = Array.Empty<SapButtonSnapshot>();

    /// <summary>All ALV grid controls detected on the screen.</summary>
    public IReadOnlyList<SapGridSnapshot>     Grids     { get; init; } = Array.Empty<SapGridSnapshot>();

    /// <summary>All tab strip controls detected on the screen.</summary>
    public IReadOnlyList<SapTabStripSnapshot> TabStrips { get; init; } = Array.Empty<SapTabStripSnapshot>();

    /// <summary>All tree controls detected on the screen.</summary>
    public IReadOnlyList<SapTreeSnapshot>     Trees     { get; init; } = Array.Empty<SapTreeSnapshot>();

    /// <summary>Top-level menu bar entries.</summary>
    public IReadOnlyList<SapMenuSnapshot>     Menus     { get; init; } = Array.Empty<SapMenuSnapshot>();

    // ── Popups ────────────────────────────────────────────────────────────────

    /// <summary>All active popup/modal windows (wnd[1..n]).</summary>
    public IReadOnlyList<SapPopupSnapshot>    Popups    { get; init; } = Array.Empty<SapPopupSnapshot>();

    /// <summary>Whether at least one popup window is currently active.</summary>
    public bool HasPopup => Popups.Count > 0;

    // ── Screenshot ────────────────────────────────────────────────────────────

    /// <summary>
    /// Base64-encoded PNG screenshot, or <see langword="null"/> if no screenshot was taken.
    /// Populated only when <c>withScreenshot: true</c> is passed to
    /// <see cref="GuiSessionScanExtensions.ScanScreen"/>.
    /// </summary>
    public string? ScreenshotBase64 { get; init; }

    // ── LLM serialisation ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns a compact plain-text representation suitable for inclusion in an LLM
    /// system prompt or user turn.  By default element IDs are omitted so the agent
    /// works by label; pass <paramref name="includeIds"/> = <see langword="true"/> when the
    /// agent needs to reference specific COM paths.
    /// </summary>
    public string ToAgentContext(bool includeIds = false)
    {
        var sb = new StringBuilder(512);
        const string divider = "─────────────────────────────────────────────────────────────";

        // ── Header ────────────────────────────────────────────────────────────
        sb.AppendLine("── SAP SCREEN " + divider);
        sb.AppendLine($"Transaction : {Transaction,-16} Screen : {WindowTitle}");
        sb.AppendLine($"System      : {SystemName,-16} User   : {UserName,-12} Client : {Client}");

        var statusPrefix = HasError ? "[ERROR]" : HasWarning ? "[WARN]" : "[OK]";
        sb.AppendLine($"Status      : {statusPrefix} {Statusbar.Text}");

        // ── Active popup ──────────────────────────────────────────────────────
        if (HasPopup)
        {
            sb.AppendLine();
            sb.AppendLine("── ACTIVE POPUP " + divider[..Math.Min(divider.Length, 45)]);
            foreach (var popup in Popups)
            {
                var typeTag = string.IsNullOrEmpty(popup.MessageType)
                    ? ""
                    : $"[{popup.MessageType}] ";
                var title = string.IsNullOrEmpty(popup.Title) ? "" : $"\"{popup.Title}\" ";
                sb.AppendLine($"{typeTag}{title}{popup.Message}".Trim());
                if (popup.Buttons.Count > 0)
                {
                    var btnList = string.Join("  ", popup.Buttons.Select(b =>
                        $"[{(string.IsNullOrEmpty(b.Text) ? b.Tooltip : b.Text)}]"));
                    sb.AppendLine($"Buttons: {btnList}");
                }
            }
        }

        // ── Input fields ──────────────────────────────────────────────────────
        var interactiveFields = Fields.Where(f => f.FieldType != "Label").ToList();
        sb.AppendLine();
        sb.AppendLine("── INPUT FIELDS " + divider[..Math.Min(divider.Length, 45)]);
        if (interactiveFields.Count == 0)
        {
            sb.AppendLine("(none visible)");
        }
        else
        {
            foreach (var f in interactiveFields)
            {
                var reqTag   = f.IsRequired ? "(required)" : "(optional)";
                var roTag    = f.IsReadOnly ? " [read-only]" : "";
                var valueBox = f.IsReadOnly ? $"[{f.Value}]" : $"[{f.Value,-10}]";

                string extra = f.FieldType switch
                {
                    "ComboBox" when f.ComboOptions.Count > 0
                        => $"  options: {string.Join(",", f.ComboOptions)}",
                    "CheckBox" or "RadioButton"
                        => $"  checked={f.Value}",
                    _   => string.Empty
                };

                if (includeIds)
                    sb.AppendLine($"{f.Label,-20} {reqTag,-10} {valueBox}  {f.FieldType}{roTag}{extra}  id={f.Id}");
                else
                    sb.AppendLine($"{f.Label,-20} {reqTag,-10} {valueBox}  {f.FieldType}{roTag}{extra}");
            }
        }

        // ── Buttons ───────────────────────────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("── BUTTONS " + divider[..Math.Min(divider.Length, 50)]);
        if (Buttons.Count == 0)
        {
            sb.AppendLine("(none visible)");
        }
        else
        {
            var btnParts = Buttons.Select(b =>
            {
                var lbl = string.IsNullOrEmpty(b.Text) ? b.Tooltip : b.Text;
                var tt  = !string.IsNullOrEmpty(b.Tooltip) && b.Tooltip != b.Text
                    ? $" ({b.Tooltip})"
                    : string.Empty;
                var dis = b.IsEnabled ? string.Empty : " [disabled]";
                return $"[{lbl}{tt}]{dis}";
            });
            sb.AppendLine(string.Join("  ", btnParts));
        }

        // ── Grids ─────────────────────────────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("── GRIDS " + divider[..Math.Min(divider.Length, 52)]);
        if (Grids.Count == 0)
        {
            sb.AppendLine("(none visible)");
        }
        else
        {
            foreach (var grid in Grids)
            {
                var moreTag = grid.HasMoreRows
                    ? $" [+{grid.TotalRowCount - grid.VisibleRowCount} more rows]"
                    : string.Empty;
                sb.AppendLine($"{grid.VisibleRowCount} of {grid.TotalRowCount} rows{moreTag}  cols: {string.Join(", ", grid.ColumnNames)}");
                if (includeIds)
                    sb.AppendLine($"  id={grid.Id}");

                // Print first few rows as a sample
                foreach (var row in grid.VisibleRows.Take(5))
                {
                    var cells = string.Join(" | ", grid.ColumnNames.Select(c =>
                        row.Cells.TryGetValue(c, out var v) ? v : string.Empty));
                    sb.AppendLine($"  [{row.RowIndex}] {cells}");
                }
                if (grid.VisibleRows.Count > 5)
                    sb.AppendLine($"  ... ({grid.VisibleRows.Count - 5} more visible rows)");
            }
        }

        // ── Tabs ──────────────────────────────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("── TABS " + divider[..Math.Min(divider.Length, 53)]);
        if (TabStrips.Count == 0)
        {
            sb.AppendLine("(none visible)");
        }
        else
        {
            foreach (var ts in TabStrips)
            {
                var tabs = string.Join("  ", ts.Tabs.Select(t =>
                    t.IsSelected ? $"[>{t.Text}<]" : $"[{t.Text}]"));
                sb.AppendLine(tabs);
                if (includeIds)
                    sb.AppendLine($"  id={ts.Id}");
            }
        }

        // ── Trees ─────────────────────────────────────────────────────────────
        if (Trees.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── TREES " + divider[..Math.Min(divider.Length, 52)]);
            foreach (var tree in Trees)
            {
                foreach (var node in tree.VisibleNodes.Take(10))
                {
                    var indent  = new string(' ', node.Level * 2);
                    var expand  = node.HasChildren ? (node.IsExpanded ? "▼" : "►") : " ";
                    var selMark = node.IsSelected ? "*" : " ";
                    sb.AppendLine($"{selMark} {indent}{expand} {node.Text}");
                }
                if (tree.VisibleNodes.Count > 10)
                    sb.AppendLine($"  ... ({tree.VisibleNodes.Count - 10} more nodes)");
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Serialises the full snapshot to a JSON string using
    /// <see cref="System.Text.Json.JsonSerializer"/>.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
    }

    /// <summary>
    /// Produces a compact diff summary describing what changed between
    /// <paramref name="previous"/> and this snapshot.
    /// Used by the agent loop to confirm what an action achieved.
    /// </summary>
    /// <param name="previous">The snapshot taken before the action.</param>
    public string DiffFrom(SapScreenSnapshot previous)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CHANGES AFTER ACTION:");

        // Status bar
        if (previous.Statusbar.Text != Statusbar.Text ||
            previous.Statusbar.MessageType != Statusbar.MessageType)
        {
            sb.AppendLine(
                $"~ Status : [{previous.Statusbar.MessageType}] \"{previous.Statusbar.Text}\" " +
                $"→ [{Statusbar.MessageType}] \"{Statusbar.Text}\"");
        }

        // Transaction
        if (previous.Transaction != Transaction)
            sb.AppendLine($"~ Transaction : \"{previous.Transaction}\" → \"{Transaction}\"");

        // Window title
        if (previous.WindowTitle != WindowTitle)
            sb.AppendLine($"~ Screen : \"{previous.WindowTitle}\" → \"{WindowTitle}\"");

        // Screen type
        if (previous.ScreenType != ScreenType)
            sb.AppendLine($"~ ScreenType : {previous.ScreenType} → {ScreenType}");

        // Fields
        var prevFieldMap = previous.Fields.ToDictionary(f => f.Id);
        var curFieldMap  = Fields.ToDictionary(f => f.Id);

        foreach (var (id, cur) in curFieldMap)
        {
            if (prevFieldMap.TryGetValue(id, out var prev))
            {
                if (prev.Value != cur.Value)
                    sb.AppendLine($"~ Field \"{cur.Label}\" : \"{prev.Value}\" → \"{cur.Value}\"");
            }
            else
            {
                sb.AppendLine($"+ Field appeared: \"{cur.Label}\" = \"{cur.Value}\"");
            }
        }

        foreach (var (id, prev) in prevFieldMap)
        {
            if (!curFieldMap.ContainsKey(id))
                sb.AppendLine($"- Field removed: \"{prev.Label}\"");
        }

        // Grids
        int prevGrids = previous.Grids.Count;
        int curGrids  = Grids.Count;
        if (prevGrids == 0 && curGrids > 0)
        {
            foreach (var g in Grids)
                sb.AppendLine($"+ Grid appeared: {g.TotalRowCount} row(s), {g.ColumnNames.Count} col(s)");
        }
        else if (prevGrids > 0 && curGrids == 0)
        {
            sb.AppendLine("- Grid(s) disappeared");
        }
        else
        {
            for (int i = 0; i < Math.Min(prevGrids, curGrids); i++)
            {
                if (previous.Grids[i].TotalRowCount != Grids[i].TotalRowCount)
                    sb.AppendLine(
                        $"~ Grid rows: {previous.Grids[i].TotalRowCount} → {Grids[i].TotalRowCount}");
            }
        }

        // Popups
        if (previous.Popups.Count == 0 && Popups.Count > 0)
        {
            foreach (var p in Popups)
                sb.AppendLine($"+ Popup appeared [{p.WindowId}]: \"{p.Title}\" {p.Message}".Trim());
        }
        else if (previous.Popups.Count > 0 && Popups.Count == 0)
        {
            sb.AppendLine("- Popup(s) dismissed");
        }

        var result = sb.ToString().TrimEnd();
        if (result == "CHANGES AFTER ACTION:")
            result += "\n  (no observable changes)";
        return result;
    }
}
