---
_layout: landing
---

# SapGui.Wrapper

[![NuGet](https://img.shields.io/nuget/v/SapGui.Wrapper.svg?label=nuget)](https://www.nuget.org/packages/SapGui.Wrapper)
[![CI](https://github.com/alexrivax/SapGui.Wrapper/actions/workflows/ci.yml/badge.svg)](https://github.com/alexrivax/SapGui.Wrapper/actions/workflows/ci.yml)

A strongly-typed .NET wrapper for the **SAP GUI Scripting API** — purpose-built for
**UiPath Coded Workflows**, giving RPA developers IntelliSense, compile-time safety,
and clean, readable code instead of raw COM calls.

The library ships three layers — use only as much as your scenario needs:

```
  AI host (Claude, VS Code Copilot, Cursor, …)
          │  MCP stdio (JSON-RPC)
          ▼
    SapGui.Wrapper.Mcp      ← dotnet tool: 18 typed MCP tools
          │
          ▼
    SapGui.Wrapper.Agent    ← label-based façade + screen snapshots
          │
          ▼
    SapGui.Wrapper          ← this NuGet package (COM wrapper)
          │
          ▼
    sapfewse.ocx            ← SAP GUI Scripting Engine (installed with SAP GUI)
          │
          ▼
     SAP GUI (running)
```

Zero build-time dependency on the OCX — the package uses late-binding COM interop,
so it works on any machine where SAP GUI is installed.

## AI Integration

`SapGui.Wrapper.Agent` adds a label-based action façade and structured screen snapshots on top of the core COM layer, so automation logic can operate on human-readable field names (`"Plant"`, `"Material"`) instead of raw COM element paths.

`SapGui.Wrapper.Mcp` wraps the Agent layer as an [MCP](https://modelcontextprotocol.io/) stdio server, exposing 18 typed tools to any compatible AI host. Install it as a global dotnet tool:

```bash
dotnet tool install --global SapGui.Wrapper.Mcp
sapgui-mcp --connection "COR\PRD\100"
```

Point Claude Desktop, VS Code Copilot Agent, or Cursor at the server and the AI can read SAP screens, fill fields, navigate transactions, and read ALV grids — without any SAP scripting knowledge.

## Install

```
dotnet add package SapGui.Wrapper
```

Or in **UiPath Studio**: Manage Packages → NuGet.org → search `SapGui.Wrapper` → Install.

## Quickstart

```csharp
using SapGui.Wrapper;

using var sap     = SapGuiClient.Attach();
var       session = sap.Session;

session.StartTransaction("SE16");
session.TextField("wnd[0]/usr/ctxtDATABROWSE-TABLENAME").Text = "MARA";
session.PressExecute();
session.WaitForReadyState(timeoutMs: 15_000);

var grid = session.GridView("wnd[0]/usr/cntlGRID/shellcont/shell");
Console.WriteLine($"Rows: {grid.RowCount}");
```

## Where to go next

- [Getting Started](articles/getting-started.md) — prerequisites, installation, first workflow
- [Common Patterns](articles/patterns.md) — tables, grids, popups, trees, multi-session
- [Resilient Automation](articles/retry.md) — `WithRetry`, `ElementExists`, `WaitForReadyState`
- [Logging](articles/logging.md) — `ILogger` and `SapLogAction` integration
- [MCP Server](articles/mcp-server.md) — 18 MCP tools, guardrails, Claude Desktop & VS Code integration
- [Agent Session](articles/agent.md) — label-based façade, screen snapshots, custom AI hosts
- [API Reference](~/api/index.md) — full type and member listing generated from XML docs
