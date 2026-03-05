# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/) and this project adheres to [Semantic Versioning](https://semver.org/).

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
