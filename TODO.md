# TODO – SapGui.Wrapper Roadmap

Based on gap analysis against the SAP GUI Scripting API spec (sap_gui_scripting_api.pdf).

---

## Priority 1 – High value, commonly used in automation

### GuiSession
- [x] `ActiveWindow` property — returns the currently active window (useful to detect which `wnd[x]` has focus without guessing the index)
- [x] `CreateSession()` — opens a new parallel session
- [x] Events: `Change`, `Destroy`, `AbapRuntimeError` — polling-based .NET events; call `StartMonitoring()` / `StopMonitoring()` to activate

### GuiApplication
- [x] `OpenConnection(description)` — launch and connect to a new SAP system
- [x] `ActiveSession` property — shortcut to the focused session

### GuiGridView
- [x] `SetCurrentCell(row, column)` — navigate to a cell before right-clicking or reading
- [x] `CurrentCellRow` / `CurrentCellColumn` properties
- [x] `SelectedRows` — list of currently selected row indices
- [x] `GetCellTooltip(row, column)` — read tooltip text on a cell
- [x] `GetCellCheckBoxValue(row, column)` — read boolean checkbox cells
- [x] `PressEnter()` — confirm cell input
- [x] `GetSymbolsForCell(row, column)` — read icon/symbol column values
- [x] `FirstVisibleRow` / `VisibleRowCount` — for scroll-aware reading

### GuiTable (classic ABAP table)
- [x] `FirstVisibleRow` / `VisibleRowCount` — understand visible range for scrolling
- [x] `ScrollToRow(row)` — scroll the table to a specific row
- [x] `CurrentCell` — get/set the focused cell (`CurrentCellRow` / `CurrentCellColumn`)

---

## Priority 2 – Useful for completeness

### GuiTextField
- [ ] `DisplayedText` — formatted/displayed value (may differ from `Text` on amount/date fields)
- [ ] `IsRequired` — whether the field is mandatory (marked with `?`)
- [ ] `IsOField` — output-only field flag

### GuiMainWindow
- [ ] `Iconify()` / `Minimize()` — minimize a window
- [ ] `IsMaximized` property

### GuiTree
- [ ] `GetItemText(nodeKey, columnName)` — for multi-column trees
- [ ] `GetAllNodeKeys()` — full flat key list across the entire tree
- [ ] `NodeContextMenu(nodeKey)` — right-click a node
- [ ] `GetNodeType(nodeKey)` — distinguish leaf vs folder nodes

### GuiComboBox
- [ ] `ShowKey` property — whether the display shows key or value
- [ ] `SetKeyAndFireEvent(key)` — set value and trigger ABAP field validation

---

## Priority 3 – New wrappers needed

- [ ] `GuiScrollContainer` — `VerticalScrollbar`, `HorizontalScrollbar`, `ScrollToTop()`
- [ ] `GuiUserArea` — wrapping it to enable `FindById` on the content area directly
- [ ] `GuiCalendar` — `SetDate(DateTime)`, `GetSelectedDate()`, `FocusDate`
- [ ] `GuiHTMLViewer` — `BrowserHandle`, `SapEvent` (fire link clicks/actions in embedded HTML)
- [ ] `GuiShell` (generic) — base fallback wrapper instead of falling through to `GuiComponent`

---

## Priority 4 – Events (architectural change, requires COM event sink)

Requires implementing `IConnectionPoint` sink for SAP COM events:

- [ ] `GuiSession.Change(GuiSession, GuiChangeArgs)` — fires after every round-trip; args contain `Text`, `FunctionCode`, `MessageType`
- [ ] `GuiSession.Destroy` — session/connection closed
- [ ] `GuiSession.AbapRuntimeError` — short dump triggered
- [ ] `GuiSession.StartRequest` / `EndRequest` — wraps a server round-trip (precise `WaitReady` alternative)

---

## Done ✅
- [x] `GetActivePopup()` — fixed `GuiModalWindow` returning `null` (0.4.4)
- [x] `GuiMessageWindow.Text` — now reads inner message body (`usr/txtMESSTXT1`…4), `Title` is the title bar (0.4.5)
- [x] `ExitTransaction()` — exits to SAP Easy Access menu via `/n` (0.4.6)
- [x] `PressExecute()` — sends F8 / Execute (0.4.6)
- [x] VKey reference table fixed in docs and README (0.4.6)
- [x] Priority 1 – GuiSession: `ActiveWindow`, `CreateSession()`, `Change`/`Destroy`/`AbapRuntimeError` events (polling-based) (0.5.0)
- [x] Priority 1 – GuiApplication: `OpenConnection(description)`, `ActiveSession` property (0.5.0)
- [x] Priority 1 – GuiGridView: `SetCurrentCell`, `CurrentCellRow`/`CurrentCellColumn`, `SelectedRows`, `GetCellTooltip`, `GetCellCheckBoxValue`, `PressEnter`, `GetSymbolsForCell`, `FirstVisibleRow`/`VisibleRowCount` (0.5.0)
- [x] Priority 1 – GuiTable: `FirstVisibleRow`, `VisibleRowCount`, `ScrollToRow(row)`, `CurrentCellRow`/`CurrentCellColumn` (0.5.0)
