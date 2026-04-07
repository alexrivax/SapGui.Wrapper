# MCP Server

`SapGui.Wrapper.Mcp` is a [Model Context Protocol](https://modelcontextprotocol.io/) stdio server that exposes SAP GUI automation as 18 typed tools consumable by any MCP-compatible AI host.

## Prerequisites

| Requirement  | Details                                                                         |
| ------------ | ------------------------------------------------------------------------------- |
| SAP GUI      | 7.40 or later, installed and running on the same Windows machine as the server  |
| Scripting    | SAP GUI scripting enabled server-side and client-side (same as core library)    |
| .NET runtime | .NET 8 SDK or runtime (x64, Windows)                                            |
| MCP host     | Claude Desktop, VS Code Copilot Agent, Cursor, or any stdio-compatible MCP host |

## Installation

```bash
dotnet tool install --global SapGui.Wrapper.Mcp
```

Start the server from a terminal (SAP Logon must already be open):

```bash
# Connect at startup using a known SAP connection
sapgui-mcp --connection "COR\PRD\100" --client 100

# Or start without a connection and use sap_connect at runtime
sapgui-mcp
```

## Connecting to SAP

Three approaches are supported:

| Approach              | How                                                      | When to use                                         |
| --------------------- | -------------------------------------------------------- | --------------------------------------------------- |
| CLI flags             | `--connection "SID\ENV\100"` and `--client 100`          | Fixed environment, single-purpose agent             |
| Environment variables | `SAP_CONNECTION_STRING=COR\PRD\100` and `SAP_CLIENT=100` | Docker / CI where CLI args are not practical        |
| `sap_connect` tool    | Call the tool after the MCP host connects                | Dynamic environments where the target system varies |

The `--connection` / `SAP_CONNECTION_STRING` format is `Server\SID\Client` (backslashes) or an RFC router string like `/H/myhost/S/3200`.

## Tool Reference

### Connection tools

| Tool             | Parameters                                         | Description                           |
| ---------------- | -------------------------------------------------- | ------------------------------------- |
| `sap_connect`    | `connectionString` (required), `client` (optional) | Connect to a running SAP GUI instance |
| `sap_disconnect` | —                                                  | Close the active SAP session          |

### Observation tools

These tools never modify SAP state and work even in `ReadOnlyMode`.

| Tool                  | Parameters                | Description                                                                |
| --------------------- | ------------------------- | -------------------------------------------------------------------------- |
| `sap_scan_screen`     | `withScreenshot=false`    | Full screen snapshot: all fields, buttons, grids, tabs, status bar, popups |
| `sap_take_screenshot` | —                         | Returns a base64-encoded PNG of the SAP window                             |
| `sap_get_field`       | `labelOrId`               | Read the current value of a field by label or COM path                     |
| `sap_read_grid`       | `gridIndex=0`, `columns?` | Read ALV grid rows; optional comma-separated column filter                 |
| `sap_wait_and_scan`   | `timeoutMs=30000`         | Wait for SAP to finish loading, then return a full screen snapshot         |

### Mutating tools

Blocked when `ReadOnlyMode` is `true` or the target transaction is in `BlockedTransactions`.

| Tool                    | Parameters                      | Description                                                            |
| ----------------------- | ------------------------------- | ---------------------------------------------------------------------- |
| `sap_set_field`         | `labelOrId`, `value`            | Set a text field, combo box, or checkbox by its visible label          |
| `sap_clear_field`       | `labelOrId`                     | Clear a field to empty                                                 |
| `sap_click_button`      | `labelOrId`                     | Click a button by label, tooltip, or COM path                          |
| `sap_press_key`         | `key`                           | Send a VKey: `Enter`, `Back`, `Execute`, `Save`, `Cancel`, `F4`, …     |
| `sap_start_transaction` | `tCode`                         | Navigate to a transaction code                                         |
| `sap_select_menu`       | `menuPath`                      | Activate a menu item by slash-separated path, e.g. `"Edit/Select All"` |
| `sap_select_tab`        | `tabName`                       | Click a tab by its visible label                                       |
| `sap_select_grid_row`   | `rowIndex`, `gridIndex=0`       | Select a row in an ALV grid                                            |
| `sap_open_grid_row`     | `rowIndex`, `gridIndex=0`       | Double-click a grid row to drill into its detail view                  |
| `sap_handle_popup`      | `action`, `buttonText?`         | Dismiss a popup: `Confirm`, `Cancel`, or `ByButtonText`                |
| `sap_expand_tree_node`  | `nodeText`                      | Expand a tree node by its display text                                 |
| `sap_select_tree_node`  | `nodeText`, `doubleClick=false` | Select (and optionally open) a tree node                               |

All tools return a plain-text string:

- Observation tools return `ToAgentContext()` — a compact structured summary of the current screen.
- Mutating tools return the updated screen context plus a `CHANGES:` diff block.
- On failure, all tools return `ERROR: <message>`.

### Valid `sap_press_key` values

`Enter`, `F1`, `F2`, `F3`, `F4`, `F5`, `F6`, `F7`, `F8`, `F9`, `F10`, `F11`, `F12`, `Back`, `Cancel`, `Save`, `Execute`, `PageUp`, `PageDown`, `Tab`, `ShiftTab`, `Help`, `Find`, `Print`

## Guardrails

The server has two safety mechanisms applied before every mutating tool call.

### Read-only mode

```json
// appsettings.json
{
  "Sap": {
    "ReadOnlyMode": true
  }
}
```

When enabled, only `sap_connect`, `sap_disconnect`, `sap_scan_screen`, `sap_take_screenshot`, `sap_get_field`, `sap_read_grid`, and `sap_wait_and_scan` are permitted. All other tools return an error.

> [!IMPORTANT]
> `ReadOnlyMode` does **not** prevent the AI host from reading sensitive data.
> It only blocks tools that write to SAP. Combine with appropriate IAM/authorization controls on the SAP side.

### Blocked transactions

`sap_start_transaction` rejects any code in the deny-list. The default list:

```
SE38  SE37  SM49  RZ10  SCC4  SU01
PFCG  SCC5  SCC1  SM59  SM50  RZ04
```

Override the list in configuration:

```json
{
  "Sap": {
    "BlockedTransactions": ["SE38", "SE37", "SM49"]
  }
}
```

An empty array disables transaction blocking entirely.

## Configuration

All options are bound from the `"Sap"` section of `appsettings.json`. Each can be overridden by a CLI argument or environment variable.

| `appsettings.json` key        | CLI argument   | Environment variable    | Default                            |
| ----------------------------- | -------------- | ----------------------- | ---------------------------------- |
| `Sap:InitialConnectionString` | `--connection` | `SAP_CONNECTION_STRING` | `null` (no startup connection)     |
| `Sap:InitialClient`           | `--client`     | `SAP_CLIENT`            | `null`                             |
| `Sap:ReadOnlyMode`            | —              | `SAP__ReadOnlyMode`     | `false`                            |
| `Sap:BlockedTransactions`     | —              | —                       | 12 system transactions (see above) |

Example `appsettings.json`:

```json
{
  "Sap": {
    "InitialConnectionString": "COR\\PRD\\100",
    "InitialClient": "100",
    "ReadOnlyMode": false,
    "BlockedTransactions": ["SE38", "SE37", "SM49", "RZ10", "SCC4", "SU01", "PFCG"]
  }
}
```

## AI Client Integration

### Claude Desktop

Add to `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) or
`%APPDATA%\Claude\claude_desktop_config.json` (Windows):

```json
{
  "mcpServers": {
    "sapgui": {
      "command": "sapgui-mcp",
      "args": ["--connection", "COR\\PRD\\100", "--client", "100"]
    }
  }
}
```

Restart Claude Desktop. A new "saputility" toolbar icon confirms the server is connected.

### VS Code Copilot Agent

Add to your workspace or user `settings.json` under the `"mcp"` key:

```json
{
  "mcp": {
    "servers": {
      "sapgui": {
        "type": "stdio",
        "command": "sapgui-mcp",
        "args": ["--connection", "COR\\PRD\\100", "--client", "100"]
      }
    }
  }
}
```

Switch to **Agent mode** in the Copilot Chat panel. The SAP tools are listed under the tools icon (wrench). Approve tool calls the first time to allow Copilot to interact with SAP.

> [!IMPORTANT]
> The MCP server uses **stdout exclusively for JSON-RPC**. All diagnostic logs go to stderr.
> Do not add console logging that writes to stdout, as it will corrupt the MCP stream.

## Typical Agent Loop

The LLM + MCP server follow a scan-act-observe cycle:

```
1. sap_connect  (once, or handled by startup args)
2. sap_scan_screen
   → LLM reads current fields / buttons / transaction
3. sap_start_transaction  "MM60"
4. sap_set_field  "Plant" → "1000"
5. sap_press_key  "Execute"
6. sap_wait_and_scan  (wait for report to finish)
   → LLM reads grid headers and row count
7. sap_read_grid  gridIndex=0
   → LLM processes data or asks for clarification
8. sap_start_transaction  "MM60"  (navigate away when done)
```

For long-running ABAP reports, always call `sap_wait_and_scan` rather than `sap_scan_screen` immediately after triggering execution — it waits for the busy indicator to clear before returning.
