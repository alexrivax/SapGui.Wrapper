using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SapGui.Wrapper.Mcp;

var builder = Host.CreateApplicationBuilder(args);

// All logs must go to stderr — stdout is reserved for MCP JSON-RPC messages.
builder.Logging.AddConsole(o =>
{
    o.LogToStandardErrorThreshold = LogLevel.Trace;
});

// ── Configuration ─────────────────────────────────────────────────────────────

var sapConfig = new McpServerConfiguration();

// Bind from appsettings.json section "Sap" if present.
builder.Configuration.GetSection("Sap").Bind(sapConfig);

// CLI overrides: --connection <value> and --client <value>
var cliConnection = builder.Configuration["connection"];
var cliClient = builder.Configuration["client"];

if (!string.IsNullOrWhiteSpace(cliConnection))
    sapConfig.InitialConnectionString = cliConnection;

if (!string.IsNullOrWhiteSpace(cliClient))
    sapConfig.InitialClient = cliClient;

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddSingleton(sapConfig);
builder.Services.AddSingleton<SapStaThread>();
builder.Services.AddSingleton<SapSessionManager>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// ── Build & run ───────────────────────────────────────────────────────────────

var host = builder.Build();

// Attempt startup connection (silent on failure — falls back to sap_connect tool).
host.Services.GetRequiredService<SapSessionManager>().TryConnectFromStartupArgs();

await host.RunAsync();
