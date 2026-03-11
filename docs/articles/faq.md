# FAQ

## Does this work with SAP Business Client (SBC)?

SAP Business Client embeds SAP GUI internally, so it depends on the configuration. If SAP
Business Client is running the classic Dynpro-based screens via the SAP GUI engine and
scripting is enabled in the SBC settings, the wrapper can attach to those sessions. Pure
Fiori / browser-based screens inside SBC are not accessible via the SAP GUI scripting API.

## Does it work in unattended / background mode?

Yes, this is the primary use case for `LaunchWithSso`. The robot starts `saplogon.exe`,
connects via SSO, and automates the session entirely without a user present. SAP GUI does
need to be **visible** on the Windows desktop session — it cannot run completely hidden because
the scripting engine requires a window handle. Use a Windows interactive service account or
RDP session for unattended robots.

## How do I handle SAP password fields?

Password fields expose a write-only `Text` property (reading always returns empty, as SAP
masks the value). Set the password the same way as a text field:

```csharp
session.TextField("wnd[0]/usr/pwdRSYST-BCODE").Text = password;
```

The wrapper maps `GuiPasswordField` to `GuiTextField` automatically via `WrapComponent`,
so `session.TextField(id)` works for password fields without any special handling.

## Can I use this outside UiPath (console apps, ASP.NET, etc.)?

Yes. The package has no dependency on UiPath. It targets `net461` and `net6.0-windows` and
works in any application that runs on Windows x64 where SAP GUI is installed. The only
UiPath-specific guidance in this documentation relates to `CodedWorkflow` syntax and the
`Log()` method — both are optional.

## Why not use the SAP .NET Connector (NCo)?

SAP NCo communicates directly with the SAP application server via RFC — it bypasses the
SAP GUI entirely. It is the right tool for data extraction, BAPI calls, and server-side
integration.

SapGui.Wrapper uses the SAP **GUI Scripting API**, which automates the graphical interface
exactly as a human would. Use SapGui.Wrapper when you need to interact with Dynpro screens
that don't expose their data via RFC, when you're building RPA workflows against existing
SAP GUI transactions, or when NCo licensing or server-side setup is not available.

## How do I find the right component ID?

The SAP GUI Script Recorder is the most reliable source:

1. **Alt+F12 → Script Recording and Playback → Start Recording**
2. Perform the actions manually
3. Stop recording — the generated VBScript contains every component ID

Alternatively, right-click any SAP GUI field while scripting is enabled and choose
**"Show scripting info"** to see the component ID and type inline.

## Can I have multiple SAP sessions open at the same time?

Yes. SAP GUI supports multiple sessions (up to six by default) on a single connection.

```csharp
// Open a new parallel session
session.CreateSession();

// Retrieve it
var newSession = sap.Application.GetFirstConnection().GetSessions().Last();
```

Each `GuiSession` is independent. Operate on them from the same thread (SAP COM is STA).

## How do I read the SAP GUI scripting version or system info?

```csharp
var app  = GuiApplication.Attach();
var info = SapGuiClient.Attach().Session.Info;

Console.WriteLine($"SAP GUI version: {app.Version}");
Console.WriteLine($"System: {info.SystemName} | User: {info.User} | Client: {info.Client}");
```

## Why does `StartTransaction` need `/n` sometimes?

When you are **already inside a transaction**, the command field only accepts IDs with an
`/n` prefix to navigate away (`/nSE16`). From the Easy Access menu (`session.Info.Transaction == "SESSION_MANAGER"`),
bare codes work. Use `/n` consistently to avoid screen-state bugs:

```csharp
session.StartTransaction("/nSE16"); // always safe
```

## Does this work with SAP GUI 7.40, 7.50, 7.60, 7.70, 8.0?

Yes. The wrapper uses late-binding COM interop with no compile-time dependency on the OCX.
All tested versions from 7.40 onwards are supported. Some features degrade gracefully on
older versions:

- COM event sink (`StartMonitoring`) falls back to polling if the SAP version does not
  expose `IConnectionPointContainer` on the session object.
- `GuiMainWindow.IsMaximized` uses a Win32 P/Invoke (`IsZoomed`) rather than the SAP
  COM property, which is unreliable across versions.
