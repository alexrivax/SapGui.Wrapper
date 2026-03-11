# Logging

The wrapper is **silent by default** — zero overhead when no logger is configured.
Three overloads of `Attach` and `LaunchWithSso` accept a logger:

| Overload                                                     | When to use                                                       |
| ------------------------------------------------------------ | ----------------------------------------------------------------- |
| `Attach()` / `LaunchWithSso(system)`                         | No logging needed                                                 |
| `Attach(ILogger)` / `LaunchWithSso(system, logger:)`         | ASP.NET Core DI, Serilog/NLog via MEL adapter                     |
| `Attach(SapLogAction)` / `LaunchWithSso(system, logAction:)` | UiPath `Log()`, Serilog static `Log`, console — no MEL dependency |

## Log levels emitted

| Level           | Events                                                                                               |
| --------------- | ---------------------------------------------------------------------------------------------------- |
| **Debug**       | Every `FindById` / `FindByIdDynamic` call with component path                                        |
| **Information** | `StartTransaction`, `ExitTransaction`, session open/close, `LaunchWithSso` progress, popup dismissed |
| **Warning**     | Popup detected, retry attempt, `WaitReady`/`WaitForReadyState` near timeout                          |
| **Error**       | All thrown exceptions before they propagate                                                          |

## UiPath — route SAP logs to UiPath's `Log()` action

No dependency on `Microsoft.Extensions.Logging` is needed in your workflow:

```csharp
// All levels
using var sap = SapGuiClient.Attach(
    logAction: (level, msg, ex) => Log(
        $"[SAP/{level}] {msg}",
        level switch
        {
            SapLogLevel.Error   => LogLevel.Error,
            SapLogLevel.Warning => LogLevel.Warn,
            SapLogLevel.Debug   => LogLevel.Trace,
            _                   => LogLevel.Info,
        }));
```

```csharp
// Warning and Error only — less noise in production
using var sap = SapGuiClient.Attach(
    logAction: (level, msg, ex) => Log($"[SAP/{level}] {msg}"),
    minLevel: SapLogLevel.Warning);
```

```csharp
// With LaunchWithSso (unattended jobs)
using var sap = SapGuiClient.LaunchWithSso(
    "PRD - Production",
    reuseExistingSession: true,
    logAction: (level, msg, ex) => Log(
        $"[SAP/{level}] {msg}",
        level switch
        {
            SapLogLevel.Error   => LogLevel.Error,
            SapLogLevel.Warning => LogLevel.Warn,
            SapLogLevel.Debug   => LogLevel.Trace,
            _                   => LogLevel.Info,
        }),
    minLevel: SapLogLevel.Warning);
```

## ASP.NET Core / Serilog / NLog (via MEL adapter)

```csharp
// ILogger injected by DI or created via LoggerFactory
using var sap = SapGuiClient.Attach(logger);

// With LaunchWithSso
using var sap = SapGuiClient.LaunchWithSso("PRD - Production", logger: logger);
```

## Serilog static `Log` class (no MEL adapter)

```csharp
using var sap = SapGuiClient.Attach(logAction: (level, msg, ex) =>
    Serilog.Log.Write(
        level switch
        {
            SapLogLevel.Debug   => Serilog.Events.LogEventLevel.Debug,
            SapLogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            SapLogLevel.Error   => Serilog.Events.LogEventLevel.Error,
            _                   => Serilog.Events.LogEventLevel.Information,
        },
        ex, "{SapMessage}", msg));
```

## Console (quick debugging)

```csharp
using var sap = SapGuiClient.Attach(
    logAction: (level, msg, ex) =>
        Console.WriteLine($"[{level}] {msg}{(ex is null ? "" : " – " + ex.Message)}"));
```

## API surface

`SapLogLevel`, `SapLogAction`, and all `Attach`/`LaunchWithSso` overloads are public.
The internal `SapLogger` bridge translates between the delegate and `ILogger<T>` so only
`Microsoft.Extensions.Logging.Abstractions` (v8.0.0) is needed as a dependency —
no concrete provider is pulled in.
