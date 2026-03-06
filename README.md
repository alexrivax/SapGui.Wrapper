# SapGui.Wrapper [![NuGet](https://img.shields.io/nuget/v/SapGui.Wrapper.svg?label=nuget)](https://www.nuget.org/packages/SapGui.Wrapper) [![CI](https://github.com/alexrivax/SapGui.Wrapper/actions/workflows/ci.yml/badge.svg)](https://github.com/alexrivax/SapGui.Wrapper/actions/workflows/ci.yml)

A strongly-typed .NET wrapper for the **SAP GUI Scripting API**
Purpose-built for **UiPath Coded Workflows** — giving RPA developers IntelliSense, compile-time safety, and clean, readable code instead of raw COM calls.

---

## Why this wrapper?

Native SAP GUI automation in UiPath forces you to choose between:

| Approach | Problem |
|---|---|
| Classic Activities (click/type) | Fragile, image-based, slow |
| Invoke Code + raw COM (`SapROTWr`) | Verbose, no IntelliSense, runtime errors only |
| Script Recorder → VBScript | Not reusable, no structure |

**SapGui.Wrapper** gives you a third option: paste the recorder IDs you already have, use C# or VB.NET objects with full IntelliSense, and get exceptions with meaningful messages the moment anything goes wrong.

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

Zero build-time dependency on the OCX — the package uses late-binding COM interop, so it works on any machine where SAP GUI is installed.

---

## Prerequisites

| Requirement | Details |
|---|---|
| SAP GUI | 7.40 or later, installed on the robot machine |
| Scripting enabled | SAP GUI Options → Accessibility & Scripting → Enable scripting |
| .NET | `net461` (legacy UiPath) or `net6.0-windows` (UiPath Studio 23+) |
| Platform | x64 Windows only (SAP GUI constraint) |

---

## Installation

**In UiPath Studio:**  
Manage Packages → NuGet.org → search `SapGui.Wrapper` → Install

**CLI:**
```
dotnet add package SapGui.Wrapper
```

---

## UiPath – Coded Workflow

This is the primary use case. A `CodedWorkflow` file in UiPath Studio is a plain C# class — use it exactly like any other C# code.

```csharp
using SapGui.Wrapper;
using UiPath.CodedWorkflows;

public class ProcessMaterialList : CodedWorkflow
{
    [Workflow]
    public DataTable Execute(string plant)
    {
        using var sap = SapGuiClient.Attach();  // throws SapGuiNotFoundException if SAP isn't running
        var session   = sap.Session;

        // ── Log who we're running as ──────────────────────────────────────────
        Log($"System: {session.Info.SystemName} | User: {session.Info.User} | TCode: {session.Info.Transaction}");

        // ── Navigate ──────────────────────────────────────────────────────────
        session.MainWindow().Maximize();
        session.StartTransaction("MM60");

        // ── Fill selection screen ─────────────────────────────────────────────
        session.TextField("wnd[0]/usr/txtS_WERKS-LOW").Text = plant;
        session.PressExecute();  // F8

        // ── Handle "no results" popup ─────────────────────────────────────────
        var popup = session.GetActivePopup();
        if (popup != null)
        {
            Log($"Popup: {popup.Text}", LogLevel.Warn);
            popup.ClickOk();
            return new DataTable();
        }

        // ── Read ALV grid ─────────────────────────────────────────────────────
        var grid = session.GridView("wnd[0]/usr/cntlGRID/shellcont/shell");
        var rows = grid.GetRows(new[] { "MATNR", "MAKTX", "MEINS", "LABST" });

        // ── Convert to DataTable for UiPath data activities ───────────────────
        var dt = new DataTable();
        dt.Columns.Add("Material"); dt.Columns.Add("Description");
        dt.Columns.Add("Unit");     dt.Columns.Add("UnrestrictedStock");

        foreach (var row in rows)
            dt.Rows.Add(row["MATNR"], row["MAKTX"], row["MEINS"], row["LABST"]);

        session.ExitTransaction();
        return dt;
    }
}
```

### What makes this clean for RPA

- **`StartTransaction`** replaces typing `/nXX` + Enter every time
- **`GetActivePopup()`** handles both `GuiMessageWindow` and `GuiModalWindow` uniformly
- **`grid.GetRows(columns)`** returns `List<Dictionary<string, string>>` — maps directly to DataTable columns
- **`ExitTransaction()`** navigates back to Easy Access without hardcoding `/n`
- Every method throws a descriptive exception on failure — no silent `null` returns from COM

---

## UiPath – Invoke Code activity (VB.NET)

Works in classic UiPath projects without Coded Workflows:

```vbnet
Dim sap As SapGuiClient = SapGuiClient.Attach()
Dim session As GuiSession = sap.Session

session.StartTransaction("VA01")
session.TextField("wnd[0]/usr/ctxtVBAK-AUART").Text = "OR"
session.ComboBox("wnd[0]/usr/cmbVBAK-VKORG").Key   = "1000"
session.PressEnter()

Dim status As String  = session.Statusbar().Text
Dim isError As Boolean = session.Statusbar().IsError
```

---

## UiPath – static one-liners (`SapGuiHelper`)

For the simplest workflows where you just need to set a field or press a button, `SapGuiHelper` removes even the `Attach()` call:

```vbnet
' VB.NET in UiPath Invoke Code — no variable management needed
SapGuiHelper.StartTransaction("MM01")
SapGuiHelper.SetText("wnd[0]/usr/txtMM01-MATNR", "MAT-0042")
SapGuiHelper.PressEnter()

Dim msg As String = SapGuiHelper.GetStatusMessage()
If SapGuiHelper.HasError() Then Throw New Exception("SAP error: " & msg)
```

---

## Getting component IDs from the SAP Script Recorder

Every component ID (`"wnd[0]/usr/txtRSYST-BNAME"`) comes directly from the **SAP GUI Script Recorder**:

1. In SAP GUI: **Alt+F12 → Script Recording and Playback → Start Recording**
2. Perform the actions manually
3. Stop recording — the VBScript output contains every ID you need
4. Paste those IDs into this wrapper; only change is adding `()` to method calls

Recorder output:
```vbscript
session.findById("wnd[0]/usr/txtRSYST-BNAME").text = "MYUSER"
session.findById("wnd[0]/tbar[1]/btn[8]").press
```

C# equivalent — paste the IDs, one mechanical change:
```csharp
session.findById("wnd[0]/usr/txtRSYST-BNAME").Text = "MYUSER";
session.findById("wnd[0]/tbar[1]/btn[8]").press();
```

`findById` returns `dynamic`, so all SAP properties and methods resolve at runtime — identical to VBScript. For compile-time IntelliSense, use the typed overload:
```csharp
session.FindById<GuiTextField>("wnd[0]/usr/txtRSYST-BNAME").Text = "MYUSER";
```

---

## Common patterns

### Reading a classic ABAP table

```csharp
var table = session.Table("wnd[0]/usr/tblSAPLXXX");

// Scroll-aware read: loop in visible-row increments
int totalRows = table.RowCount;
int pageSize  = table.VisibleRowCount;

for (int start = 0; start < totalRows; start += pageSize)
{
    table.ScrollToRow(start);
    for (int r = start; r < Math.Min(start + pageSize, totalRows); r++)
        Console.WriteLine(table.GetCellValue(r, 0));
}
```

### Reading an ALV grid (scroll-aware)

```csharp
var grid = session.GridView("wnd[0]/usr/cntlGRID/shellcont/shell");

Log($"Total rows: {grid.RowCount}, visible: {grid.VisibleRowCount}, first: {grid.FirstVisibleRow}");

// Navigate to a specific cell before reading a tooltip or checkbox
grid.SetCurrentCell(3, "STATUS");
string tooltip = grid.GetCellTooltip(3, "STATUS");
bool   flagged = grid.GetCellCheckBoxValue(3, "CRITICAL");

// Get currently selected rows (e.g. after SelectAll)
grid.SelectAll();
var selected = grid.SelectedRows;   // IReadOnlyList<int>
```

### Handling popups

```csharp
// Works for both GuiMessageWindow (pure dialogs) and GuiModalWindow (form dialogs)
var popup = session.GetActivePopup();
if (popup != null)
{
    Log($"[{popup.MessageType}] {popup.Title}: {popup.Text}");

    // Option 1 – standard buttons
    popup.ClickOk();      // or popup.ClickCancel()

    // Option 2 – custom button by partial text
    popup.ClickButton("Continue");

    // Option 3 – inspect all buttons
    foreach (var btn in popup.GetButtons())
        Log(btn.Text);
}
```

### Tabs, toolbars, and menus

```csharp
// Select a tab
session.TabStrip("wnd[0]/usr/tabsTABSTRIP").SelectTab(1);

// Press a toolbar button
session.Toolbar().PressButtonById("BACK");

// Navigate a menu
session.Menu("wnd[0]/mbar/menu[4]/menu[2]").Select();
```

### Tree controls

```csharp
var tree = session.Tree("wnd[0]/usr/cntlTREE/shellcont/shell");

tree.ExpandNode("ROOT");
foreach (var key in tree.GetChildNodeKeys("ROOT"))
    Log($"{key}: {tree.GetNodeText(key)}");

tree.SelectNode("CHILD01");
tree.DoubleClickNode("CHILD01");
```

### Multi-session workflows

```csharp
var sap = SapGuiClient.Attach();

// Get existing sessions
var session0 = sap.GetSession(connectionIndex: 0, sessionIndex: 0);
var session1 = sap.GetSession(connectionIndex: 0, sessionIndex: 1);

// Open a new parallel session on demand
session0.CreateSession();
// Then retrieve it:
var newSession = sap.Application
                    .GetFirstConnection()
                    .GetSessions()
                    .Last();
```

### Reactive automation with session events

`StartMonitoring()` starts a lightweight background thread that raises .NET events when the session state changes — useful for logging round-trips or detecting abends without polling in your workflow loop.

```csharp
var session = SapGuiClient.Attach().Session;

session.StartRequest += (_, e) =>
    Log($"Round-trip starting");

session.EndRequest += (_, e) =>
    Log($"Round-trip done — FunctionCode={e.FunctionCode}");

session.Change += (_, e) =>
    Log($"Round-trip done — [{e.MessageType}] {e.Text}  FunctionCode={e.FunctionCode}");

session.AbapRuntimeError += (_, e) =>
    throw new Exception($"ABAP abend: {e.Message}");

session.Destroy += (_, _) =>
    Log("SAP session closed", LogLevel.Warn);

// StartMonitoring tries a COM event sink first (no polling thread).
// Falls back to polling (500 ms) if the COM sink cannot connect.
session.StartMonitoring(pollMs: 500);

// ... run your automation ...

session.StopMonitoring();
```

### Connect to a new SAP system

```csharp
var app        = GuiApplication.Attach();
var connection = app.OpenConnection("PRD - Production");   // SAP Logon Pad entry name
var session    = connection.GetFirstSession();

// Or quickly get whichever session has focus
var active = app.ActiveSession;
```

---

## Waiting for SAP to finish

After navigation steps that trigger a server round-trip, call `WaitReady()` to block until SAP is done:

```csharp
session.PressExecute();
session.WaitReady(timeoutMs: 30_000);   // default: 30 seconds, polls every 200 ms

if (session.Statusbar().IsError)
    throw new Exception($"SAP error: {session.Statusbar().Text}");
```

---

## Exception types

| Exception | When thrown |
|---|---|
| `SapGuiNotFoundException` | SAP GUI is not running or scripting is disabled |
| `SapComponentNotFoundException` | `FindById` path does not match any component |
| `InvalidCastException` | `FindById<T>` found a component but it's a different type |
| `TimeoutException` | `WaitReady` timed out while session was busy |

---

## VKey reference

| VKey | Key | Action |
|---:|---|---|
| 0 | Enter | Confirm / navigate forward |
| 3 | F3 | Back — one screen back within the transaction |
| 4 | F4 | Input Help / Possible Values |
| 8 | F8 | **Execute** — runs the current report / selection screen |
| 11 | Ctrl+S | Save |
| 12 | F12 | Cancel — discards changes and closes the current screen |
| 15 | Shift+F3 | Exit — steps back to the previous menu level |
| 71 | Ctrl+Home | Scroll to top |
| 82 | Ctrl+End | Scroll to bottom |

Shortcuts: `PressEnter()` · `PressBack()` · `PressExecute()` · `ExitTransaction()`

---

## API coverage

### Component wrappers

| SAP COM Class | Wrapper | Key members |
|---|:---:|---|
| `GuiApplication` | ✅ | `Version`, `ActiveSession`, `GetConnections()`, `OpenConnection()` |
| `GuiConnection` | ✅ | `Description`, `GetSessions()` |
| `GuiSession` | ✅ | `ActiveWindow`, `FindById`, `StartTransaction`, `ExitTransaction`, `CreateSession`, `PressEnter/Back/Execute`, `SendVKey`, `GetActivePopup`, `WaitReady`, `StartMonitoring` (COM sink → polling fallback), `StartRequest`, `EndRequest`, `Change`, `Destroy`, `AbapRuntimeError`, `UserArea`, `ScrollContainer`, `Calendar`, `HtmlViewer`, `Shell` |
| `GuiMainWindow` | ✅ | `Title`, `IsMaximized`, `SendVKey`, `HardCopy`, `Maximize`, `Iconify`, `Restore`, `Close` |
| `GuiTextField` | ✅ | `Text`, `DisplayedText`, `MaxLength`, `IsReadOnly`, `IsRequired`, `IsOField`, `CaretPosition` |
| `GuiPasswordField` | ✅\* | `Text` set (write-only) — falls back to `GuiTextField` |
| `GuiButton` | ✅ | `Text`, `Press()` |
| `GuiLabel` | ✅ | `Text` (read-only) |
| `GuiCheckBox` | ✅ | `Selected` |
| `GuiRadioButton` | ✅ | `Selected` |
| `GuiComboBox` | ✅ | `Key`, `Value`, `ShowKey`, `Entries`, `SetKeyAndFireEvent` |
| `GuiStatusbar` | ✅ | `Text`, `MessageType`, `IsError` / `IsWarning` / `IsSuccess` |
| `GuiTable` | ✅ | `RowCount`, `ColumnCount`, `FirstVisibleRow`, `VisibleRowCount`, `ScrollToRow`, `CurrentCellRow/Column`, `GetCellValue`, `SetCellValue`, `GetVisibleRows`, `SelectRow` |
| `GuiGridView` | ✅ | `RowCount`, `FirstVisibleRow`, `VisibleRowCount`, `ColumnNames`, `CurrentCellRow/Column`, `SetCurrentCell`, `SelectedRows`, `GetCellValue`, `GetCellTooltip`, `GetCellCheckBoxValue`, `GetSymbolsForCell`, `GetRows`, `PressEnter`, `ClickCell`, `PressToolbarButton`, `SelectAll` |
| `GuiTree` | ✅ | `ExpandNode`, `CollapseNode`, `SelectNode`, `DoubleClickNode`, `GetNodeText`, `GetItemText`, `GetNodeType`, `GetChildNodeKeys`, `GetAllNodeKeys`, `NodeContextMenu`, `SelectedNode` |
| `GuiTabStrip` / `GuiTab` | ✅ | `TabCount`, `GetTabs()`, `SelectTab(int)`, `GetTabByName()`, `Tab.Select()` |
| `GuiToolbar` | ✅ | `ButtonCount`, `PressButton(int)`, `PressButtonById(string)`, `GetButtonTooltip(int)` |
| `GuiMenubar` / `GuiMenu` | ✅ | `Count`, `SelectItem()`, `GetChildren()` |
| `GuiContextMenu` | ✅ | `SelectByFunctionCode()`, `GetItemTexts()`, `Close()` |
| `GuiMessageWindow` | ✅ | `Text`, `Title`, `MessageType`, `ClickOk()`, `ClickCancel()`, `ClickButton()`, `GetButtons()` |
| `GuiModalWindow` | 🔶 | Via `session.GetActivePopup()` — children accessible with `findById` |
| `GuiUserArea` | ✅ | `FindById(relativeId)`, `FindById<T>(relativeId)` — address children with relative IDs |
| `GuiScrollContainer` | ✅ | `VerticalScrollbar`, `HorizontalScrollbar` (`Position`, `Minimum`, `Maximum`, `PageSize`), `ScrollToTop()` |
| `GuiShell` (generic) | ✅ | `SubType` — typed fallback for unrecognised shell variants |
| `GuiCalendar` | ✅ | `FocusedDate`, `SetDate(DateTime)`, `GetSelectedDate()` |
| `GuiHTMLViewer` | ✅ | `BrowserHandle`, `FireSapEvent(event, param1, param2)` |

**Legend:** ✅ typed wrapper · 🔶 accessible via dynamic / base class · ❌ not yet implemented

### Events

`GuiSession` exposes three polling-based .NET events, activated by calling `StartMonitoring()`:

| Event | When it fires |
|---|---|
| `Change` | After every server round-trip (IsBusy → idle transition) |
| `Destroy` | When the session becomes unreachable (window closed / connection lost) |
| `AbapRuntimeError` | When a status bar message type `A` (abend) is detected after a round-trip |

> For COM-native event sinks (Priority 4 on the roadmap), see `TODO.md`.

---

## Building the NuGet package

```powershell
cd SapGui.Wrapper
dotnet pack -c Release -o ../nupkg
```

---

## Project structure

```
SapGui.Wrapper/
├── SapGuiClient.cs          ← main entry point (Attach / GetSession)
├── SapGuiHelper.cs          ← static one-liner helpers for UiPath Invoke Code
├── Exceptions.cs
├── GlobalUsings.cs
│
├── Com/
│   └── SapRot.cs            ← ROT access (SapROTWr + P/Invoke fallback)
│
├── Core/
│   ├── GuiComponent.cs      ← base wrapper (late-binding helpers)
│   ├── GuiApplication.cs    ← GetConnections, OpenConnection, ActiveSession
│   ├── GuiConnection.cs     ← GetSessions
│   ├── GuiSession.cs        ← FindById, typed finders, events, CreateSession
│   ├── GuiMainWindow.cs     ← SendVKey, HardCopy, Maximize
│   └── SessionEvents.cs     ← SessionChangeEventArgs, SessionEventMonitor
│
├── Elements/
│   ├── GuiTextField.cs
│   ├── GuiButton.cs
│   ├── GuiComboBox.cs
│   ├── GuiCheckBox.cs / GuiRadioButton.cs
│   ├── GuiLabel.cs
│   ├── GuiStatusbar.cs
│   ├── GuiTable.cs          ← FirstVisibleRow, ScrollToRow, CurrentCell
│   ├── GuiGridView.cs       ← SetCurrentCell, SelectedRows, GetCellTooltip, …
│   ├── GuiTree.cs
│   ├── GuiTabStrip.cs
│   ├── GuiToolbar.cs
│   ├── GuiMenu.cs           ← GuiMenubar, GuiMenu, GuiContextMenu
│   └── GuiMessageWindow.cs
│
└── Enums/
    └── SapComponentType.cs
```

---

## License

MIT

