# Resilient Automation

SAP GUI is timing-sensitive. Network latency, slow ABAP reports, and post-navigation busy
pulses all cause flaky automation when you interact with the screen too early.
The wrapper provides five methods to handle this:

| Method                                           | Use when                                                                           |
| ------------------------------------------------ | ---------------------------------------------------------------------------------- |
| `WaitReady(timeoutMs)`                           | Simple busy-flag poll after navigation                                             |
| `WaitForReadyState(timeoutMs, pollMs, settleMs)` | Stricter: also waits for a settle period and verifies the main window is reachable |
| `ElementExists(id, timeoutMs)`                   | Poll until a specific component appears before touching it                         |
| `WaitUntilHidden(id, timeoutMs)`                 | Poll until a loading spinner or progress dialog disappears                         |
| `WithRetry(maxAttempts, delayMs).Run(…)`         | Re-execute a block on `SapComponentNotFoundException` or `TimeoutException`        |

## `WaitReady` — simple busy-flag poll

```csharp
session.PressExecute();
session.WaitReady(timeoutMs: 30_000); // default: 30 s, polls every 200 ms
```

Use this after short navigation steps. For screen transitions that involve a brief
second busy pulse, prefer `WaitForReadyState`.

## `WaitForReadyState` — stricter wait

After screen transitions:

```csharp
session.StartTransaction("/nMM60");
// Waits until IsBusy is false, adds a 300 ms settle, then confirms the main window is reachable.
session.WaitForReadyState(timeoutMs: 15_000, settleMs: 300);
```

Default values: `timeoutMs = 30_000`, `pollMs = 200`, `settleMs = 0`.

## `ElementExists` — wait for a component to appear

Don't call typed finders until the field is actually on screen:

```csharp
const string reportField = "wnd[0]/usr/ctxtSELWERKS-LOW";

if (!session.ElementExists(reportField, timeoutMs: 8_000))
    throw new Exception("Selection screen did not load in time.");

session.TextField(reportField).Text = plant;
```

## `WaitUntilHidden` — wait for a spinner or dialog to close

```csharp
const string spinner = "wnd[1]/usr/txtMESSAGE";
session.PressExecute();
session.WaitUntilHidden(spinner, timeoutMs: 60_000);
session.WaitForReadyState(timeoutMs: 10_000);
```

## `WithRetry` — resilient execution block

`WithRetry` retries on `SapComponentNotFoundException` (slow screen loads) and `TimeoutException`
(session still busy). It **never** retries on `SapGuiNotFoundException` — that is a fatal setup error
and should propagate immediately.

```csharp
session.WithRetry(maxAttempts: 3, delayMs: 500).Run(() =>
{
    session.StartTransaction("/nMM60");
    session.WaitForReadyState(timeoutMs: 15_000);
    session.TextField("wnd[0]/usr/ctxtSELWERKS-LOW").Text = plant;
    session.PressExecute();
    session.WaitForReadyState(timeoutMs: 30_000);
});
```

Return a value from the protected block with `Run<T>`:

```csharp
string status = session.WithRetry(maxAttempts: 3, delayMs: 400).Run(() =>
{
    session.WaitReady(timeoutMs: 10_000);
    return session.Statusbar().Text;
});
```

## Combined pattern — navigate, wait, read

```csharp
using var sap     = SapGuiClient.Attach();
var       session = sap.Session;

const string tableField = "wnd[0]/usr/ctxtDATABROWSE-TABLENAME";

// 1. Navigate with retry in case the first attempt races
session.WithRetry(maxAttempts: 3, delayMs: 400).Run(() =>
{
    session.StartTransaction("/nSE16");
    session.WaitForReadyState(timeoutMs: 10_000);
    if (!session.ElementExists(tableField, timeoutMs: 5_000))
        throw new SapComponentNotFoundException(tableField);
});

// 2. Interact only after the page is confirmed ready
session.TextField(tableField).Text = "MARA";
session.PressExecute();
session.WaitForReadyState(timeoutMs: 30_000);

// 3. Wait for a progress dialog to disappear
bool hadSpinner = session.WaitUntilHidden("wnd[1]", timeoutMs: 60_000);
if (hadSpinner) Log("Progress dialog closed.");

var grid = session.GridView("wnd[0]/usr/cntlGRID/shellcont/shell");
Log($"Rows returned: {grid.RowCount}");
```

## Exception types reference

| Exception                       | When thrown                                                           |
| ------------------------------- | --------------------------------------------------------------------- |
| `SapGuiNotFoundException`       | SAP GUI not running or scripting disabled — fatal, never retried      |
| `SapComponentNotFoundException` | `FindById` path not found — retried by `WithRetry`                    |
| `TimeoutException`              | `WaitReady` timed out while session was busy — retried by `WithRetry` |
| `InvalidCastException`          | `FindById<T>` found a component of the wrong type                     |
| `InvalidOperationException`     | `LaunchWithSso` session conflict or connection error                  |
