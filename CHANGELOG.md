# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/) and this project adheres to [Semantic Versioning](https://semver.org/).

## [1.1.0] – 2026-04-03 — `SapGui.Wrapper.Mcp` (new package)

### New package

- **`SapGui.Wrapper.Mcp` 1.1.0** — MCP stdio server (`sapgui-mcp` dotnet tool) that exposes SAP GUI automation as 18 typed MCP tools consumable by any MCP-capable host (Claude Desktop, VS Code Copilot Agent, Cursor, and others). Includes a screen observation layer (typed snapshots, JSON context) and semantic action façade (fuzzy field/button resolution via label) bundled as an internal dependency — no separate package needed. All SAP COM calls are marshalled onto a dedicated STA thread. Includes built-in guardrails: 12 dangerous transactions blocked by default and a `ReadOnlyMode` flag that restricts the server to observation-only tools.

### Infrastructure

- `Directory.Build.props` — single source of truth for `SharedVersion`, shared NuGet metadata (Authors, License, URLs), and compiler settings (`LangVersion latest`, `Nullable enable`, `ImplicitUsings enable`, `Deterministic true`) across all projects.
- CI (`ci.yml`) — dry-run `dotnet pack` for both packages on every PR.
- Publish (`publish.yml`) — reads version from `Directory.Build.props`, builds, packs, and pushes both packages on every push to `main`.

## [1.0.0] – 2026-03-11

### First stable release

SapGui.Wrapper is now considered production-ready for general use. All core APIs, element wrappers, events, retry/wait mechanisms, logging, health checks, and SSO support have been validated through extensive integration testing and iterative refinement across the 0.x series.

### Breaking changes

- **`GuiTable.VerticalScrollbarPosition` removed** — this property was deprecated in 0.5.0 in favour of `FirstVisibleRow`. Use `FirstVisibleRow` instead.
- **`GuiComboBox.SetKeyAndFireEvent` removed** (since 0.8.4) — use `cb.Key = value` instead.

### Summary of capabilities (since 0.0.0)

- **20+ typed element wrappers** — `GuiTextField`, `GuiButton`, `GuiGridView`, `GuiTable`, `GuiTree`, `GuiComboBox`, `GuiCheckBox`, `GuiRadioButton`, `GuiTabStrip`, `GuiToolbar`, `GuiMenu`, `GuiStatusbar`, `GuiMessageWindow`, `GuiCalendar`, `GuiHTMLViewer`, `GuiScrollContainer`, `GuiUserArea`, `GuiShell`, `GuiLabel`, and more.
- **Session management** — `SapGuiClient.Attach()`, `LaunchWithSso()`, `HealthCheck()`, `EnsureHealthy()`, `GetSession()`, multi-connection/multi-session support.
- **Resilient automation** — `WaitReady`, `WaitForReadyState`, `ElementExists`, `WaitUntilHidden`, `WithRetry` (retries on `SapComponentNotFoundException` and `TimeoutException`).
- **COM event sink** — `StartMonitoring()` connects a true COM event sink (with polling fallback): `StartRequest`, `EndRequest`, `Change`, `Destroy`, `AbapRuntimeError`.
- **Logging** — `ILogger` and `SapLogAction` delegate overloads, zero overhead when unconfigured.
- **Post-login popup handling** — `DismissPostLoginPopups` handles multiple-logon dialogs, license warnings, system messages.
- **NuGet hardening** — deterministic builds, SourceLink, SBOM generation, package signing script.
- **COM lifecycle safety** — all intermediate COM objects released via `try/finally` + `Marshal.ReleaseComObject`; `SapGuiClient` and `GuiSession` are `IDisposable`.
- **GitHub Pages documentation** — full API reference (auto-generated from XML docs via DocFX), Getting Started, Common Patterns, Logging, Resilient Automation, Health Check, Session Events, Best Practices, Troubleshooting, and FAQ articles published at `https://alexrivax.github.io/SapGui.Wrapper`.

## [0.9.2] – 2026-03-10

### Changed

- **`SapGuiClient.LaunchWithSso` — new `reuseExistingSession` parameter** — Instead of attempting to dismiss the SAP Logon "License information for multiple logons" dialog programmatically (which proved unreliable because the dialog is a native Win32 window owned by `saplogon.exe`, outside the SAP scripting API), `LaunchWithSso` now detects an existing open session for the requested system **before** calling `OpenConnection`, preventing the dialog from ever appearing. Pass `reuseExistingSession: true` to reuse the existing session; omit it (default `false`) to receive a clear `InvalidOperationException` with instructions.

### Removed

- All Win32 dialog-dismissal code (`EnumWindows`, `keybd_event`, `SetForegroundWindow`, background watcher task) introduced in 0.9.1 — replaced by the pre-flight session check above.

## [0.9.1] – 2026-03-10

### Bug fix

- **`DismissPostLoginPopups` now handles the "License information for multiple logons" dialog** — When a user is already logged on to the same SAP system, SAP shows a radio-button dialog titled _"License information for multiple logons"_ before the standard session-management popup. The dialog was not matched by the existing heuristics and was left untouched, blocking automation. A dedicated check is now inserted before the generic `MULTIPLE LOGON` branch: when the title contains `"License information for multiple logons"`, `DismissPostLoginPopups` presses Enter (VKey 0), accepting the default option _"Continue without ending other logon"_ — the non-destructive, license-safe choice.

## [0.9.0] – 2026-03-09

### Added

- **`SapGuiClient.HealthCheck()`** — static, never-throws pre-flight method that returns a `HealthCheckResult` record (`IsHealthy`, `Findings`, `FailureSummary`). Runs five ordered checks: SAP GUI process is running → scripting API accessible via ROT → at least one connection exists → at least one session exists → session info (user / system / client) is readable. Each finding is prefixed `OK:`, `WARN:`, or `FAIL:` for easy filtering. The temporary COM reference obtained during the check is released in a `finally` block.
- **`SapGuiClient.EnsureHealthy()`** — convenience throwing variant: calls `HealthCheck()` and raises `InvalidOperationException` with all `FAIL:` lines when any check fails. Designed for fail-fast workflows.
- **`HealthCheckResult`** record — `IsHealthy` (`bool`), `Findings` (`IReadOnlyList<string>`), `FailureSummary` (FAIL lines joined by newline), `ToString()` (all findings).
- **`Polyfills.cs`** — internal `System.Runtime.CompilerServices.IsExternalInit` shim (guarded by `#if !NET5_0_OR_GREATER`) enabling C# 9 `record` types on net461 with zero runtime cost.

### NuGet / build hardening

- **Transitive dependencies pinned** — `Microsoft.Build.Tasks.Git` and `Microsoft.SourceLink.Common` are now explicitly listed at `8.0.0` in the `.csproj`, matching the already-present `Microsoft.SourceLink.GitHub 8.0.0`. This produces a fully deterministic, auditable NuGet dependency graph required by enterprise artifact repositories (Artifactory, Azure Artifacts with policy enforcement).
- **SBOM on every pack** — `GenerateSbom` AfterPack MSBuild target invokes `dotnet CycloneDX` and writes `SapGui.Wrapper-{version}-sbom.cdx.json` alongside the `.nupkg`. The tool is pinned to `cyclonedx 3.0.8` in `.config/dotnet-tools.json`. Run `dotnet tool restore` once before the first pack.
- **Package signing script** — `scripts/New-SigningCert.ps1` creates a self-signed code-signing certificate (RSA-3072, SHA-256, 5-year validity), exports it as a PFX, and signs all `.nupkg` files in `nupkg/` via `dotnet nuget sign` with a DigiCert timestamp. `*.pfx` and `*.p12` are now excluded by `.gitignore`.

## [0.8.7] – 2026-03-09

### Added

- **`RetryPolicy`** — new class returned by `session.WithRetry(maxAttempts, delayMs)`. Call `.Run(action)` or `.Run<T>(func)` to execute an operation with automatic retries on `SapComponentNotFoundException` (slow screen loads) and `TimeoutException` (session still busy). `SapGuiNotFoundException` is never retried — it is a fatal setup error.
- **`GuiSession.WithRetry(maxAttempts, delayMs)`** — fluent entry point that creates a `RetryPolicy` scoped to the current session.
- **`GuiSession.WaitForReadyState(timeoutMs, pollMs, settleMs)`** — more reliable alternative to `WaitReady`. After the busy flag clears it waits an additional settle period, then verifies the main window COM object is still accessible. Catches the brief second busy pulse that `WaitReady` can miss during screen transitions.
- **`GuiSession.ElementExists(id, timeoutMs, pollMs)`** — polls until a component ID is accessible or the timeout elapses. Returns `bool`. Use instead of a try/catch around `FindById` when you need an explicit wait.
- **`GuiSession.WaitUntilHidden(id, timeoutMs, pollMs)`** — polls until a component ID is no longer accessible. Returns `bool`. Use to wait out loading spinners or processing dialogs.

## [0.8.6] – 2026-03-09

### Bug fixes

- **`GuiTable.ColumnCount` always returned 0** — `GuiTableControl` exposes columns via a `Columns` collection, not a flat `ColumnCount` integer property. `GetInt("ColumnCount")` silently caught `DISP_E_UNKNOWNNAME` and returned 0. Fixed to read `Columns.Count` via the collection object.
- **`GuiTable.FirstVisibleRow` always returned 0 / `ScrollToRow` threw `DISP_E_UNKNOWNNAME`** — Both used dotted property paths (`"VerticalScrollbar.Position"`) passed to `InvokeMember`, which does not resolve nested COM objects. Fixed to retrieve the `VerticalScrollbar` object first, then get/set `Position` on it.
- **`GuiTable.GetCellValue` always returned empty string** — `GetCellRaw` was navigating `Rows.Item(row).Item(col)`, which does not work on `GuiTableControl`. Fixed to call `GetCell(row, col)` directly on the table COM object.
- **`GuiTable.GetVisibleRows` returned all rows instead of visible rows** — Loop iterated `RowCount` (total) starting at index 0. SAP only populates COM cells for the currently visible viewport; off-screen rows always return empty values. Fixed to iterate `VisibleRowCount` rows beginning at `FirstVisibleRow`.

## [0.8.5] – 2026-03-09

### Bug fix

- **`session.Table()` failed with `GuiTableControl` type** — Some SAP systems return `"GuiTableControl"` from the COM `Type` property instead of `"GuiTable"` for the same classic ABAP table control. `WrapComponent` now maps both `GuiTable` and `GuiTableControl` to `GuiTable`, so `session.Table(id)` works regardless of which type string the system reports.

## [0.8.4] – 2026-03-09

### Breaking change

- **`GuiComboBox.SetKeyAndFireEvent` removed** — SAP GUI does not expose `FireSelectEvent` on `GuiComboBox`; the method was equivalent to setting `Key` directly. Use `cb.Key = value` followed by `session.WaitReady()` or `session.SendVKey(0)` if field-level PAI validation is needed.

## [0.8.3] – 2026-03-09

### Bug fix

- **`GuiMainWindow.IsMaximized` always returned `false`** — The SAP COM `IsMaximized` property is not consistently updated after `Maximize()` / `Restore()` across SAP GUI versions. Replaced the COM property read with a Win32 `IsZoomed(HWND)` P/Invoke call, using the HWND already exposed by SAP's `GuiFrameWindow.Handle`. Also added `GuiMainWindow.Handle` (`IntPtr`) as a new public property.

## [0.8.2] – 2026-03-09

### Bug fixes

- **`GetBool` silent VARIANT_BOOL cast failure** — `GuiComponent.GetBool` was casting the COM return value with `(bool)` directly. SAP's COM layer returns `VARIANT_BOOL` as a boxed `short` (-1 = true, 0 = false); the cast threw `InvalidCastException`, which the `catch` swallowed and returned `false`. Changed to `Convert.ToBoolean(val)`, which handles `bool`, `short`, and `int` correctly. This was a silent bug affecting every boolean property across the wrapper: `IsMaximized`, `IsBusy`, `IsReadOnly`, `IsRequired`, `IsOField`, `IsModified`, `Changeable`, `DisabledByServer`, `ShowKey`, `IsDialog`, and all `GetBool` call sites.

### GuiSession — `StartTransaction` documentation

- Expanded XML doc on `StartTransaction(tCode)` to document the `/n` prefix requirement when calling from inside an existing transaction (`"/nSE16"`), the `/o` prefix to open in a new session, and the difference from bare code (only reliable from Easy Access menu).

### UiPath Studio project (UiPathTests)

- `SapGui.Wrapper.Tests/UiPathTests/` is now a standalone **UiPath Studio 2023.10+ project**: added `project.json` (NuGet dependencies, net6.0-windows target) and `Main.xaml` (sequence that calls all 14 tests via `InvokeWorkflowFile`).
- All 14 coded workflow files updated: unified namespace `SapGuiWrapperTests`, added all missing `using` directives (`System`, `System.Collections.Generic`, `System.Linq`, `System.IO`, `System.Threading`, `UiPath.Core`), removed unused UiPath package imports.
- **Test 01** — `GuiConnection.Description` → `GuiConnection.Host` (property was renamed); `application.ActiveSession` wrapped in try/catch with `session.MainWindow().SetFocus()` before the call to handle OS-focus dependency.
- **Test 03** — `StartTransaction("SE16")` → `StartTransaction("/nSE16")` so navigation works when already inside another transaction.
- **Test 11** — `GuiMenubar.GetChildren()` does not exist; replaced with index-based loop using `session.Menu("wnd[0]/mbar/menu[{i}]")` to retrieve top-level `GuiMenu` items.
- `SapGui.Wrapper.Tests.csproj` — added `<Compile Remove="UiPathTests\**\*.cs" />` to exclude UiPath files from the C# build.

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
- ~~`SetKeyAndFireEvent(key)`~~ — removed in 0.8.4; use `cb.Key = value` instead.

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

- Added `FirstVisibleRow` — index of the first visible row (replaces `VerticalScrollbarPosition`, removed in 1.0.0).
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
