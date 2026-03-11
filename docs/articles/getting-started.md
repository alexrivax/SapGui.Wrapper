# Getting Started

## Prerequisites

| Requirement       | Details                                                          |
| ----------------- | ---------------------------------------------------------------- |
| SAP GUI           | 7.40 or later, installed on the robot / developer machine        |
| Scripting enabled | SAP GUI Options → Accessibility & Scripting → Enable scripting   |
| .NET              | `net461` (legacy UiPath) or `net6.0-windows` (UiPath Studio 23+) |
| Platform          | x64 Windows only (SAP GUI constraint)                            |

> [!IMPORTANT]
> SAP GUI scripting must be enabled **both server-side and client-side**.
> Server-side is controlled by your SAP Basis team (profile parameter `sapgui/user_scripting = TRUE`).
> Client-side is under SAP GUI Options → Accessibility & Scripting.

## Installation

**In UiPath Studio:**
Manage Packages → NuGet.org → search `SapGui.Wrapper` → Install

**CLI:**

```bash
dotnet add package SapGui.Wrapper
```

## Attaching to a running SAP session

The simplest starting point — SAP GUI is already open and logged in:

```csharp
using SapGui.Wrapper;

using var sap     = SapGuiClient.Attach();
var       session = sap.Session;   // the currently focused session

session.StartTransaction("MM60");
session.TextField("wnd[0]/usr/txtS_WERKS-LOW").Text = "1000";
session.PressExecute();
session.WaitForReadyState(timeoutMs: 30_000);
```

`SapGuiClient` and `GuiSession` both implement `IDisposable`. Use `using` for deterministic COM release.

## UiPath Coded Workflow example

```csharp
using SapGui.Wrapper;
using UiPath.CodedWorkflows;

public class ProcessMaterialList : CodedWorkflow
{
    [Workflow]
    public DataTable Execute(string plant)
    {
        using var sap     = SapGuiClient.Attach();
        var       session = sap.Session;

        Log($"System: {session.Info.SystemName} | User: {session.Info.User}");

        session.MainWindow().Maximize();
        session.StartTransaction("MM60");

        session.TextField("wnd[0]/usr/txtS_WERKS-LOW").Text = plant;
        session.PressExecute();
        session.WaitForReadyState(timeoutMs: 30_000);

        var popup = session.GetActivePopup();
        if (popup != null)
        {
            Log($"Popup: {popup.Text}", LogLevel.Warn);
            popup.ClickOk();
            return new DataTable();
        }

        var grid = session.GridView("wnd[0]/usr/cntlGRID/shellcont/shell");
        var rows = grid.GetRows(new[] { "MATNR", "MAKTX", "MEINS", "LABST" });

        var dt = new DataTable();
        dt.Columns.Add("Material");  dt.Columns.Add("Description");
        dt.Columns.Add("Unit");      dt.Columns.Add("UnrestrictedStock");
        foreach (var row in rows)
            dt.Rows.Add(row["MATNR"], row["MAKTX"], row["MEINS"], row["LABST"]);

        session.ExitTransaction();
        return dt;
    }
}
```

## SSO / Unattended login

Use `LaunchWithSso` when the robot must start SAP itself (e.g. unattended UiPath jobs):

```csharp
using var sap     = SapGuiClient.LaunchWithSso("PRD - Production");
using var session = sap.Session;

// Clear any post-login popups (system messages, license notices, etc.)
int dismissed = session.DismissPostLoginPopups();

session.StartTransaction("MM60");
```

`LaunchWithSso` starts `saplogon.exe` if not running, connects via SSO (no credential dialog),
and blocks until the session is ready — throwing `TimeoutException` rather than silently returning a busy session.

### Reusing an existing session

If the user is already logged in to the same system, SAP Logon shows a license dialog that blocks outside the scripting API. Use `reuseExistingSession` to skip opening a new connection:

```csharp
// true  → reuse the already-open session (safe for concurrent unattended robots)
using var sap = SapGuiClient.LaunchWithSso("PRD - Production", reuseExistingSession: true);

// false (default) → throw if a session already exists, so the caller can decide
using var sap = SapGuiClient.LaunchWithSso("PRD - Production"); // throws if already logged on
```

## Static one-liners (`SapGuiHelper`)

For the simplest scripts where no variable management is needed:

```vbnet
' VB.NET in UiPath Invoke Code
SapGuiHelper.StartTransaction("MM01")
SapGuiHelper.SetText("wnd[0]/usr/txtMM01-MATNR", "MAT-0042")
SapGuiHelper.PressEnter()

Dim msg As String = SapGuiHelper.GetStatusMessage()
If SapGuiHelper.HasError() Then Throw New Exception("SAP error: " & msg)
```

## Getting component IDs from the SAP Script Recorder

Every component ID (`"wnd[0]/usr/txtRSYST-BNAME"`) comes directly from the **SAP GUI Script Recorder**:

1. In SAP GUI: **Alt+F12 → Script Recording and Playback → Start Recording**
2. Perform the actions manually
3. Stop recording — the VBScript output contains every ID you need
4. Paste those IDs into this wrapper

Recorder output:

```vbscript
session.findById("wnd[0]/usr/txtRSYST-BNAME").text = "MYUSER"
session.findById("wnd[0]/tbar[1]/btn[8]").press
```

C# — paste the IDs, only one change needed:

```csharp
session.findById("wnd[0]/usr/txtRSYST-BNAME").Text = "MYUSER";
session.findById("wnd[0]/tbar[1]/btn[8]").press();
```

`findById` (lowercase) returns `dynamic`, so all SAP properties and methods resolve at runtime.
For compile-time IntelliSense, use the typed overload:

```csharp
session.FindById<GuiTextField>("wnd[0]/usr/txtRSYST-BNAME").Text = "MYUSER";
```

## VKey reference

| VKey | Key       | Action                                        |
| ---: | --------- | --------------------------------------------- |
|    0 | Enter     | Confirm / navigate forward                    |
|    3 | F3        | Back — one screen back within the transaction |
|    4 | F4        | Input Help / Possible Values                  |
|    8 | F8        | Execute — runs the current selection screen   |
|   11 | Ctrl+S    | Save                                          |
|   12 | F12       | Cancel — discards changes                     |
|   15 | Shift+F3  | Exit — back to previous menu level            |
|   71 | Ctrl+Home | Scroll to top                                 |
|   82 | Ctrl+End  | Scroll to bottom                              |

Shortcuts: `PressEnter()` · `PressBack()` · `PressExecute()` · `ExitTransaction()`
