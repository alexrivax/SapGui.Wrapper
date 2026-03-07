# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/) and this project adheres to [Semantic Versioning](https://semver.org/).

## [0.8.1] – 2026-03-07

### Security — COM lifecycle hardening

- `SapRot.GetGuiApplication()` now releases all intermediate COM objects (`rotWrapper`, `sapGuiRaw`, `IRunningObjectTable`, per-iteration `IBindCtx`) via `try/finally` + `Marshal.ReleaseComObject`. No COM references leak after `Attach()` completes.
- `SapGuiClient.Dispose()` now explicitly calls `Marshal.ReleaseComObject(Application.RawObject)` instead of relying on the GC finalizer, returning the COM reference to SAP GUI immediately.
- `GuiSession` now implements `IDisposable`. `Dispose()` calls `StopMonitoring()` first (disconnects COM event sinks and stops any polling thread) then releases the session COM RCW. Safe to use in `using` blocks.

### SapGuiClient — new `LaunchWithSso` factory

- `SapGuiClient.LaunchWithSso(systemDescription, connectionTimeoutMs)` — starts `saplogon.exe` if it is not already running, waits for SAP GUI to register in the Windows ROT, opens the connection by SAP Logon Pad entry name (SSO-configured systems complete without a credential dialog), and polls until a non-busy session is available before returning. Throws `SapGuiNotFoundException`, `InvalidOperationException`, or `TimeoutException` on failure — never silently succeeds with an unusable session.

### GuiSession — new `DismissPostLoginPopups`

- `DismissPostLoginPopups(maxPopups, timeoutMs)` — automatically dismisses post-SSO / post-login dialogs: "User already logged on / Multiple Logon" (clicks **Continue** to keep both sessions alive), license expiration warnings, system message banners, and any single-button information dialog. Unrecognised multi-button dialogs are left untouched. Returns the count of dismissed popups.

## [0.8.0]

### GuiSession — COM event sink (architectural improvement)

- `StartMonitoring()` now first tries to connect a true COM event sink to the SAP session via `IConnectionPointContainer::EnumConnectionPoints` / `IConnectionPoint::Advise`. If the COM sink connects successfully, all five SAP session events are driven by the native SAP COM source — no polling thread is started.
- Falls back automatically to the previous polling-based monitor when the COM object does not support connection points (e.g. older SAP GUI versions).
- `SessionChangeEventArgs.FunctionCode` is now populated (e.g. `"BACK"`, `"EXEC"`) when the COM event sink is active. It remains empty in the polling fallback.

### GuiSession — new `StartRequest` / `EndRequest` events

- Added `StartRequest` event (`StartRequestEventArgs`: `Text`) — fires at the beginning of a server round-trip. When using the COM sink this is a true SAP event; in the polling fallback it fires when `IsBusy` first becomes `true`.
- Added `EndRequest` event (`EndRequestEventArgs`: `Text`, `FunctionCode`, `MessageType`) — fires at the end of a server round-trip, before `Change`. Provides a precise `WaitReady` alternative when combined with `StartMonitoring()`.

### Internal

- Added `Com/ComConnectionPoint.cs` — declares `IConnectionPoint`, `IConnectionPointContainer`, `IEnumConnectionPoints` COM interfaces (standard OLE GUIDs; not present in .NET 6+ BCL).
- Added `Com/GuiSessionComSink.cs` — `[ComVisible]` `IReflect`-based dispatch sink. Routes both name-based and DISPID-based SAP event calls to the correct .NET event raisers.

## [0.7.0]

### GuiScrollContainer (new wrapper)

- Added `GuiScrollContainer` typed wrapper for SAP GUI scroll container controls.
- `VerticalScrollbar` and `HorizontalScrollbar` properties return a `GuiScrollbar` object exposing `Position` (read/write), `Minimum`, `Maximum`, and `PageSize`.
- `ScrollToTop()` convenience method sets the vertical scroll position to its minimum.
- `session.ScrollContainer(id)` typed finder added to `GuiSession`.

### GuiUserArea (new wrapper)

- Added `GuiUserArea` typed wrapper for the dynpro content area (typically `wnd[0]/usr`).
- `FindById(relativeId)` and `FindById<T>(relativeId)` allow child lookup with short relative IDs instead of fully qualified paths.
- `session.UserArea(id)` typed finder added to `GuiSession` (defaults to `"wnd[0]/usr"`).

### GuiCalendar (new wrapper)

- Added `GuiCalendar` typed wrapper for SAP GUI calendar controls.
- `FocusedDate` property returns the focused date as a nullable `DateTime`.
- `SetDate(DateTime)` — sets the focused/selected date.
- `GetSelectedDate()` — returns the currently selected date.
- `session.Calendar(id)` typed finder added to `GuiSession`.

### GuiHTMLViewer (new wrapper)

- Added `GuiHTMLViewer` typed wrapper for embedded HTML viewer controls.
- `BrowserHandle` — Win32 handle of the embedded browser control.
- `FireSapEvent(eventName, param1, param2)` — fires a named SAP event defined inside the embedded HTML page.
- `session.HtmlViewer(id)` typed finder added to `GuiSession`.

### GuiShell (new wrapper)

- Added `GuiShell` as a typed generic fallback wrapper for SAP shell controls whose specific sub-type is not individually wrapped.
- `SubType` property exposes the shell sub-type string (e.g. `"GridView"`, `"TreeView"`, `"Chart"`).
- `session.Shell(id)` typed finder added to `GuiSession`.
- `WrapComponent` now routes `GuiShell` type strings to `GuiShell` instead of falling through to the bare `GuiComponent` base.

## [0.6.0]

### GuiTextField

- Added `DisplayedText` property — returns the formatted/displayed value as shown in SAP GUI (may differ from `Text` on amount or date fields where SAP applies locale-specific formatting).
- Added `IsRequired` property — returns `true` if the field is mandatory (marked with `?`).
- Added `IsOField` property — returns `true` if the field is an output-only screen field.

### GuiMainWindow

- Added `Iconify()` — minimizes the window to the taskbar.
- Added `IsMaximized` property — returns `true` when the window is currently maximised.

### GuiTree

- Added `GetItemText(nodeKey, columnName)` — reads a cell value in a multi-column tree.
- Added `GetAllNodeKeys()` — returns a flat `IReadOnlyList<string>` of every node key in the tree.
- Added `NodeContextMenu(nodeKey)` — opens the right-click context menu for a node.
- Added `GetNodeType(nodeKey)` — returns the node type string (e.g. `"LEAF"`, `"FOLDER"`).

### GuiComboBox

- Added `ShowKey` property — returns `true` when the combo box shows the technical key rather than the description.
- Added `SetKeyAndFireEvent(key)` — sets the selected key and fires the SAP field-validation event, triggering ABAP PAI logic on the field.

## [0.5.0]

### GuiSession

- Added `ActiveWindow` property — returns the currently active (focused) window without guessing the `wnd[x]` index.
- Added `CreateSession()` — opens a new parallel SAP session on the same connection.
- Added polling-based .NET events: `Change`, `Destroy`, `AbapRuntimeError`. Call `StartMonitoring(pollMs)` / `StopMonitoring()` to activate. `SessionChangeEventArgs` carries `Text`, `FunctionCode`, and `MessageType`; `AbapRuntimeErrorEventArgs` carries `Message`.

### GuiApplication

- Added `ActiveSession` property — shortcut to the currently focused session.
- Added `OpenConnection(description, sync)` — opens a new connection by SAP Logon Pad entry name and returns the resulting `GuiConnection`.

### GuiGridView

- Added `FirstVisibleRow` and `VisibleRowCount` — viewport scroll-awareness properties.
- Added `CurrentCellRow` and `CurrentCellColumn` — focused cell coordinates.
- Added `SetCurrentCell(row, column)` — navigates focus to a cell before reading tooltips, checkboxes, or invoking context menus.
- Added `SelectedRows` — returns currently selected row indices as `IReadOnlyList<int>`.
- Added `GetCellTooltip(row, column)` — reads the tooltip text on a cell.
- Added `GetCellCheckBoxValue(row, column)` — reads boolean checkbox-type cells.
- Added `GetSymbolsForCell(row, column)` — reads icon/symbol column values.
- Added `PressEnter()` — confirms cell input.

### GuiTable

- Added `FirstVisibleRow` — index of the first visible row (replaces `VerticalScrollbarPosition`, now `[Obsolete]`).
- Added `VisibleRowCount` — number of rows visible in the current viewport.
- Added `ScrollToRow(row)` — scrolls the table to put the given row at the top.
- Added `CurrentCellRow` and `CurrentCellColumn` — row index and column key of the focused cell.

### README

- Rewritten with an RPA / UiPath Coded Workflow focus: new "Why this wrapper?" section, lead example as a full `CodedWorkflow`, consolidated "Common patterns" section covering all major automation scenarios.

## [0.4.6]

- Added `GuiSession.ExitTransaction()`: exits the current transaction and returns to the SAP Easy Access menu (writes `/n` to the command field and presses Enter).
- Added `GuiSession.PressExecute()`: sends F8 (VKey 8) to the main window, equivalent to clicking the Execute button on a selection screen.
- Fixed `GuiSession.SendVKey()` XML doc comment: corrected VKey-to-action mapping and expanded it to a reference table covering VKey 0/3/4/8/11/12/15.

## [0.4.5]

- Bug fix: `GuiMessageWindow.Text` now returns the actual message body text by reading the inner SAP text fields (`usr/txtMESSTXT1`…`usr/txtMESSTXT4`), joining non-empty lines with a space. Falls back to the window title if no inner fields exist.
- Bug fix: `GuiMessageWindow.Title` and `.Text` were previously identical (both returned the window title bar text).

## [0.4.4]

- Bug fix: `GetActivePopup()` now correctly returns a `GuiMessageWindow` when the popup is a SAP `GuiModalWindow` (e.g. dialog boxes with form fields). Previously it returned `null` because `WrapComponent` maps `GuiModalWindow` to `GuiMainWindow`, causing the typed cast to fail silently.

## [0.4.0]

- Bug fix: SapComponentType.FromString now correctly resolves GuiMenu, GuiContextMenu, GuiApoGrid, GuiCalendar and GuiOfficeIntegration (previously returned Unknown).
- Added GuiSession.RadioButton() typed convenience finder.
- Fixed SapGuiHelper.GetSession COM handle leak (now properly disposed).
- Full XML documentation on all public members (zero CS1591 warnings).

## [0.3.0]

- Added typed wrappers: GuiTabStrip, GuiTab, GuiToolbar, GuiMenubar, GuiMenu, GuiContextMenu, GuiMessageWindow.
- Added GuiSession convenience finders: TabStrip(), Tab(), Toolbar(), Menubar(), Menu(), Tree(), GetActivePopup().
- FindById() now returns typed instances for all newly added types.
- SapComponentType enum extended with GuiMenu, GuiContextMenu, GuiCalendar, GuiOfficeIntegration.

## [0.2.0]

- All public types moved into a single SapGui.Wrapper namespace. Only one import is now needed: Imports SapGui.Wrapper (VB.NET) or using SapGui.Wrapper; (C#).

## [0.1.0]

- Added findById(id) camelCase alias on GuiSession returning dynamic; recorder VBScript can be used in C# with only one change: add () to method calls.
- Added FindByIdDynamic(id) returning dynamic for full IDispatch late-binding.
- Promoted Text, Press() and SetFocus() to GuiComponent base class; FindById(id).Text / .Press() / .SetFocus() now work without casting.

## [0.0.0]

- Initial release.
