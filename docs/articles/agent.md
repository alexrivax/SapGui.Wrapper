# Agent Session

`SapGui.Wrapper.Agent` is a label-based façade for building custom SAP AI automation hosts in .NET. It lifts the programming model from raw SAP COM element IDs to human-readable field labels and provides structured, serialisable screen snapshots suitable for LLM context.

> [!NOTE]
> `SapGui.Wrapper.Agent` is not published separately to NuGet. It is bundled as a dependency of the
> [`SapGui.Wrapper.Mcp`](mcp-server.md) dotnet tool. To use it directly in a custom host, add a
> project reference from source.

## Getting Started

```csharp
using SapGui.Wrapper;
using SapGui.Wrapper.Agent.Actions;

using var sap    = SapGuiClient.Attach();
var       agent  = sap.Session.Agent();      // SapAgentSession

// Observe the current screen
var snapshot = agent.ScanScreen();
Console.WriteLine(snapshot.ToAgentContext()); // structured text for an LLM

// Take an action by label — no COM paths required
agent.StartTransaction("MM60");
agent.SetField("Plant", "1000");
agent.PressKey(SapKeyAction.Execute);

// Read results
var result = agent.ReadGrid();
foreach (var row in result.SnapshotAfter?.Grids[0].Rows ?? [])
    Console.WriteLine(string.Join(", ", row.Cells.Values));
```

## Observing the Screen

`ScanScreen()` walks the entire SAP GUI component tree and returns an immutable `SapScreenSnapshot`.

```csharp
var snapshot = agent.ScanScreen();                        // text-only (~0 ms overhead)
var snapshot = agent.ScanScreen(withScreenshot: true);    // + base64 PNG (~300 ms)
```

### `SapScreenSnapshot` properties

| Property           | Type                                 | Description                                            |
| ------------------ | ------------------------------------ | ------------------------------------------------------ |
| `Transaction`      | `string`                             | Active transaction code                                |
| `WindowTitle`      | `string`                             | Main window title                                      |
| `SystemName`       | `string`                             | SAP system identifier                                  |
| `UserName`         | `string`                             | Logged-in user name                                    |
| `Client`           | `string`                             | SAP client number                                      |
| `ScreenType`       | `SapScreenType`                      | Detected screen category (see below)                   |
| `Statusbar`        | `SapStatusbarSnapshot`               | Status bar text and message type                       |
| `Fields`           | `IReadOnlyList<SapFieldSnapshot>`    | Text fields, combos, checkboxes, radio buttons, labels |
| `Buttons`          | `IReadOnlyList<SapButtonSnapshot>`   | Push buttons and toolbar buttons                       |
| `Grids`            | `IReadOnlyList<SapGridSnapshot>`     | ALV GridView controls                                  |
| `TabStrips`        | `IReadOnlyList<SapTabStripSnapshot>` | Tab controls                                           |
| `Trees`            | `IReadOnlyList<SapTreeSnapshot>`     | Tree controls                                          |
| `Menus`            | `IReadOnlyList<SapMenuSnapshot>`     | Menu bar with one level of children                    |
| `Popups`           | `IReadOnlyList<SapPopupSnapshot>`    | Active modal windows (`wnd[1]` – `wnd[9]`)             |
| `ScreenshotBase64` | `string?`                            | Base64 PNG; only populated when `withScreenshot: true` |

### Snapshot methods

| Method                              | Description                                                                                                      |
| ----------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `ToAgentContext(includeIds: false)` | Compact structured plain-text representation of the screen for pasting into LLM prompts                          |
| `ToJson()`                          | Full camelCase JSON serialisation via `System.Text.Json`                                                         |
| `DiffFrom(previous)`                | Short diff string comparing two snapshots: transaction, title, screen type, status bar, and changed field values |

### `SapScreenType` values

`Unknown`, `EasyAccess`, `SelectionScreen`, `AlvGrid`, `AlvTree`, `ClassicTable`, `EntryForm`, `DisplayForm`, `Dialog`, `MessageDialog`, `TreeNavigation`, `HtmlViewer`, `Calendar`, `Login`

## Taking Actions

All `SapAgentSession` methods follow the **scan-before-act** pattern: capture a snapshot before the action, execute it, capture a snapshot after, compute a diff, and return a `SapActionResult`.

### `SapAgentSession` methods

| Method                                  | Description                                                                                 |
| --------------------------------------- | ------------------------------------------------------------------------------------------- |
| `ScanScreen(withScreenshot)`            | Pure observation; returns `SapActionResult` with `OkReadOnly` status                        |
| `SetField(labelOrId, value)`            | Set a text field, combo box, checkbox, or radio button by label or COM path                 |
| `GetField(labelOrId)`                   | Read the current value of a field; no after-snapshot                                        |
| `ClearField(labelOrId)`                 | Equivalent to `SetField(…, "")`                                                             |
| `ClickButton(textOrId)`                 | Click by visible text, tooltip, function code, or COM path; searches main screen and popups |
| `PressKey(SapKeyAction)`                | Send a named VKey shortcut to the main window                                               |
| `StartTransaction(tCode)`               | Enter a transaction; automatically prepends `/n` when already inside one                    |
| `SelectMenu(menuPath)`                  | Navigate menu by slash-separated path, e.g. `"Edit/Select All"`                             |
| `SelectTab(tabName)`                    | Select a tab by label; tries exact then contains match                                      |
| `ReadGrid(gridIndex, columns?)`         | Return grid rows as `OkReadOnly` result; optional column name filter                        |
| `SelectGridRow(rowIndex, gridIndex)`    | Set the selected row on an ALV grid                                                         |
| `OpenGridRow(rowIndex, gridIndex)`      | Double-click a row to drill into a document                                                 |
| `HandlePopup(action, buttonText?)`      | `Ok` / `Cancel` / `Yes` / `No` / `ByButtonText`                                             |
| `ExpandTreeNode(nodeText)`              | Expand a tree node by its display text                                                      |
| `SelectTreeNode(nodeText, doubleClick)` | Select (and optionally double-click) a tree node                                            |
| `WaitAndScan(timeoutMs)`                | Wait for SAP to finish a server round-trip, then return a snapshot                          |

### `SapActionResult` properties

| Property         | Type                 | Description                                            |
| ---------------- | -------------------- | ------------------------------------------------------ |
| `Success`        | `bool`               | `true` when the action completed without error         |
| `ErrorMessage`   | `string?`            | Error description when `Success` is `false`            |
| `SnapshotBefore` | `SapScreenSnapshot?` | Screen state immediately before the action             |
| `SnapshotAfter`  | `SapScreenSnapshot?` | Screen state immediately after the action              |
| `Diff`           | `string?`            | Compact change summary from `DiffFrom`                 |
| `ResolvedId`     | `string?`            | Actual SAP COM element path used to execute the action |

### `SapKeyAction` values

| Value          | VKey | Keyboard  |
| -------------- | ---- | --------- |
| `Enter`        | 0    | Enter     |
| `F4`           | 4    | F4        |
| `Back`         | 3    | F3        |
| `Execute`      | 8    | F8        |
| `Save`         | 11   | Ctrl+S    |
| `Cancel`       | 12   | F12       |
| `Exit`         | 15   | Shift+F3  |
| `ScrollTop`    | 71   | Ctrl+Home |
| `CtrlHome`     | 70   | —         |
| `CtrlEnd`      | 83   | —         |
| `ScrollBottom` | 82   | Ctrl+End  |

## Field and Button Resolution

### Field resolution — 4 steps (in order)

| Step | Strategy                                  | Example                         |
| ---- | ----------------------------------------- | ------------------------------- |
| 1    | Exact COM path match (starts with `wnd[`) | `wnd[0]/usr/txtS_WERKS-LOW`     |
| 2    | Exact label match (case-insensitive)      | `"Plant"`                       |
| 3    | Partial COM ID suffix match               | `"S_WERKS-LOW"`                 |
| 4    | Levenshtein distance ≤ 2                  | `"Platn"` resolves to `"Plant"` |

When no field matches, `SapAgentResolutionException` is thrown with a list of all available field labels to help the caller correct the input.

### Button resolution — 5 steps (in order)

| Step | Strategy                           | Example                 |
| ---- | ---------------------------------- | ----------------------- |
| 1    | Exact COM path                     | `wnd[0]/tbar[1]/btn[8]` |
| 2    | Exact visible text                 | `"Execute"`             |
| 3    | Exact tooltip                      | `"Execute (F8)"`        |
| 4    | Exact function code                | `"F8"`                  |
| 5    | Partial text or tooltip (contains) | `"Exec"`                |

Button resolution also searches active popup windows, so `ClickButton("Continue")` works regardless of whether the button is on the main screen or inside a `wnd[1]` popup.

## Snapshot Data Types

### `SapFieldSnapshot`

| Property       | Type                     | Description                                                                  |
| -------------- | ------------------------ | ---------------------------------------------------------------------------- |
| `Id`           | `string`                 | SAP COM element path                                                         |
| `Label`        | `string`                 | Human-readable label resolved by `LabelResolver`                             |
| `Value`        | `string`                 | Current field value                                                          |
| `FieldType`    | `SapFieldType`           | `TextField`, `ComboBox`, `CheckBox`, `RadioButton`, `Label`, `PasswordField` |
| `IsReadOnly`   | `bool`                   | Whether the field is read-only                                               |
| `IsRequired`   | `bool`                   | Whether the field is mandatory                                               |
| `MaxLength`    | `int`                    | Maximum character count                                                      |
| `ComboOptions` | `IReadOnlyList<string>?` | Available options for combo box fields                                       |

### `SapButtonSnapshot`

| Property       | Type     | Description                               |
| -------------- | -------- | ----------------------------------------- |
| `Id`           | `string` | SAP COM element path                      |
| `Text`         | `string` | Visible button label                      |
| `Tooltip`      | `string` | Button tooltip text                       |
| `IsEnabled`    | `bool`   | Whether the button is currently clickable |
| `ButtonType`   | `string` | `PushButton` or toolbar button type       |
| `FunctionCode` | `string` | SAP function code, e.g. `"F8"`            |

### `SapGridSnapshot`

| Property          | Type                                | Description                                           |
| ----------------- | ----------------------------------- | ----------------------------------------------------- |
| `Id`              | `string`                            | SAP COM element path                                  |
| `ColumnNames`     | `IReadOnlyList<string>`             | All column identifiers                                |
| `Rows`            | `IReadOnlyList<SapGridRowSnapshot>` | Visible rows (`RowIndex` + `Cells` dictionary)        |
| `TotalRowCount`   | `int`                               | Total rows including those not yet scrolled into view |
| `VisibleRowCount` | `int`                               | Number of currently visible rows                      |
| `FirstVisibleRow` | `int`                               | Zero-based index of the first visible row             |
| `HasMoreRows`     | `bool`                              | `true` when `TotalRowCount > VisibleRowCount`         |

### `SapPopupSnapshot`

| Property      | Type                               | Description                                                        |
| ------------- | ---------------------------------- | ------------------------------------------------------------------ |
| `WindowId`    | `string`                           | COM window ID, e.g. `"wnd[1]"`                                     |
| `Title`       | `string`                           | Popup dialog title                                                 |
| `Message`     | `string`                           | Popup message text                                                 |
| `MessageType` | `string`                           | `I` (info), `W` (warning), `E` (error), `S` (success), `A` (abort) |
| `Buttons`     | `IReadOnlyList<SapButtonSnapshot>` | Available buttons                                                  |
| `IsError`     | `bool`                             | Shorthand for `MessageType == "E"`                                 |
| `IsWarning`   | `bool`                             | Shorthand for `MessageType == "W"`                                 |

### `SapStatusbarSnapshot`

| Property      | Type     | Description                                        |
| ------------- | -------- | -------------------------------------------------- |
| `Text`        | `string` | Status bar message                                 |
| `MessageType` | `string` | Same type codes as popup (`I`, `W`, `E`, `S`, `A`) |
| `IsError`     | `bool`   | `true` when `MessageType == "E"`                   |
| `IsWarning`   | `bool`   | `true` when `MessageType == "W"`                   |
| `IsSuccess`   | `bool`   | `true` when `MessageType == "S"`                   |

## Threading

> [!IMPORTANT]
> SAP GUI COM objects are **Single-Threaded Apartment (STA)** and must be used on the thread
> that created them. **Do not wrap `SapAgentSession` methods in `Task.Run`.** Doing so will
> cause intermittent `InvalidCastException` or `COMException` failures.

For async hosts (web API, MCP server), marshal all SAP calls to a dedicated STA thread:

```csharp
// Good — dedicated STA thread pattern (same as SapStaThread in SapGui.Wrapper.Mcp)
var tcs = new TaskCompletionSource<string>();
_staThread.Post(() =>
{
    try   { tcs.SetResult(agent.SetField("Plant", "1000").SnapshotAfter?.ToAgentContext() ?? ""); }
    catch (Exception ex) { tcs.SetException(ex); }
});
var context = await tcs.Task;

// Avoid — marshalling to a thread pool thread loses STA affinity
var context = await Task.Run(() => agent.SetField("Plant", "1000"));
```

The `SapStaThread` class in `SapGui.Wrapper.Mcp` is a reusable reference implementation of this pattern using a `Channel<Func<Task>>` queue.
