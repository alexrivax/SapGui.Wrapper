# Common Patterns

## Reading a classic ABAP table

```csharp
var table    = session.Table("wnd[0]/usr/tblSAPLXXX");
int total    = table.RowCount;
int pageSize = table.VisibleRowCount;

for (int start = 0; start < total; start += pageSize)
{
    table.ScrollToRow(start);
    for (int r = start; r < Math.Min(start + pageSize, total); r++)
        Console.WriteLine(table.GetCellValue(r, 0));
}
```

## Reading an ALV grid (scroll-aware)

```csharp
var grid = session.GridView("wnd[0]/usr/cntlGRID/shellcont/shell");

Log($"Total: {grid.RowCount}, visible: {grid.VisibleRowCount}, first: {grid.FirstVisibleRow}");

// Navigate focus before reading tooltips or checkboxes
grid.SetCurrentCell(3, "STATUS");
string tooltip = grid.GetCellTooltip(3, "STATUS");
bool   flagged = grid.GetCellCheckBoxValue(3, "CRITICAL");

// Get selected rows after SelectAll
grid.SelectAll();
IReadOnlyList<int> selected = grid.SelectedRows;
```

### Bulk row read with column mapping

```csharp
var rows = grid.GetRows(new[] { "MATNR", "MAKTX", "LABST" });
foreach (var row in rows)
    Console.WriteLine($"{row["MATNR"]} | {row["MAKTX"]} | {row["LABST"]}");
```

## Handling popups

`GetActivePopup()` returns a unified wrapper for both `GuiMessageWindow` (pure dialogs)
and `GuiModalWindow` (form dialogs):

```csharp
var popup = session.GetActivePopup();
if (popup != null)
{
    Log($"[{popup.MessageType}] {popup.Title}: {popup.Text}");

    // Standard buttons
    popup.ClickOk();
    // or popup.ClickCancel()

    // Custom button by partial text match
    popup.ClickButton("Continue");

    // Inspect all buttons
    foreach (var btn in popup.GetButtons())
        Log(btn.Text);
}
```

### Status bar — check after every navigation

```csharp
session.PressExecute();
session.WaitForReadyState(timeoutMs: 15_000);

var status = session.Statusbar();
if (status.IsError)
    throw new Exception($"SAP error: {status.Text}");
```

## Tabs, toolbars, and menus

```csharp
// Select a tab by index
session.TabStrip("wnd[0]/usr/tabsTABSTRIP").SelectTab(1);

// Press a toolbar button by SAP function code
session.Toolbar().PressButtonByFunctionCode("BACK");

// Navigate a menu item
session.Menu("wnd[0]/mbar/menu[4]/menu[2]").Select();
```

## Tree controls

```csharp
var tree = session.Tree("wnd[0]/usr/cntlTREE/shellcont/shell");

tree.ExpandNode("ROOT");
foreach (var key in tree.GetChildNodeKeys("ROOT"))
    Log($"{key}: {tree.GetNodeText(key)}");

tree.SelectNode("CHILD01");
tree.DoubleClickNode("CHILD01");

// Multi-column tree cell
string cellValue = tree.GetItemText("CHILD01", "AMOUNT");

// Context menu
tree.NodeContextMenu("CHILD01");
```

## Combo boxes

```csharp
var cb = session.ComboBox("wnd[0]/usr/cmbVBAK-VKORG");

// List all available entries
foreach (var entry in cb.Entries)
    Log($"{entry.Key}: {entry.Value}");

// Set by key
cb.Key = "1000";
session.SendVKey(0); // PAI validation
```

## Multi-session workflows

```csharp
var sap = SapGuiClient.Attach();

// Get sessions by index
var session0 = sap.GetSession(connectionIndex: 0, sessionIndex: 0);
var session1 = sap.GetSession(connectionIndex: 0, sessionIndex: 1);

// Open a new parallel session on demand
session0.CreateSession();
var newSession = sap.Application
                    .GetFirstConnection()
                    .GetSessions()
                    .Last();
```

## Connecting to a new SAP system

```csharp
var app        = GuiApplication.Attach();
var connection = app.OpenConnection("PRD - Production"); // SAP Logon Pad entry name
var session    = connection.GetFirstSession();

// Or get whichever session has focus
var active = app.ActiveSession;
```

## UserArea — relative IDs

`GuiUserArea` (`wnd[0]/usr`) lets you address children with short relative IDs instead of
the full qualified path:

```csharp
var usr = session.UserArea(); // defaults to "wnd[0]/usr"

var field = usr.FindById<GuiTextField>("txtMM01-MATNR");
field.Text = "MAT-0042";
```

## Scroll containers

```csharp
var sc = session.ScrollContainer("wnd[0]/usr/shellcont");
sc.VerticalScrollbar.Position = 10;
sc.ScrollToTop();
```

## Calendar controls

```csharp
var cal = session.Calendar("wnd[0]/usr/cntlCAL/shellcont/shell");
cal.SetDate(new DateTime(2026, 3, 31));
DateTime? selected = cal.GetSelectedDate();
```
