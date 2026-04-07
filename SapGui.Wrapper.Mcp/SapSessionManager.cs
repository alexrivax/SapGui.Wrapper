using Microsoft.Extensions.Logging;
using SapGui.Wrapper.Agent.Actions;

namespace SapGui.Wrapper.Mcp;

/// <summary>
/// Manages the SAP GUI session lifecycle for the MCP server.
/// <para>
/// Owns a single <see cref="SapGuiClient"/> and <see cref="SapAgentSession"/>,
/// both created and used exclusively on the <see cref="SapStaThread"/> STA thread.
/// </para>
/// </summary>
public sealed class SapSessionManager : IDisposable
{
    private readonly SapStaThread _sta;
    private readonly McpServerConfiguration _config;
    private readonly ILogger<SapSessionManager> _logger;

    private SapGuiClient? _client;
    private SapAgentSession? _agentSession;

    /// <summary>Whether a SAP session is currently open.</summary>
    public bool IsConnected => _agentSession is not null;

    /// <summary>Initialises the manager. Does not open a SAP connection.</summary>
    public SapSessionManager(
        SapStaThread sta,
        McpServerConfiguration config,
        ILogger<SapSessionManager> logger)
    {
        _sta = sta ?? throw new ArgumentNullException(nameof(sta));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Startup ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to open a SAP session using startup arguments or environment variables.
    /// Silently succeeds with no session if no connection info is present or the connection fails,
    /// so the server can still start and fall back to an explicit <c>sap_connect</c> tool call.
    /// </summary>
    public void TryConnectFromStartupArgs()
    {
        var connectionString =
            _config.InitialConnectionString ??
            Environment.GetEnvironmentVariable("SAP_CONNECTION_STRING");

        var client =
            _config.InitialClient ??
            Environment.GetEnvironmentVariable("SAP_CLIENT");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogInformation(
                "No startup connection string found. Waiting for sap_connect tool call.");
            return;
        }

        try
        {
            ConnectAsync(connectionString, client).GetAwaiter().GetResult();
            _logger.LogInformation("Startup connection established to '{Connection}'.", connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Startup connection to '{Connection}' failed. Server will wait for sap_connect.",
                connectionString);
        }
    }

    // ── Session lifecycle ─────────────────────────────────────────────────────

    /// <summary>
    /// Opens a SAP session on the STA thread.
    /// If a session is already open, the existing session is disconnected first.
    /// </summary>
    /// <param name="connectionString">
    /// SAP connection string, e.g. <c>"COR\PRD\100"</c> or <c>"/H/saphost/S/3200"</c>.
    /// </param>
    /// <param name="client">Optional SAP client number override, e.g. <c>"100"</c>.</param>
    public async Task ConnectAsync(string connectionString, string? client = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must not be empty.", nameof(connectionString));

        await _sta.RunAsync(() =>
        {
            DisposeSapObjects();
            _client = SapGuiClient.Attach();
            _agentSession = _client.Session.Agent();
            _logger.LogInformation("SAP session connected.");
        });
    }

    /// <summary>Closes the active SAP session, if any.</summary>
    public async Task DisconnectAsync()
    {
        await _sta.RunAsync(() =>
        {
            DisposeSapObjects();
            _logger.LogInformation("SAP session disconnected.");
        });
    }

    // ── Tool entry point ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current <see cref="SapAgentSession"/>, throwing if no session is open.
    /// Always call this at the start of every tool implementation.
    /// </summary>
    /// <exception cref="InvalidOperationException">No active session — caller must invoke <c>sap_connect</c> first.</exception>
    public SapAgentSession GetSession()
    {
        return _agentSession
            ?? throw new InvalidOperationException(
                "No active SAP session. Call the sap_connect tool first.");
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void DisposeSapObjects()
    {
        _agentSession = null;

        try { _client?.Dispose(); } catch { /* best-effort */ }
        _client = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _sta.RunAsync(DisposeSapObjects).GetAwaiter().GetResult();
    }
}
