# Session Events

`GuiSession` exposes five .NET events activated by calling `StartMonitoring()`.

Since v0.8.0, `StartMonitoring()` first attempts to connect a **true COM event sink** to the
SAP session (no polling thread). It falls back automatically to a lightweight polling thread
when the SAP GUI version does not support connection points. No extra configuration is needed.

## Event reference

| Event              | When it fires                                                                                           |
| ------------------ | ------------------------------------------------------------------------------------------------------- |
| `StartRequest`     | At the start of a server round-trip (session transitions to busy)                                       |
| `EndRequest`       | At the end of a server round-trip, before `Change`; includes `FunctionCode` when the COM sink is active |
| `Change`           | After every server round-trip (IsBusy → idle)                                                           |
| `Destroy`          | When the session becomes unreachable (window closed / connection lost)                                  |
| `AbapRuntimeError` | When a status bar message type `A` (abend) is detected after a round-trip                               |

## Usage

```csharp
var session = SapGuiClient.Attach().Session;

session.StartRequest += (_, e) =>
    Log($"Round-trip starting");

session.EndRequest += (_, e) =>
    Log($"Round-trip done — FunctionCode={e.FunctionCode}");

session.Change += (_, e) =>
    Log($"Screen changed — [{e.MessageType}] {e.Text}  FunctionCode={e.FunctionCode}");

session.AbapRuntimeError += (_, e) =>
    throw new Exception($"ABAP abend: {e.Message}");

session.Destroy += (_, _) =>
    Log("SAP session closed", LogLevel.Warn);

// StartMonitoring tries a COM event sink first (no polling thread).
// Falls back to polling (500 ms default) if the COM sink cannot connect.
session.StartMonitoring(pollMs: 500);

// ... run your automation ...

session.StopMonitoring();
```

## `EndRequest` vs `Change`

|                | `EndRequest`                          | `Change`                                   |
| -------------- | ------------------------------------- | ------------------------------------------ |
| Fires          | Immediately when the round-trip ends  | After `EndRequest`, on the same round-trip |
| `FunctionCode` | Always populated when COM sink active | Populated when COM sink active             |
| `MessageType`  | Populated                             | Populated                                  |
| Use for        | Precise wait-for-completion signal    | Status bar message inspection              |

## Stopping and cleanup

Always call `StopMonitoring()` when you are done — or let `session.Dispose()` handle it:

```csharp
// Dispose stops monitoring and releases the COM RCW
using var session = sap.Session;
session.StartMonitoring();
// ... automation ...
// Dispose is called here: StopMonitoring() + Marshal.ReleaseComObject
```

`StopMonitoring()` disconnects the COM event sink (or stops the polling thread) and is safe
to call multiple times.
