# Pre-flight Health Check

Before running automation in a robot, call `HealthCheck()` to verify the SAP environment is
ready — or `EnsureHealthy()` for a single fail-fast call.

## Usage

```csharp
// Non-throwing: inspect findings yourself
var result = SapGuiClient.HealthCheck();
if (!result.IsHealthy)
    throw new InvalidOperationException(result.FailureSummary);

foreach (var line in result.Findings)
    Log(line);   // each line is prefixed OK: / WARN: / FAIL:

// Throwing shorthand — equivalent to the above
SapGuiClient.EnsureHealthy();

// Then proceed normally
using var sap = SapGuiClient.Attach();
sap.Session.StartTransaction("SE16");
```

## Checks performed (in order)

| #   | Check                                          | FAIL condition                                          |
| --- | ---------------------------------------------- | ------------------------------------------------------- |
| 1   | `saplogon.exe` is running                      | Process not found — SAP GUI not installed / not started |
| 2   | Scripting API accessible via Windows ROT       | Scripting disabled or ROT registration failed           |
| 3   | At least one active connection                 | No SAP system logged on                                 |
| 4   | At least one active session                    | Connection exists but no window open                    |
| 5   | Session info readable (user / system / client) | Session is mid-logon or in an error state               |

`HealthCheck()` never throws — it returns a `HealthCheckResult` record regardless.
The temporary COM reference obtained during the check is always released in a `finally` block.

## `HealthCheckResult` members

| Member           | Type                    | Description                                                |
| ---------------- | ----------------------- | ---------------------------------------------------------- |
| `IsHealthy`      | `bool`                  | `true` when no `FAIL:` findings exist                      |
| `Findings`       | `IReadOnlyList<string>` | All check results, each prefixed `OK:` / `WARN:` / `FAIL:` |
| `FailureSummary` | `string`                | All `FAIL:` lines joined by newline; empty when healthy    |
| `ToString()`     | `string`                | All findings joined by newline                             |

## Recommended usage in robots

Run `EnsureHealthy()` at the very start of your robot before any SAP interaction.
This surfaces configuration problems (scripting disabled, SAP not started) as a clear
error message instead of a cryptic COM exception mid-run:

```csharp
// In UiPath: use this as the first step in your Main workflow
SapGuiClient.EnsureHealthy();

using var sap = SapGuiClient.Attach();
// ... rest of automation
```
