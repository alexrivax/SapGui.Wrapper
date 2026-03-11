# Best Practices

## 1. Always use `using` blocks for `SapGuiClient` and `GuiSession`

Both implement `IDisposable`. Using them in `using` blocks releases COM references immediately
instead of waiting for the garbage collector:

```csharp
// Good
using var sap     = SapGuiClient.Attach();
using var session = sap.Session;

// Avoid
var sap     = SapGuiClient.Attach(); // COM ref leaks until GC
var session = sap.Session;
```

## 2. Always wait after navigation

Every SAP transaction navigation triggers a server round-trip. Interact with the next screen
only after it is fully ready:

```csharp
session.StartTransaction("/nSE16");
session.WaitForReadyState(timeoutMs: 15_000); // not just WaitReady

session.TextField("wnd[0]/usr/ctxtDATABROWSE-TABLENAME").Text = "MARA";
session.PressExecute();
session.WaitForReadyState(timeoutMs: 30_000);
```

Prefer `WaitForReadyState` over `WaitReady` after screen transitions — it handles the brief
second busy pulse that `WaitReady` can miss.

## 3. Use `ElementExists` instead of try/catch for conditional UI presence

```csharp
// Good
if (session.ElementExists("wnd[0]/usr/ctxtFIELD", timeoutMs: 5_000))
    session.TextField("wnd[0]/usr/ctxtFIELD").Text = value;

// Avoid
try { session.TextField("wnd[0]/usr/ctxtFIELD").Text = value; }
catch (SapComponentNotFoundException) { /* silently ignore */ }
```

## 4. Wrap unattended blocks in `WithRetry`

Network latency and slow ABAP reports cause intermittent failures.
Wrap navigation + field-filling blocks in `WithRetry` for unattended robots:

```csharp
session.WithRetry(maxAttempts: 3, delayMs: 500).Run(() =>
{
    session.StartTransaction("/nMM60");
    session.WaitForReadyState(timeoutMs: 15_000);
    session.TextField("wnd[0]/usr/txtS_WERKS-LOW").Text = plant;
    session.PressExecute();
    session.WaitForReadyState(timeoutMs: 30_000);
});
```

## 5. Run `EnsureHealthy()` at robot startup

Surfaces configuration problems (scripting disabled, SAP not running) as a clear error
before any automation logic runs:

```csharp
// First step in your Main workflow
SapGuiClient.EnsureHealthy();
using var sap = SapGuiClient.Attach();
```

## 6. Call `DismissPostLoginPopups` after SSO login

SAP often shows license notices, system messages, or multi-logon dialogs immediately after
connecting. Suppress these automatically before any automation:

```csharp
using var sap     = SapGuiClient.LaunchWithSso("PRD - Production", reuseExistingSession: true);
using var session = sap.Session;
session.DismissPostLoginPopups(); // handles all common post-login dialogs
session.StartTransaction("MM60");
```

## 7. Always read and log the status bar after data operations

```csharp
session.PressExecute();
session.WaitForReadyState(timeoutMs: 30_000);

var status = session.Statusbar();
Log($"Status [{status.MessageType}]: {status.Text}");
if (status.IsError)
    throw new Exception($"SAP error after execute: {status.Text}");
```

## 8. Use `ILogger` or `SapLogAction` in production

Zero-logging robots are hard to diagnose. Wire up at least Warning + Error:

```csharp
using var sap = SapGuiClient.Attach(
    logAction: (level, msg, _) => Log($"[SAP/{level}] {msg}"),
    minLevel: SapLogLevel.Warning);
```

## 9. Do not share sessions across threads

The SAP COM scripting layer is single-threaded (STA). Each robot process or thread should
obtain its own `SapGuiClient` and `GuiSession`. `session.CreateSession()` opens a parallel
window in the same SAP client — use that for multi-session workflows within a single thread.

## 10. Use `/n` prefix for `StartTransaction` when inside another transaction

```csharp
// Inside a transaction — use /n prefix to navigate away cleanly
session.StartTransaction("/nSE16");

// From Easy Access menu — bare code is fine
session.StartTransaction("SE16");
```

Omitting `/n` when already inside a transaction may leave the previous transaction screen
in an inconsistent state on some SAP versions.
