# SapGui.Wrapper [![NuGet](https://img.shields.io/nuget/v/SapGui.Wrapper.svg?label=nuget)](https://www.nuget.org/packages/SapGui.Wrapper) [![CI](https://github.com/alexrivax/SapGui.Wrapper/actions/workflows/ci.yml/badge.svg)](https://github.com/alexrivax/SapGui.Wrapper/actions/workflows/ci.yml)

A strongly-typed .NET wrapper for the **SAP GUI Scripting API** (`sapfewse.ocx`).  
Lets you automate SAP GUI from C# or VB.NET exactly the way VBA does it natively – fully usable in **UiPath** via Invoke Code / Coded Workflows.

## How it works

```
Your C# / VB.NET code
        │
        ▼
  SapGui.Wrapper          ← this NuGet package (late-binding COM interop)
        │
        ▼
  sapfewse.ocx            ← SAP GUI Scripting Engine (installed with SAPGUI)
        │
        ▼
   SAP GUI (running)
```

The package uses **late-binding COM interop** (`Type.InvokeMember`) so it has **zero build-time dependency** on the OCX.  
At runtime, SAP GUI must be installed and running, with scripting enabled.

---

## Prerequisites

| Requirement       | Details                                                                    |
| ----------------- | -------------------------------------------------------------------------- |
| SAP GUI           | 7.40 or later, installed on the machine running the automation             |
| Scripting enabled | SAP GUI Options → Accessibility & Scripting → Scripting → Enable scripting |
| .NET              | `net461` (legacy UiPath) or `net6.0-windows` (modern UiPath / Studio 23+)  |
| Platform          | x64 Windows only (SAP GUI constraint)                                      |

---

## Installation

```
dotnet add package SapGui.Wrapper
```

Or in UiPath Studio: **Manage Packages → Add local feed** pointing to the `.nupkg` file.

---

## Quick start – C#

```csharp
using SapGui.Wrapper;

// Attach to the running SAP GUI
using var sap = SapGuiClient.Attach();
var session   = sap.Session;           // first connection, first session

// Navigate to a transaction
session.StartTransaction("SE16");

// Fill a field  (ID comes from the SAP GUI Script Recorder)
session.TextField("wnd[0]/usr/ctxtDATABROWSE-TABLENAME").Text = "MARA";
session.PressEnter();

// Read the status bar
var status = session.Statusbar();
Console.WriteLine($"[{status.MessageType}] {status.Text}");

// Main window title
Console.WriteLine(session.MainWindow().Title);
```

---

## Quick start – VB.NET (for UiPath Invoke Code activity)

```vbnet
Dim sap As SapGuiClient = SapGuiClient.Attach()
Dim session As GuiSession = sap.Session

session.StartTransaction("MM60")
session.TextField("wnd[0]/usr/txtS_WERKS-LOW").Text = "1000"
session.TextField("wnd[0]/usr/txtS_MATNR-LOW").Text = "MAT001"
session.PressEnter()

Dim msg As String = session.Statusbar().Text
```

---

## UiPath – static helper (one-liner style)

The `SapGuiHelper` class lets you write single-line calls without managing the `SapGuiClient` object yourself:

```vbnet
' VB.NET in UiPath Invoke Code
SapGuiHelper.StartTransaction("VA01")
SapGuiHelper.SetText("wnd[0]/usr/ctxtVBAK-AUART", "OR")
SapGuiHelper.SetComboBox("wnd[0]/usr/cmbVBAK-VKORG", "1000")
SapGuiHelper.PressEnter()

Dim orderStatus As String = SapGuiHelper.GetStatusMessage()
```

---

## Finding components by ID (recorder compatibility)

The `findById` method mirrors the SAP Script Recorder output. Recorder-generated VBScript:

```vbscript
session.findById("wnd[0]/usr/txtRSYST-BNAME").text = "MYUSER"
```

In C# with this wrapper:

```csharp
session.findById("wnd[0]/usr/txtRSYST-BNAME").Text = "MYUSER";
```

`findById` returns `dynamic`, so any SAP property or method accessible on the COM object works at runtime without explicit casting. For compile-time safety, use the typed overload:

```csharp
var tf = session.FindById<GuiTextField>("wnd[0]/usr/txtRSYST-BNAME");
tf.Text = "MYUSER";
```

---

## Working with tabs (GuiTabStrip / GuiTab)

```csharp
var tabs = session.TabStrip("wnd[0]/usr/tabsTABSTRIP");

// Enumerate tabs
foreach (var tab in tabs.GetTabs())
    Console.WriteLine(tab.Text);

// Select by index or name
tabs.SelectTab(0);
// -- or --
tabs.GetTabByName("Details").Select();
```

---

## Working with toolbars (GuiToolbar)

```csharp
var toolbar = session.Toolbar("wnd[0]/tbar[1]");

// Press toolbar button by index
toolbar.PressButton(0);

// Press by ID fragment
toolbar.PressButtonById("BACK");

// Read tooltip
Console.WriteLine(toolbar.GetButtonTooltip(2));
```

---

## Working with menus (GuiMenubar / GuiMenu)

```csharp
var menubar = session.Menubar("wnd[0]/mbar");

// Select a top-level menu item by text
menubar.SelectItem("System");

// Navigate sub-menus via typed wrapper
var systemMenu = session.Menu("wnd[0]/mbar/menu[4]");
foreach (var child in systemMenu.GetChildren())
    Console.WriteLine(child.Text);
```

---

## Working with context menus (GuiContextMenu)

```csharp
// Right-click action triggers a context menu – grab it via GetActivePopup
var ctx = session.GetActivePopup() as GuiContextMenu;

// Select by SAP function code
ctx?.SelectByFunctionCode("COPY");

// Or list all items
foreach (var item in ctx?.GetItemTexts() ?? Array.Empty<string>())
    Console.WriteLine(item);
```

---

## Working with modal message windows (GuiMessageWindow)

```csharp
var popup = session.GetActivePopup() as GuiMessageWindow;
if (popup != null)
{
    Console.WriteLine($"[{popup.MessageType}] {popup.Text}");
    popup.ClickOk();            // click first OK-like button
    // -- or --
    popup.ClickCancel();
    // -- or --
    popup.ClickButton("Yes");   // partial text match
}
```

---

## Working with tables and grids

### Classic ABAP table (GuiTable)

```csharp
var table = session.Table("wnd[0]/usr/tabsTABSTRIP/tabpTAB1/ssubSUB:SAPLRFCTEST:0001/tblSAPLRFCTESTT_RFC");
int rows  = table.RowCount;

for (int r = 0; r < rows; r++)
    Console.WriteLine(table.GetCellValue(r, 0));
```

### ALV Grid (GuiGridView)

```csharp
var grid = session.GridView("wnd[0]/usr/cntlGRID/shellcont/shell");

foreach (var row in grid.GetRows(new[] { "MATNR", "MAKTX", "MEINS" }))
    Console.WriteLine($"{row["MATNR"]} | {row["MAKTX"]} | {row["MEINS"]}");
```

### Tree control (GuiTree)

```csharp
var tree = session.Tree("wnd[0]/usr/cntlTREE/shellcont/shell");

tree.ExpandNode("ROOT");
foreach (var key in tree.GetChildNodeKeys("ROOT"))
    Console.WriteLine($"{key}: {tree.GetNodeText(key)}");

tree.SelectNode("CHILD01");
tree.DoubleClickNode("CHILD01");
```

---

## Obtaining component IDs

The IDs (e.g. `"wnd[0]/usr/txtRSYST-BNAME"`) come directly from the **SAP GUI Script Recorder**:

1. In SAP GUI: **Customize Local Layout (Alt+F12) → Script Recording and Playback → Start Recording**
2. Perform the actions manually
3. Stop the recording – the generated VBScript shows all the IDs
4. Use those IDs directly in this wrapper

---

## Multi-session support

```csharp
var sap      = SapGuiClient.Attach();
var session0 = sap.GetSession(connectionIndex: 0, sessionIndex: 0);
var session1 = sap.GetSession(connectionIndex: 0, sessionIndex: 1);
```

---

## VKey reference (SendVKey)

| VKey | Action                           |
| ---: | -------------------------------- |
|    0 | Enter                            |
|    3 | F3 – Back                        |
|    4 | F4 – Possible values / matchcode |
|    5 | F5                               |
|    8 | Ctrl+S – Save                    |
|   11 | F11                              |
|   12 | Shift+F4 – Exit                  |
|   15 | Shift+F3 – Cancel                |
|   71 | Ctrl+Home – scroll to top        |
|   82 | Ctrl+End – scroll to bottom      |

---

## API coverage (v1.3.0)

### Objects / component types

| SAP COM Class            | Typed wrapper | Key members exposed                                                                                              | Notes                        |
| ------------------------ | :-----------: | ---------------------------------------------------------------------------------------------------------------- | ---------------------------- |
| `GuiApplication`         |      ✅       | `Version`, `GetConnections()`                                                                                    |                              |
| `GuiConnection`          |      ✅       | `Description`, `GetSessions()`                                                                                   |                              |
| `GuiSession`             |      ✅       | `FindById`, `StartTransaction`, `SendVKey`, `WaitReady`                                                          |                              |
| `GuiMainWindow`          |      ✅       | `Title`, `SendVKey`, `HardCopy`, `Maximize`                                                                      |                              |
| `GuiTextField`           |      ✅       | `Text`, `MaxLength`, `IsReadOnly`, `CaretPosition`                                                               |                              |
| `GuiPasswordField`       |     ✅\*      | `Text` set (write-only)                                                                                          | Falls back to `GuiTextField` |
| `GuiButton`              |      ✅       | `Text`, `Press()`                                                                                                |                              |
| `GuiLabel`               |      ✅       | `Text` (read-only)                                                                                               |                              |
| `GuiCheckBox`            |      ✅       | `Selected`                                                                                                       |                              |
| `GuiRadioButton`         |      ✅       | `Selected`                                                                                                       |                              |
| `GuiComboBox`            |      ✅       | `Key`, `Value`, `Entries`                                                                                        |                              |
| `GuiStatusbar`           |      ✅       | `Text`, `MessageType`, `IsError/IsWarning/IsSuccess`                                                             |                              |
| `GuiTable`               |      ✅       | `RowCount`, `ColumnCount`, `GetCellValue`, `SetCellValue`, `GetVisibleRows`, `SelectRow`                         |                              |
| `GuiGridView`            |      ✅       | `RowCount`, `ColumnNames`, `GetCellValue`, `GetRows`, `ClickCell`, `PressToolbarButton`, `SelectAll`             |                              |
| `GuiTree`                |      ✅       | `ExpandNode`, `CollapseNode`, `SelectNode`, `DoubleClickNode`, `GetNodeText`, `GetChildNodeKeys`, `SelectedNode` |                              |
| `GuiTabStrip`            |      ✅       | `TabCount`, `GetTabs()`, `GetTab(int)`, `GetTabByName(string)`, `SelectTab(int)`                                 | **New in 1.3.0**             |
| `GuiTab`                 |      ✅       | `Text`, `Select()`                                                                                               | **New in 1.3.0**             |
| `GuiToolbar`             |      ✅       | `ButtonCount`, `PressButton(int)`, `PressButtonById(string)`, `GetButtonTooltip(int)`                            | **New in 1.3.0**             |
| `GuiMenubar`             |      ✅       | `Count`, `SelectItem(string)`                                                                                    | **New in 1.3.0**             |
| `GuiMenu` / `GuiSubMenu` |      ✅       | `Text`, `Select()`, `GetChildren()`                                                                              | **New in 1.3.0**             |
| `GuiContextMenu`         |      ✅       | `SelectByFunctionCode(string)`, `GetItemTexts()`, `Close()`                                                      | **New in 1.3.0**             |
| `GuiMessageWindow`       |      ✅       | `Text`, `MessageType`, `ClickOk()`, `ClickCancel()`, `ClickButton(string)`, `SendVKey(int)`, `GetButtons()`      | **New in 1.3.0**             |
| `GuiModalWindow`         |      🔶       | Available via `session.GetActivePopup()` → `dynamic`                                                             | Use `findById` for children  |
| `GuiFrameWindow`         |      🔶       | `Title` via base `GuiComponent`                                                                                  |                              |
| `GuiUserArea`            |      🔶       | Container; navigate with `findById`                                                                              |                              |
| `GuiCustomControl`       |      🔶       | Container; navigate with `findById`                                                                              |                              |
| `GuiShell` (generic)     |      🔶       | Use `findById` → `dynamic` for all shell methods                                                                 | Many sub-types               |
| `GuiSplitterContainer`   |      🔶       | Container; navigate with `findById`                                                                              |                              |
| `GuiScrollContainer`     |      🔶       | Container; navigate with `findById`                                                                              |                              |
| `GuiCalendar`            |      ❌       | Not yet wrapped; use `findById` → `dynamic`                                                                      |                              |
| `GuiOfficeIntegration`   |      ❌       | Not yet wrapped; use `findById` → `dynamic`                                                                      |                              |
| `GuiHTMLViewer`          |      ❌       | Not yet wrapped; use `findById` → `dynamic`                                                                      |                              |
| `GuiChart`               |      ❌       | Not yet wrapped; use `findById` → `dynamic`                                                                      |                              |
| `GuiMap`                 |      ❌       | Not yet wrapped; use `findById` → `dynamic`                                                                      |                              |

**Legend:** ✅ typed wrapper · 🔶 accessible via dynamic / base class · ❌ not yet implemented

### Events

SAP GUI Scripting COM events (`GuiSession.Change`, `GuiSession.Destroy`, etc.) are **not currently exposed** as .NET events. Use `WaitReady()` as a polling alternative after navigation steps.

---

## Building the NuGet package

```powershell
cd SapGui.Wrapper
dotnet pack -c Release -o ../nupkg
```

The `.nupkg` file appears in the `nupkg/` folder. Add that folder as a NuGet feed in UiPath Studio.

---

## Architecture

```
SapGui.Wrapper/
├── SapGuiClient.cs          ← main entry point  (Attach / GetSession)
├── SapGuiHelper.cs          ← static one-liner helpers for UiPath
├── Exceptions.cs            ← SapGuiNotFoundException, SapComponentNotFoundException
├── GlobalUsings.cs
│
├── Com/
│   └── SapRot.cs            ← ROT access (SapROTWr + P/Invoke fallback)
│
├── Core/
│   ├── GuiComponent.cs      ← base wrapper (Text, Press, SetFocus, late-binding helpers)
│   ├── GuiApplication.cs    ← GetConnections()
│   ├── GuiConnection.cs     ← GetSessions()
│   ├── GuiSession.cs        ← FindById, typed finders, StartTransaction, …
│   └── GuiMainWindow.cs     ← SendVKey, HardCopy, Maximize
│
├── Elements/
│   ├── GuiTextField.cs      ← Text get/set, MaxLength, IsReadOnly
│   ├── GuiButton.cs         ← Press()
│   ├── GuiComboBox.cs       ← Key, Value, Entries
│   ├── GuiCheckBox.cs       ← Selected
│   ├── GuiRadioButton.cs    ← Selected
│   ├── GuiLabel.cs          ← Text (read-only)
│   ├── GuiStatusbar.cs      ← Text, MessageType, IsError/IsWarning/IsSuccess
│   ├── GuiTable.cs          ← GetCellValue, SetCellValue, GetVisibleRows
│   ├── GuiGridView.cs       ← GetCellValue, GetRows, ClickCell, PressToolbarButton
│   ├── GuiTree.cs           ← ExpandNode, SelectNode, GetChildNodeKeys          [1.2]
│   ├── GuiTabStrip.cs       ← TabCount, GetTabs, SelectTab  +  GuiTab           [1.3]
│   ├── GuiToolbar.cs        ← PressButton, PressButtonById, GetButtonTooltip    [1.3]
│   ├── GuiMenu.cs           ← GuiMenubar, GuiMenu, GuiContextMenu               [1.3]
│   └── GuiMessageWindow.cs  ← ClickOk, ClickCancel, ClickButton                [1.3]
│
└── Enums/
    └── SapComponentType.cs  ← enum + string→enum mapping
```

---

## Version history

| Version   | Changes                                                                                                                                                                                                                                                                                                                                      |
| --------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **1.3.0** | Added `GuiTabStrip`, `GuiTab`, `GuiToolbar`, `GuiMenubar`, `GuiMenu`, `GuiContextMenu`, `GuiMessageWindow` typed wrappers. Added `GuiSession` typed finders: `TabStrip()`, `Tab()`, `Toolbar()`, `Menubar()`, `Menu()`, `Tree()`, `GetActivePopup()`. Enum extended with `GuiMenu`, `GuiContextMenu`, `GuiCalendar`, `GuiOfficeIntegration`. |
| **1.2.0** | Single `SapGui.Wrapper` namespace (one `using` statement). `GuiTree` wrapper. `findById` camelCase dynamic alias. `Text`, `Press()`, `SetFocus()` promoted to base `GuiComponent`.                                                                                                                                                           |
| **1.1.0** | `FindByIdDynamic`, typed `FindById<T>`, `SapGuiHelper` static helpers.                                                                                                                                                                                                                                                                       |
| **1.0.0** | Initial release. Core + primary element wrappers. ROT P/Invoke fallback.                                                                                                                                                                                                                                                                     |

---

## License

MIT
