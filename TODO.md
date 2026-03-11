# Release Notes — v1.0.0

**Status:** Released · **Scope:** C# only · Keep it simple · Maximize reliability

All items below have been completed and shipped in v1.0.0.

---

## 1. Security First

Wrapper is now hardened for enterprise COM lifecycle:

- `SapRot.GetGuiApplication()` releases all intermediate COM objects (`rotWrapper`, `sapGuiRaw`, `IBindCtx`, `IRunningObjectTable`) via `try/finally` + `Marshal.ReleaseComObject` — no dangling COM references after attach.
- `SapGuiClient.Dispose()` now explicitly calls `Marshal.ReleaseComObject(Application.RawObject)` instead of relying on the GC finalizer.
- `GuiSession` now implements `IDisposable`: calls `StopMonitoring()` (disconnects COM event sinks and stops the polling thread) then releases its COM RCW immediately. Safe to use in `using` blocks.
- All XML doc comments explain the security model and lifetime guarantees.

---

## 2. SSO Initialization & Session Management

- [x] **Create an SSO Launch Method:** `SapGuiClient.LaunchWithSso(systemDescription, connectionTimeoutMs)` — starts `saplogon.exe` if not running, waits for it to register in the Windows ROT, opens the connection (SSO means no credential dialog), and polls until a non-busy session is available.
- [x] **Handle Login Pop-ups:** `GuiSession.DismissPostLoginPopups(maxPopups, timeoutMs)` — automatically dismisses "User already logged on / Multiple Logon", license expiration warnings, system message banners, and any single-button info dialog. Unrecognised multi-button dialogs are left untouched to avoid silent data loss.

## 3. Retry Policy (Built-in, No External Dependencies)

- [x] Create `RetryPolicy` class with configurable `MaxAttempts` and `DelayMs`
- [x] Wrap `WaitReady()` with retry on `TimeoutException`
- [x] Add `session.WithRetry(maxAttempts, delayMs)` fluent entry point that returns a retry-scoped session proxy
- [x] Retry on `SapComponentNotFoundException` with configurable max attempts (handles slow screen loads)
- [x] Do NOT retry on `SapGuiNotFoundException` — that is a fatal setup error
- [x] Add XML doc comments explaining when to use retry vs `WaitReady()`
- [x] **Implement `WaitForReadyState()`:** Create a method that reliably checks if the SAP GUI is actually ready for input, bypassing the sometimes-unreliable native COM sync.
- [x] **Add `ElementExists(id, timeout)`:** Build an explicit wait function that polls for a specific SAP ID to appear before interacting with it, preventing "Object not found" COM crashes on slow networks.
- [x] **Add `WaitUntilHidden(id, timeout)`:** Useful for waiting out loading spinners or processing dialogue boxes.

**Why:** SAP GUI is timing-sensitive. Retries must be in the library, not copy-pasted into every workflow.

---

## 4. Health Check / Pre-flight

- [x] Add `SapGuiClient.HealthCheck()` returning a `HealthCheckResult` (IsHealthy, list of findings)
- [x] Check: SAP GUI process is running
- [x] Check: Scripting is enabled (attempt ROT access, surface clear error if not)
- [x] Check: At least one active session exists
- [x] Check: Current user and system name are readable (confirms session is logged in)
- [x] Return structured result — do not throw — so callers can decide how to handle
- [x] Add `SapGuiClient.EnsureHealthy()` throwing variant for workflows that prefer fail-fast

**Why:** Robots failing silently mid-run because scripting was disabled after a patch is a common and avoidable pain.

---

## 5. NuGet Package Hardening

- [x] Enable deterministic builds (`<Deterministic>true</Deterministic>`)
- [x] Add Source Link (`Microsoft.SourceLink.GitHub`) so stack traces resolve to exact source lines
- [x] Sign the package with a code-signing certificate — `scripts/New-SigningCert.ps1` creates a self-signed cert, exports it as PFX, and signs all `.nupkg` files via `dotnet nuget sign`; add `*.pfx` / `*.p12` to `.gitignore`
- [x] Generate a basic SBOM on pack — `dotnet CycloneDX` wired via `GenerateSbom` AfterPack MSBuild target; tool pinned in `.config/dotnet-tools.json` (`cyclonedx 3.0.8`)
- [x] Add `<PackageReadmeFile>README.md</PackageReadmeFile>` to surface docs on NuGet.org
- [x] Pin all transitive dependencies to minimum secure versions in the `.csproj` (`Microsoft.Build.Tasks.Git` and `Microsoft.SourceLink.Common` explicitly pinned to `8.0.0`)

## 6. ILOGGER INTEGRATION (MICROSOFT.EXTENSIONS.LOGGING)

---

- [x] Accept ILogger in `SapGuiClient.Attach(ILogger logger)` — null = silent via no-arg overload
- [x] Accept `SapLogAction` delegate in `SapGuiClient.Attach(SapLogAction logAction)` for zero-dependency integrations
- [x] Log at Debug: every `FindById` and `FindByIdDynamic` call with the component path
- [x] Log at Information: `StartTransaction`, `ExitTransaction`, session open/close, `LaunchWithSso` progress
- [x] Log at Warning: popup detected, retry attempt, `WaitReady`/`WaitForReadyState` near timeout
- [x] Log at Error: all thrown exceptions (`Attach`, `LaunchWithSso`, `StartTransaction`, `ExitTransaction`, `WaitReady`, `WaitForReadyState`) before they propagate
- [x] Take only `Microsoft.Extensions.Logging.Abstractions` (v8.0.0) as a dependency — no concrete provider
- [x] `SapLogLevel` enum and `SapLogAction` delegate are public; `SapLogger` bridge is internal
- [x] Both `LaunchWithSso` and `Attach` have ILogger and SapLogAction overloads
- [x] `session.WithRetry(...)` propagates the session logger to `RetryPolicy` for retry-attempt warnings

## 7. Create GitHub pages ✅

- [x] Document all events, methods and functions available to users (API reference auto-generated from XML docs via DocFX)
- [x] Follow same structure and approach as Microsoft Documentation does (DocFX modern template)
- [x] Create examples for each of the methods, events or functions (getting-started, patterns, retry, logging, events articles)
- [x] Add a troubleshooting section with common errors and their solutions (`docs/articles/troubleshooting.md`)
- [x] Add a FAQ section with common questions and answers about the library (`docs/articles/faq.md`)
- [x] Ensure the documentation is clear, concise and easy to understand for users of all levels
- [x] Add a section on best practices for using the library effectively and securely (`docs/articles/best-practices.md`)
- [x] Clean up the README.md to be a high-level overview and link to the full documentation for details
- [x] DocFX config, article structure and GitHub Actions deploy workflow scaffolded (`docs/`, `.github/workflows/docs.yml`)
