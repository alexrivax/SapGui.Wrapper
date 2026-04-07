using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SapGui.Wrapper.Mcp.Tools;

/// <summary>
/// MCP tools for establishing and closing the SAP GUI connection.
/// </summary>
[McpServerToolType]
public sealed class SapConnectionTools
{
    private readonly SapSessionManager _session;

    /// <summary>Initialises the tool class with the session manager.</summary>
    public SapConnectionTools(SapSessionManager session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>
    /// Opens a connection to a running SAP GUI instance.
    /// SAP Logon must already be open on the Windows desktop.
    /// Use this if no startup connection was configured.
    /// </summary>
    [McpServerTool(Name = "sap_connect")]
    [Description(
        "Connect to a running SAP GUI instance on this machine. " +
        "SAP Logon must already be open. " +
        "connectionString examples: \"COR\\PRD\\100\" (server\\SID\\client) or " +
        "\"/H/myhost/S/3200\" (router address). " +
        "client overrides the logon client number (optional).")]
    public async Task<string> ConnectAsync(
        [Description("SAP connection string, e.g. \"COR\\PRD\\100\" or \"/H/myhost/S/3200\"")]
        string connectionString,
        [Description("SAP client number override, e.g. \"100\". Leave empty to use the connection default.")]
        string? client = null)
    {
        await _session.ConnectAsync(connectionString, client);
        return $"Connected to SAP. Session is ready.";
    }

    /// <summary>
    /// Closes the current SAP GUI session.
    /// </summary>
    [McpServerTool(Name = "sap_disconnect")]
    [Description("Disconnect from the active SAP GUI session.")]
    public async Task<string> DisconnectAsync()
    {
        await _session.DisconnectAsync();
        return "SAP session disconnected.";
    }
}
