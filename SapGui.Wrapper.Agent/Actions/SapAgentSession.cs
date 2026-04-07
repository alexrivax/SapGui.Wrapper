using SapGui.Wrapper.Agent.Observation;

namespace SapGui.Wrapper.Agent.Actions;

/// <summary>
/// Semantic façade over <see cref="GuiSession"/> for AI agents.
/// <para>
/// Every method accepts a semantic target (a human-readable label, visible button text,
/// tab name, etc.) instead of a raw SAP element ID, resolves it against the current screen
/// snapshot, executes the action via the underlying typed wrappers, and returns a
/// <see cref="SapActionResult"/> containing before/after snapshots and a compact diff.
/// </para>
/// <para>
/// <b>Threading:</b> All SAP COM calls are synchronous (STA). Do not wrap these methods with
/// <c>Task.Run</c>. The <see cref="GuiSession"/> must be used on the thread that created it.
/// </para>
/// </summary>
public sealed class SapAgentSession
{
    private readonly GuiSession _session;

    /// <summary>Creates an agent session wrapping the given <paramref name="session"/>.</summary>
    public SapAgentSession(GuiSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    // ── Observation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Captures a complete snapshot of the current SAP screen without making any changes.
    /// </summary>
    public SapScreenSnapshot ScanScreen(bool withScreenshot = false) =>
        _session.ScanScreen(withScreenshot);

    // ── Field operations ───────────────────────────────────────────────────────

    /// <summary>
    /// Sets a field value by its label or element ID.
    /// Handles <c>TextField</c>, <c>ComboBox</c>, <c>CheckBox</c>, and <c>RadioButton</c> types.
    /// </summary>
    /// <param name="labelOrId">Field label (e.g. "Plant") or full COM path.</param>
    /// <param name="value">
    /// Value to set. For ComboBox, provide the key or display text.
    /// For CheckBox, use "X" / "true" / "1" to check, anything else to uncheck.
    /// </param>
    public SapActionResult SetField(string labelOrId, string value)
    {
        var before = _session.ScanScreen();
        try
        {
            var field = FieldFinder.Resolve(labelOrId, before);
            ApplyFieldValue(field, value);
            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after, field.Id);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    /// <summary>
    /// Reads the current value of a field by its label or element ID.
    /// Returns a read-only <see cref="SapActionResult"/> (no after-snapshot).
    /// </summary>
    public SapActionResult GetField(string labelOrId)
    {
        var before = _session.ScanScreen();
        try
        {
            var field = FieldFinder.Resolve(labelOrId, before);
            return SapActionResult.OkReadOnly(before, field.Id);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    /// <summary>
    /// Clears a field to empty by its label or element ID.
    /// </summary>
    public SapActionResult ClearField(string labelOrId)
        => SetField(labelOrId, string.Empty);

    // ── Button / key operations ───────────────────────────────────────────────

    /// <summary>
    /// Clicks a button by its visible text, tooltip, function code, or element ID.
    /// Searches both the main screen and any active popups.
    /// </summary>
    public SapActionResult ClickButton(string textOrId)
    {
        var before = _session.ScanScreen();
        try
        {
            var button = ButtonFinder.Resolve(textOrId, before);
            _session.FindById<GuiButton>(button.Id).Press();
            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after, button.Id);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    /// <summary>
    /// Sends a named keyboard shortcut to the SAP main window.
    /// </summary>
    public SapActionResult PressKey(SapKeyAction key)
    {
        var before = _session.ScanScreen();
        try
        {
            _session.SendVKey(MapVKey(key));
            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    // ── Navigation ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Navigates to a SAP transaction code.
    /// Automatically prefixes with <c>/n</c> when already inside a transaction
    /// so the navigation is unconditional.
    /// </summary>
    public SapActionResult StartTransaction(string tCode)
    {
        var before = _session.ScanScreen();
        try
        {
            // Prefix /n only when currently inside a transaction (not at the menu screen)
            var target = !string.IsNullOrWhiteSpace(before.Transaction)
                         && !before.Transaction.Equals("SESSION_MANAGER", StringComparison.OrdinalIgnoreCase)
                         && !tCode.StartsWith("/", StringComparison.Ordinal)
                ? "/n" + tCode
                : tCode;

            _session.StartTransaction(target);
            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    /// <summary>
    /// Navigates a menu by a slash-separated path of label segments,
    /// e.g. <c>"System / User Profile / Own Data"</c>.
    /// </summary>
    public SapActionResult SelectMenu(string menuPath)
    {
        var before = _session.ScanScreen();
        try
        {
            var segments = menuPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            if (segments.Length == 0)
                return SapActionResult.Fail("Menu path must not be empty.", before);

            var menuId = ResolveMenuPath(segments, before);
            _session.Menu(menuId).Select();

            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after, menuId);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    // ── Tab operations ────────────────────────────────────────────────────────

    /// <summary>
    /// Selects a tab by its visible label (case-insensitive, partial match allowed).
    /// Searches all tab strips on the current screen.
    /// </summary>
    public SapActionResult SelectTab(string tabName)
    {
        var before = _session.ScanScreen();
        try
        {
            SapTabSnapshot? found = null;
            foreach (var strip in before.TabStrips)
            {
                var tab = strip.Tabs.FirstOrDefault(
                    t => t.Text.Equals(tabName, StringComparison.OrdinalIgnoreCase));
                tab ??= strip.Tabs.FirstOrDefault(
                    t => t.Text.Contains(tabName, StringComparison.OrdinalIgnoreCase));
                if (tab is not null) { found = tab; break; }
            }

            if (found is null)
            {
                var available = before.TabStrips
                    .SelectMany(s => s.Tabs)
                    .Select(t => t.Text)
                    .ToArray();
                throw new SapAgentResolutionException(tabName, "tab", available);
            }

            _session.FindById<GuiTab>(found.Id).Select();
            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after, found.Id);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    // ── Grid operations ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns all visible rows from the specified ALV grid.
    /// The snapshot before the read is included in the result.
    /// </summary>
    /// <param name="gridIndex">Zero-based index of the grid on the current screen.</param>
    /// <param name="columns">
    /// Optional filter — only return data for these columns.
    /// When <see langword="null"/>, all columns are returned.
    /// </param>
    public SapActionResult ReadGrid(int gridIndex = 0, string[]? columns = null)
    {
        var before = _session.ScanScreen();
        try
        {
            if (before.Grids.Count == 0)
                return SapActionResult.Fail("No grids found on the current screen.", before);

            if (gridIndex < 0 || gridIndex >= before.Grids.Count)
                return SapActionResult.Fail(
                    $"Grid index {gridIndex} is out of range (0–{before.Grids.Count - 1}).", before);

            var grid = before.Grids[gridIndex];
            return SapActionResult.OkReadOnly(before, grid.Id);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    /// <summary>
    /// Sets the current row on an ALV grid (equivalent to selecting / clicking a row).
    /// </summary>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="gridIndex">Zero-based grid index. Default: 0.</param>
    public SapActionResult SelectGridRow(int rowIndex, int gridIndex = 0)
    {
        var before = _session.ScanScreen();
        try
        {
            var gridId = ResolveGridId(before, gridIndex);
            _session.GridView(gridId).SelectRow(rowIndex);
            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after, gridId);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    /// <summary>
    /// Double-clicks a row in an ALV grid to open its detail record.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="gridIndex">Zero-based grid index. Default: 0.</param>
    public SapActionResult OpenGridRow(int rowIndex, int gridIndex = 0)
    {
        var before = _session.ScanScreen();
        try
        {
            var gridId = ResolveGridId(before, gridIndex);
            var grid = _session.GridView(gridId);

            // Double-click on the first available column to open the row detail
            var col = grid.ColumnNames.FirstOrDefault() ?? string.Empty;
            grid.DoubleClickCell(rowIndex, col);

            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after, gridId);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    // ── Popup handling ────────────────────────────────────────────────────────

    /// <summary>
    /// Responds to the active SAP popup dialog.
    /// </summary>
    /// <param name="action">How to respond — OK, Cancel, Yes, No, or by specific button text.</param>
    /// <param name="buttonText">Required when <paramref name="action"/> is <see cref="PopupAction.ByButtonText"/>.</param>
    public SapActionResult HandlePopup(PopupAction action, string? buttonText = null)
    {
        var before = _session.ScanScreen();
        try
        {
            var popup = _session.GetActivePopup();
            if (popup is null)
                return SapActionResult.Fail("No active popup found.", before);

            switch (action)
            {
                case PopupAction.Ok:
                    popup.ClickOk();
                    break;

                case PopupAction.Cancel:
                    popup.ClickCancel();
                    break;

                case PopupAction.Yes:
                    popup.ClickButton("Yes");
                    break;

                case PopupAction.No:
                    popup.ClickButton("No");
                    break;

                case PopupAction.ByButtonText:
                    if (string.IsNullOrWhiteSpace(buttonText))
                        return SapActionResult.Fail(
                            $"ByButtonText requires a non-empty {nameof(buttonText)}.", before);
                    popup.ClickButton(buttonText);
                    break;

                default:
                    return SapActionResult.Fail($"Unknown PopupAction: {action}", before);
            }

            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    // ── Tree operations ───────────────────────────────────────────────────────

    /// <summary>
    /// Expands a tree node by matching its display text (case-insensitive).
    /// Searches all tree controls on the current screen.
    /// </summary>
    public SapActionResult ExpandTreeNode(string nodeText)
    {
        var before = _session.ScanScreen();
        try
        {
            var (treeId, nodeKey) = ResolveTreeNode(nodeText, before);
            _session.Tree(treeId).ExpandNode(nodeKey);
            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after, $"{treeId}#{nodeKey}");
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    /// <summary>
    /// Selects a tree node by its display text, optionally double-clicking it.
    /// </summary>
    public SapActionResult SelectTreeNode(string nodeText, bool doubleClick = false)
    {
        var before = _session.ScanScreen();
        try
        {
            var (treeId, nodeKey) = ResolveTreeNode(nodeText, before);
            var tree = _session.Tree(treeId);

            tree.SelectNode(nodeKey);
            if (doubleClick) tree.DoubleClickNode(nodeKey);

            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after, $"{treeId}#{nodeKey}");
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    // ── Wait ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Waits for the SAP session to finish processing and returns a fresh screen snapshot.
    /// Use after any navigation or action that triggers server-side processing.
    /// </summary>
    /// <param name="timeoutMs">Maximum wait time in milliseconds. Default: 30 000.</param>
    public SapActionResult WaitAndScan(int timeoutMs = 30_000)
    {
        var before = _session.ScanScreen();
        try
        {
            _session.WaitForReadyState(timeoutMs);
            var after = _session.ScanScreen();
            return SapActionResult.Ok(before, after);
        }
        catch (Exception ex)
        {
            return SapActionResult.Fail(ex.Message, before);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void ApplyFieldValue(SapFieldSnapshot field, string value)
    {
        switch (field.FieldType)
        {
            case "ComboBox":
                var combo = _session.ComboBox(field.Id);
                // Try matching by key first; fall back to display value
                var matchByKey = combo.Entries.FirstOrDefault(
                    e => e.Key.Equals(value, StringComparison.OrdinalIgnoreCase));
                if (matchByKey != default)
                {
                    combo.Key = matchByKey.Key;
                }
                else
                {
                    var matchByValue = combo.Entries.FirstOrDefault(
                        e => e.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
                    combo.Key = matchByValue != default ? matchByValue.Key : value;
                }
                break;

            case "CheckBox":
                _session.CheckBox(field.Id).Selected =
                    value is "X" or "x" or "true" or "True" or "1" or "yes" or "Yes";
                break;

            case "RadioButton":
                _session.RadioButton(field.Id).Selected = true;
                break;

            default:
                // TextField, PasswordField, CTextField, output-only — use Text property
                _session.TextField(field.Id).Text = value;
                break;
        }
    }

    private static int MapVKey(SapKeyAction key) => key switch
    {
        SapKeyAction.Enter => 0,
        SapKeyAction.Back => 3,
        SapKeyAction.F4 => 4,
        SapKeyAction.Execute => 8,
        SapKeyAction.Save => 11,
        SapKeyAction.Cancel => 12,
        SapKeyAction.Exit => 15,
        SapKeyAction.CtrlHome => 70,
        SapKeyAction.ScrollTop => 71,
        SapKeyAction.ScrollBottom => 82,
        SapKeyAction.CtrlEnd => 83,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown SapKeyAction."),
    };

    private static string ResolveGridId(SapScreenSnapshot snapshot, int gridIndex)
    {
        if (snapshot.Grids.Count == 0)
            throw new InvalidOperationException("No grids found on the current screen.");
        if (gridIndex < 0 || gridIndex >= snapshot.Grids.Count)
            throw new IndexOutOfRangeException(
                $"Grid index {gridIndex} is out of range (0–{snapshot.Grids.Count - 1}).");
        return snapshot.Grids[gridIndex].Id;
    }

    private static (string TreeId, string NodeKey) ResolveTreeNode(
        string nodeText, SapScreenSnapshot snapshot)
    {
        foreach (var tree in snapshot.Trees)
        {
            var node = tree.VisibleNodes.FirstOrDefault(
                n => n.Text.Equals(nodeText, StringComparison.OrdinalIgnoreCase));
            node ??= tree.VisibleNodes.FirstOrDefault(
                n => n.Text.Contains(nodeText, StringComparison.OrdinalIgnoreCase));

            if (node is not null) return (tree.Id, node.NodeKey);
        }

        var candidates = snapshot.Trees
            .SelectMany(t => t.VisibleNodes)
            .Select(n => n.Text)
            .Distinct()
            .ToArray();

        throw new SapAgentResolutionException(nodeText, "tree node", candidates);
    }

    /// <summary>
    /// Resolves a slash-separated menu path to the SAP element ID of the leaf menu item
    /// by walking the live COM menu tree.
    /// </summary>
    private string ResolveMenuPath(string[] segments, SapScreenSnapshot snapshot)
    {
        // Walk the snapshot's menu tree to find matching IDs.
        // The snapshot menus have the element IDs we need — we just match by text.
        var roots = snapshot.Menus;
        SapMenuSnapshot? current = null;
        string? resolvedId = null;

        foreach (var segment in segments)
        {
            IReadOnlyList<SapMenuSnapshot> candidates = current?.Children ?? roots;

            var next = candidates.FirstOrDefault(
                m => m.Text.Equals(segment, StringComparison.OrdinalIgnoreCase));
            next ??= candidates.FirstOrDefault(
                m => m.Text.Contains(segment, StringComparison.OrdinalIgnoreCase));

            if (next is null)
            {
                var available = candidates.Select(m => m.Text).ToArray();
                throw new SapAgentResolutionException(segment, "menu item", available);
            }

            current = next;
            resolvedId = next.Id;
        }

        return resolvedId
               ?? throw new InvalidOperationException("Failed to resolve menu path.");
    }
}
