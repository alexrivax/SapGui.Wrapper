using System.ComponentModel;
using ModelContextProtocol.Server;
using SapGui.Wrapper.Agent.Actions;

namespace SapGui.Wrapper.Mcp.Tools;

/// <summary>
/// MCP tools that map 1:1 to <see cref="SapAgentSession"/> methods.
/// Every tool calls <see cref="SapSessionManager.GetSession"/> (throws if not connected),
/// applies guardrails via <see cref="McpServerConfiguration.ApplyGuardrails"/>,
/// dispatches to the STA thread, and returns the screen context or an error string.
/// </summary>
[McpServerToolType]
public sealed class SapActionTools
{
    private readonly SapSessionManager _session;
    private readonly SapStaThread _sta;
    private readonly McpServerConfiguration _config;

    /// <summary>Initialises the tool class.</summary>
    public SapActionTools(
        SapSessionManager session,
        SapStaThread sta,
        McpServerConfiguration config)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _sta = sta ?? throw new ArgumentNullException(nameof(sta));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    // ── Observation ────────────────────────────────────────────────────────────

    [McpServerTool(Name = "sap_scan_screen")]
    [Description(
        "Capture a complete snapshot of the current SAP screen. " +
        "Returns a compact text description of all fields, buttons, grids, tabs, and the status bar. " +
        "Always call this first to understand the current screen state before taking any action.")]
    public async Task<string> ScanScreenAsync(
        [Description("Set to true to also capture a base64-encoded PNG screenshot of the SAP window.")]
        bool withScreenshot = false)
    {
        var agent = _session.GetSession();
        var snapshot = await _sta.RunAsync(() => agent.ScanScreen(withScreenshot));
        return snapshot.ToAgentContext();
    }

    [McpServerTool(Name = "sap_take_screenshot")]
    [Description(
        "Capture a screenshot of the current SAP window as a base64-encoded PNG. " +
        "Use when visual confirmation of the screen state is needed.")]
    public async Task<string> TakeScreenshotAsync()
    {
        var agent = _session.GetSession();
        var snapshot = await _sta.RunAsync(() => agent.ScanScreen(withScreenshot: true));
        return snapshot.ScreenshotBase64 is not null
            ? $"screenshot:base64:{snapshot.ScreenshotBase64}"
            : snapshot.ToAgentContext();
    }

    // ── Field operations ───────────────────────────────────────────────────────

    [McpServerTool(Name = "sap_set_field")]
    [Description(
        "Set the value of a field on the current SAP screen. " +
        "Identifies the field by its visible label (e.g. \"Plant\", \"Material\") or by its SAP element ID. " +
        "For ComboBox fields, provide the key or display text. " +
        "For CheckBox fields, pass \"X\" or \"true\" to check, anything else to uncheck. " +
        "Returns a diff summary of what changed on screen.")]
    public async Task<string> SetFieldAsync(
        [Description("Visible field label (e.g. \"Plant\") or SAP element ID path.")]
        string labelOrId,
        [Description("Value to set into the field.")]
        string value)
    {
        McpServerConfiguration.ApplyGuardrails(null, isMutating: true, _config);
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.SetField(labelOrId, value));
        return FormatResult(result);
    }

    [McpServerTool(Name = "sap_get_field")]
    [Description(
        "Read the current value of a field on the SAP screen. " +
        "Identifies the field by its visible label or SAP element ID. " +
        "Returns the field value as a string.")]
    public async Task<string> GetFieldAsync(
        [Description("Visible field label or SAP element ID path.")]
        string labelOrId)
    {
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.GetField(labelOrId));
        return FormatResult(result);
    }

    [McpServerTool(Name = "sap_clear_field")]
    [Description("Clear (empty) a field on the current SAP screen by its visible label or element ID.")]
    public async Task<string> ClearFieldAsync(
        [Description("Visible field label or SAP element ID path.")]
        string labelOrId)
    {
        McpServerConfiguration.ApplyGuardrails(null, isMutating: true, _config);
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.ClearField(labelOrId));
        return FormatResult(result);
    }

    // ── Button / key operations ────────────────────────────────────────────────

    [McpServerTool(Name = "sap_click_button")]
    [Description(
        "Click a button on the current SAP screen. " +
        "Identifies the button by its visible label, tooltip, or SAP element ID. " +
        "Examples: \"Execute\", \"Save\", \"Back\", \"Enter\".")]
    public async Task<string> ClickButtonAsync(
        [Description("Button label, tooltip, or SAP element ID.")]
        string labelOrId)
    {
        McpServerConfiguration.ApplyGuardrails(null, isMutating: true, _config);
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.ClickButton(labelOrId));
        return FormatResult(result);
    }

    [McpServerTool(Name = "sap_press_key")]
    [Description(
        "Send a keyboard shortcut or function key to the SAP session. " +
        "Common values: Enter, F1–F12, Back, Cancel, Save, Execute, PageUp, PageDown, " +
        "Tab, ShiftTab, Help, Find, Print.")]
    public async Task<string> PressKeyAsync(
        [Description(
            "Key action name. Valid values: Enter, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, " +
            "Back, Cancel, Save, Execute, PageUp, PageDown, Tab, ShiftTab, Help, Find, Print.")]
        string key)
    {
        McpServerConfiguration.ApplyGuardrails(null, isMutating: true, _config);
        if (!Enum.TryParse<SapKeyAction>(key, ignoreCase: true, out var keyAction))
            return $"Unknown key '{key}'. Valid values: {string.Join(", ", Enum.GetNames<SapKeyAction>())}.";

        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.PressKey(keyAction));
        return FormatResult(result);
    }

    // ── Navigation ─────────────────────────────────────────────────────────────

    [McpServerTool(Name = "sap_start_transaction")]
    [Description(
        "Navigate to a SAP transaction code (e.g. \"MM60\", \"VA01\", \"ME21N\"). " +
        "Equivalent to entering /n<tcode> in the command field. " +
        "Some system transactions are blocked by the server's safety configuration.")]
    public async Task<string> StartTransactionAsync(
        [Description("SAP transaction code, e.g. \"MM60\", \"VA01\", \"ME21N\".")]
        string tCode)
    {
        McpServerConfiguration.ApplyGuardrails(tCode, isMutating: true, _config);
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.StartTransaction(tCode));
        return FormatResult(result);
    }

    [McpServerTool(Name = "sap_select_menu")]
    [Description(
        "Activate a menu item by its path. " +
        "Use '/' as the path separator, e.g. \"Edit/Select All\" or \"Environment/Display Document\".")]
    public async Task<string> SelectMenuAsync(
        [Description("Menu path with '/' separators, e.g. \"Edit/Select All\".")]
        string menuPath)
    {
        McpServerConfiguration.ApplyGuardrails(null, isMutating: true, _config);
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.SelectMenu(menuPath));
        return FormatResult(result);
    }

    // ── Tab operations ─────────────────────────────────────────────────────────

    [McpServerTool(Name = "sap_select_tab")]
    [Description("Click a tab on the current SAP screen by its visible tab title.")]
    public async Task<string> SelectTabAsync(
        [Description("Tab title as shown on screen, e.g. \"General Data\", \"Purchasing\".")]
        string tabName)
    {
        McpServerConfiguration.ApplyGuardrails(null, isMutating: true, _config);
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.SelectTab(tabName));
        return FormatResult(result);
    }

    // ── Grid operations ────────────────────────────────────────────────────────

    [McpServerTool(Name = "sap_read_grid")]
    [Description(
        "Read the contents of an ALV grid table on the current SAP screen. " +
        "Returns all rows and columns as a JSON array, or a filtered subset if column names are supplied.")]
    public async Task<string> ReadGridAsync(
        [Description("Zero-based index of the grid when multiple grids are present. Defaults to 0.")]
        int gridIndex = 0,
        [Description("Optional comma-separated list of column names to include. Leave empty to return all columns.")]
        string? columns = null)
    {
        var agent = _session.GetSession();
        var colArray = columns?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = await _sta.RunAsync(() => agent.ReadGrid(gridIndex, colArray));
        return FormatResult(result);
    }

    [McpServerTool(Name = "sap_select_grid_row")]
    [Description("Select a row in an ALV grid by its zero-based row index.")]
    public async Task<string> SelectGridRowAsync(
        [Description("Zero-based row index to select.")]
        int rowIndex,
        [Description("Zero-based grid index when multiple grids are present. Defaults to 0.")]
        int gridIndex = 0)
    {
        McpServerConfiguration.ApplyGuardrails(null, isMutating: true, _config);
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.SelectGridRow(rowIndex, gridIndex));
        return FormatResult(result);
    }

    [McpServerTool(Name = "sap_open_grid_row")]
    [Description(
        "Double-click a row in an ALV grid to open its detail view. " +
        "Commonly used to drill into a document or line item.")]
    public async Task<string> OpenGridRowAsync(
        [Description("Zero-based row index to open.")]
        int rowIndex,
        [Description("Zero-based grid index when multiple grids are present. Defaults to 0.")]
        int gridIndex = 0)
    {
        McpServerConfiguration.ApplyGuardrails(null, isMutating: true, _config);
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.OpenGridRow(rowIndex, gridIndex));
        return FormatResult(result);
    }

    // ── Popup handling ─────────────────────────────────────────────────────────

    [McpServerTool(Name = "sap_handle_popup")]
    [Description(
        "Dismiss or interact with a SAP popup dialog. " +
        "action values: Confirm (click the default/primary button), " +
        "Cancel (click Cancel/No), ByButtonText (click the button named in buttonText).")]
    public async Task<string> HandlePopupAsync(
        [Description("Popup action: \"Confirm\", \"Cancel\", or \"ByButtonText\".")]
        string action,
        [Description("Button label to click when action is \"ByButtonText\".")]
        string? buttonText = null)
    {
        McpServerConfiguration.ApplyGuardrails(null, isMutating: true, _config);
        if (!Enum.TryParse<PopupAction>(action, ignoreCase: true, out var popupAction))
            return $"Unknown popup action '{action}'. Valid values: {string.Join(", ", Enum.GetNames<PopupAction>())}.";

        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.HandlePopup(popupAction, buttonText));
        return FormatResult(result);
    }

    // ── Tree operations ────────────────────────────────────────────────────────

    [McpServerTool(Name = "sap_expand_tree_node")]
    [Description("Expand a node in a SAP tree control by its visible text label.")]
    public async Task<string> ExpandTreeNodeAsync(
        [Description("Visible label of the tree node to expand, e.g. \"Plant 1000\".")]
        string nodeText)
    {
        McpServerConfiguration.ApplyGuardrails(null, isMutating: true, _config);
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.ExpandTreeNode(nodeText));
        return FormatResult(result);
    }

    [McpServerTool(Name = "sap_select_tree_node")]
    [Description(
        "Select a node in a SAP tree control. " +
        "Use doubleClick=true to open the node's detail view.")]
    public async Task<string> SelectTreeNodeAsync(
        [Description("Visible label of the tree node to select.")]
        string nodeText,
        [Description("Set to true to double-click the node (opens detail view). Defaults to false (single click).")]
        bool doubleClick = false)
    {
        McpServerConfiguration.ApplyGuardrails(null, isMutating: true, _config);
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.SelectTreeNode(nodeText, doubleClick));
        return FormatResult(result);
    }

    // ── Wait ───────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "sap_wait_and_scan")]
    [Description(
        "Wait for the SAP screen to finish loading and then return a fresh screen snapshot. " +
        "Use after triggering a long-running SAP operation (e.g. a report execution). " +
        "Returns the screen context once loading completes or the timeout is reached.")]
    public async Task<string> WaitAndScanAsync(
        [Description("Maximum time to wait in milliseconds before returning. Defaults to 30000 (30 seconds).")]
        int timeoutMs = 30_000)
    {
        var agent = _session.GetSession();
        var result = await _sta.RunAsync(() => agent.WaitAndScan(timeoutMs));
        return FormatResult(result);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static string FormatResult(SapActionResult result)
    {
        if (!result.Success)
            return $"ERROR: {result.ErrorMessage}";

        var snapshot = result.SnapshotAfter ?? result.SnapshotBefore;
        var context = snapshot?.ToAgentContext() ?? "(no screen context available)";

        if (!string.IsNullOrWhiteSpace(result.Diff))
            return $"{context}\n\nCHANGES:\n{result.Diff}";

        return context;
    }
}
