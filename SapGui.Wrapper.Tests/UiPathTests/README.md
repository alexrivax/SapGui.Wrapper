# SapGui.Wrapper – UiPath Studio Integration Tests

This folder is a **standalone UiPath Studio 2023.10+ project** that exercises every
public API surface of the `SapGui.Wrapper` library via Coded Workflows.

## Requirements

| Requirement | Details |
|---|---|
| UiPath Studio | 2023.10 or later (Coded Workflow / .NET 6 support required) |
| SAP GUI | Installed, running, and logged on to a test system |
| SAP Scripting | Enabled – in SAP GUI: *Options → Accessibility & Scripting → Scripting → Enable* |
| NuGet package | `SapGui.Wrapper` ≥ 0.8.0 (resolved automatically via `project.json`) |

## Opening the project

1. In UiPath Studio choose **Open** and browse to this `UiPathTests` folder.
   Studio reads `project.json` and restores NuGet packages automatically.
2. Build the project once to verify all dependencies resolve.

## Running the tests

### Run all tests via Main.xaml
Open `Main.xaml` and press **Run** (or F5).  
All 14 tests execute in sequence; results are written to the UiPath log panel.

### Run a single test
Right-click any `.cs` file in the Project panel and choose **Run File**.  
Each coded workflow is self-contained and can be run independently.

## Adapting control IDs to your system

Every test uses SAP Script-Recorder ID paths (`"wnd[0]/usr/xxx"`).  
Lines marked `← ADAPT` must be updated to match your target SAP system.

Quick method: run the **SAP Script Recorder** while navigating to the same
transaction, then copy the ID strings into the test file.

## Test inventory

| File | Transaction | What is tested |
|------|-------------|----------------|
| `Test_01_Session.cs` | any | `GuiSessionInfo`, `GetConnections`, `IsBusy`, `WaitReady`, `ActiveWindow` |
| `Test_02_MainWindow.cs` | any | `Title`, `IsMaximized`, `Maximize`, `Iconify`, `Restore`, `HardCopy` |
| `Test_03_Navigation.cs` | SE16 | `StartTransaction`, `ExitTransaction`, `PressBack`, `SendVKey` |
| `Test_04_TextField.cs` | SE16 | `GuiTextField` read/write, `MaxLength`, `IsReadOnly`, `DisplayedText`, `CaretPosition` |
| `Test_05_Statusbar.cs` | SE16 | `GuiStatusbar` `MessageType`, `IsError`, `IsWarning`, `IsSuccess` |
| `Test_06_ComboBox.cs` | SU3 | `GuiComboBox` `Key`, `Value`, `ShowKey`, `IsReadOnly` |
| `Test_07_GridView.cs` | SM37 | `GuiGridView` `RowCount`, `ColumnNames`, `GetCellValue`, `SelectAll`, `GetRows` |
| `Test_08_Table.cs` | SE16→T000 | `GuiTable` `GetCellValue`, `GetVisibleRows`, `ScrollToRow` |
| `Test_09_Tree.cs` | Easy Access | `GuiTree` `GetAllNodeKeys`, `ExpandNode`, `SelectNode`, `NodeContextMenu` |
| `Test_10_TabStrip.cs` | SU3 | `GuiTabStrip` `GetTabs`, `SelectTab`, `GetTabByName`, `Tab.Select` |
| `Test_11_Toolbar_Menu.cs` | SE16 | `GuiToolbar` `ButtonCount`/`GetButtonTooltip`; `GuiMenubar`+`GuiMenu` children |
| `Test_12_UserArea_ScrollContainer.cs` | SE16 | `GuiUserArea.FindById`, `GuiScrollContainer` scrollbar position |
| `Test_13_Popup.cs` | SE16 | `GetActivePopup`, `GuiMessageWindow` `GetButtons`, `ClickOk`/`ClickCancel` |
| `Test_14_Events.cs` | SE16 | `StartMonitoring`, `StopMonitoring`, `Change`, `StartRequest`, `EndRequest` |

## Logging conventions

All tests log to the UiPath output panel:

- **Info** (default) – field values and step confirmations  
- **Warn** – unexpected values that do not abort the test  
- **Error** – `AbapRuntimeError` events  

No test throws an exception for an unexpected value; every test ends with a
`PASSED` log line even when some values could not be verified.
Where a comment says `// ← ADAPT`, replace the ID / value with one from your system.
Use **Alt+F12 → Script Recording** in SAP GUI to capture the IDs you need.
