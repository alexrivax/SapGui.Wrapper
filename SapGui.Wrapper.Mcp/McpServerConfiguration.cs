namespace SapGui.Wrapper.Mcp;

/// <summary>
/// Runtime configuration for the SAP MCP server.
/// Bind from <c>appsettings.json</c> (section <c>"Sap"</c>) or environment variables
/// prefixed <c>SAP__</c>.
/// </summary>
public sealed class McpServerConfiguration
{
    /// <summary>
    /// When <see langword="true"/>, all tools that modify SAP state are blocked.
    /// Only <c>sap_connect</c>, <c>sap_disconnect</c>, <c>sap_scan_screen</c>,
    /// <c>sap_get_field</c>, <c>sap_read_grid</c>, and <c>sap_take_screenshot</c>
    /// are permitted in read-only mode.
    /// </summary>
    public bool ReadOnlyMode { get; set; }

    /// <summary>
    /// Transaction codes that the MCP server will refuse to start.
    /// Defaults to a deny-list of 12 high-risk system transactions.
    /// </summary>
    public HashSet<string> BlockedTransactions { get; set; } =
    [
        "SE38", "SE37", "SM49", "RZ10", "SCC4", "SU01",
        "PFCG", "SCC5", "SCC1", "SM59", "SM50", "RZ04",
    ];

    /// <summary>
    /// SAP connection string used when the server is started with a pre-configured
    /// connection (e.g. <c>"COR\PRD\100"</c>). May also be supplied via the
    /// <c>SAP_CONNECTION_STRING</c> environment variable or the <c>--connection</c>
    /// command-line argument.
    /// </summary>
    public string? InitialConnectionString { get; set; }

    /// <summary>
    /// SAP client number used with <see cref="InitialConnectionString"/>.
    /// May also be supplied via <c>SAP_CLIENT</c> env var or <c>--client</c> arg.
    /// </summary>
    public string? InitialClient { get; set; }

    // ── Guard helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Validates a pending tool call against the configured guardrails.
    /// Throws <see cref="SapAgentBlockedException"/> if the call should be denied.
    /// </summary>
    /// <param name="tCode">Transaction code being started (only relevant for <c>sap_start_transaction</c>).</param>
    /// <param name="isMutating">
    /// <see langword="true"/> for tools that write to SAP; <see langword="false"/> for read-only tools.
    /// </param>
    /// <param name="config">The current server configuration.</param>
    public static void ApplyGuardrails(
        string? tCode,
        bool isMutating,
        McpServerConfiguration config)
    {
        if (isMutating && config.ReadOnlyMode)
            throw new SapAgentBlockedException(
                "The MCP server is running in read-only mode. This tool is not permitted.");

        if (isMutating && tCode is not null)
        {
            var upper = tCode.Trim().ToUpperInvariant();
            if (config.BlockedTransactions.Contains(upper))
                throw new SapAgentBlockedException(
                    $"Transaction '{upper}' is on the server's blocked-transaction list.");
        }
    }
}
