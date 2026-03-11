# Troubleshooting

## `SapGuiNotFoundException` — SAP GUI is not running or scripting is disabled

**Symptom:** `SapGuiNotFoundException: SAP GUI is not running or scripting is not enabled.`

**Causes and fixes:**

| Cause                                 | Fix                                                                                   |
| ------------------------------------- | ------------------------------------------------------------------------------------- |
| `saplogon.exe` is not running         | Start SAP GUI Logon Pad manually, or use `LaunchWithSso`                              |
| Client-side scripting is disabled     | SAP GUI → Options → Accessibility & Scripting → Enable scripting ✓                    |
| Server-side scripting is disabled     | Ask your SAP Basis team to set `sapgui/user_scripting = TRUE`                         |
| Running as a service / SYSTEM account | SAP GUI scripting registers in the user's Windows ROT — run under a real user profile |

**Quick check:**

```csharp
var result = SapGuiClient.HealthCheck();
foreach (var line in result.Findings)
    Console.WriteLine(line);
```

---

## `SapComponentNotFoundException` — wrong or stale component ID

**Symptom:** `SapComponentNotFoundException: Component not found: wnd[0]/usr/txtXXX`

**Causes and fixes:**

| Cause                                         | Fix                                                                                |
| --------------------------------------------- | ---------------------------------------------------------------------------------- |
| The screen hasn't loaded yet                  | Add `WaitForReadyState` before the `FindById` call                                 |
| Typo in the ID                                | Copy the ID directly from the SAP Script Recorder output                           |
| The component is on a different window index  | Check if a popup or new window is active — IDs start with `wnd[0]`, `wnd[1]`, etc. |
| The field only appears on certain screens     | Use `ElementExists(id, timeoutMs)` to check before accessing                       |
| Different SAP system / version / user profile | IDs can vary; re-record on the target system                                       |

---

## `TimeoutException` from `WaitReady` / `WaitForReadyState`

**Symptom:** `TimeoutException: Session did not become ready within 30000 ms.`

**Causes and fixes:**

| Cause                                         | Fix                                                                    |
| --------------------------------------------- | ---------------------------------------------------------------------- |
| ABAP report is genuinely slow                 | Increase `timeoutMs` — some batch jobs run for minutes                 |
| A modal dialog appeared (e.g. input required) | Check for `GetActivePopup()` before waiting                            |
| Session lost connection to the server         | Check SAP network connectivity; `HealthCheck()` will show this         |
| Wrong session index                           | Verify you're operating on the correct session with `sap.GetSession()` |

---

## `InvalidCastException` from `FindById<T>`

**Symptom:** `InvalidCastException: Cannot cast 'GuiComboBox' to 'GuiTextField'`

**Fix:** The component ID is correct but the SAP control type differs from what you expected.
Use the untyped `findById(id)` (dynamic) first to inspect the SAP `Type` property:

```csharp
dynamic raw = session.findById("wnd[0]/usr/cmbVBAK-VKORG");
Console.WriteLine(raw.Type); // e.g. "GuiComboBox"
```

Then use the correct typed finder: `session.ComboBox(id)`.

---

## `InvalidOperationException` from `LaunchWithSso`

**Symptom:** `InvalidOperationException: A session for system 'PRD' is already open. Pass reuseExistingSession: true to reuse it.`

**Fix:**

```csharp
// Reuse the already-open session
using var sap = SapGuiClient.LaunchWithSso("PRD - Production", reuseExistingSession: true);
```

Or close the existing session before calling `LaunchWithSso` without the flag.

---

## COM objects leaking / SAP crashes after run

**Symptom:** SAP GUI becomes unresponsive or crashes after the robot finishes.

**Fix:** Ensure `SapGuiClient` and `GuiSession` are wrapped in `using` blocks so their
`Dispose()` methods are called and COM RCWs are released deterministically:

```csharp
using var sap     = SapGuiClient.Attach();
using var session = sap.Session;
// ... automation ...
// Dispose called here — COM references released immediately
```

---

## `GuiTable` returns empty cell values

**Symptom:** `table.GetCellValue(row, col)` returns empty string for all rows.

**Cause:** SAP only populates COM cells for the currently visible viewport.
Off-screen rows always return empty values.

**Fix:** Use the scroll-aware loop pattern:

```csharp
int total    = table.RowCount;
int pageSize = table.VisibleRowCount;
for (int start = 0; start < total; start += pageSize)
{
    table.ScrollToRow(start);
    for (int r = start; r < Math.Min(start + pageSize, total); r++)
        Console.WriteLine(table.GetCellValue(r, 0));
}
```

---

## Build error on `net461`: `string.Contains(string, StringComparison)` not available

**Symptom:** Compile error on net461 target when using `str.Contains("x", StringComparison.OrdinalIgnoreCase)`.

**Fix:** Use `.IndexOf("x", StringComparison.OrdinalIgnoreCase) >= 0` instead.
This is listed in the contributing guide and applies to custom code that extends the wrapper.

---

## `DismissPostLoginPopups` doesn't dismiss a dialog

**Symptom:** A post-login dialog appears that `DismissPostLoginPopups` leaves untouched.

**Explanation:** Unrecognised **multi-button dialogs** are intentionally left untouched to avoid
silent data loss. Only well-known dialogs with a single safe action are dismissed automatically.

**Fix:** Inspect the dialog manually:

```csharp
var popup = session.GetActivePopup();
if (popup != null)
{
    Log($"Title: {popup.Title}  Text: {popup.Text}");
    foreach (var btn in popup.GetButtons())
        Log($"Button: {btn.Text}");
    popup.ClickButton("Continue"); // or whichever is safe for your scenario
}
```
