---
_layout: landing
---

# SapGui.Wrapper

[![NuGet](https://img.shields.io/nuget/v/SapGui.Wrapper.svg?label=nuget)](https://www.nuget.org/packages/SapGui.Wrapper)
[![CI](https://github.com/alexrivax/SapGui.Wrapper/actions/workflows/ci.yml/badge.svg)](https://github.com/alexrivax/SapGui.Wrapper/actions/workflows/ci.yml)

A strongly-typed .NET wrapper for the **SAP GUI Scripting API** — purpose-built for
**UiPath Coded Workflows**, giving RPA developers IntelliSense, compile-time safety,
and clean, readable code instead of raw COM calls.

```
UiPath Coded Workflow (C# / VB.NET)
          │
          ▼
    SapGui.Wrapper          ← this NuGet package
          │
          ▼
    sapfewse.ocx            ← SAP GUI Scripting Engine (installed with SAP GUI)
          │
          ▼
     SAP GUI (running)
```

Zero build-time dependency on the OCX — the package uses late-binding COM interop,
so it works on any machine where SAP GUI is installed.

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
- [API Reference](api/index.md) — full type and member listing generated from XML docs
