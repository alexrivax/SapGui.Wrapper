# Next Steps TODO before big release 1.0.0

**Scope:** C# only · Keep it simple · Maximize reliability

---

## 1. Security First ✅

Wrapper is now hardened for enterprise COM lifecycle:

- `SapRot.GetGuiApplication()` releases all intermediate COM objects (`rotWrapper`, `sapGuiRaw`, `IBindCtx`, `IRunningObjectTable`) via `try/finally` + `Marshal.ReleaseComObject` — no dangling COM references after attach.
- `SapGuiClient.Dispose()` now explicitly calls `Marshal.ReleaseComObject(Application.RawObject)` instead of relying on the GC finalizer.
- `GuiSession` now implements `IDisposable`: calls `StopMonitoring()` (disconnects COM event sinks and stops the polling thread) then releases its COM RCW immediately. Safe to use in `using` blocks.
- All XML doc comments explain the security model and lifetime guarantees.

---

## 2. SSO Initialization & Session Management ✅

- [x] **Create an SSO Launch Method:** `SapGuiClient.LaunchWithSso(systemDescription, connectionTimeoutMs)` — starts `saplogon.exe` if not running, waits for it to register in the Windows ROT, opens the connection (SSO means no credential dialog), and polls until a non-busy session is available.
- [x] **Handle Login Pop-ups:** `GuiSession.DismissPostLoginPopups(maxPopups, timeoutMs)` — automatically dismisses "User already logged on / Multiple Logon", license expiration warnings, system message banners, and any single-button info dialog. Unrecognised multi-button dialogs are left untouched to avoid silent data loss.

## 3. Retry Policy (Built-in, No External Dependencies)

- [ ] Create `RetryPolicy` class with configurable `MaxAttempts` and `DelayMs`
- [ ] Wrap `WaitReady()` with retry on `TimeoutException`
- [ ] Add `session.WithRetry(maxAttempts, delayMs)` fluent entry point that returns a retry-scoped session proxy
- [ ] Retry on `SapComponentNotFoundException` with configurable max attempts (handles slow screen loads)
- [ ] Do NOT retry on `SapGuiNotFoundException` — that is a fatal setup error
- [ ] Add XML doc comments explaining when to use retry vs `WaitReady()`
- [ ] **Implement `WaitForReadyState()`:** Create a method that reliably checks if the SAP GUI is actually ready for input, bypassing the sometimes-unreliable native COM sync.
- [ ] **Add `ElementExists(id, timeout)`:** Build an explicit wait function that polls for a specific SAP ID to appear before interacting with it, preventing "Object not found" COM crashes on slow networks.
- [ ] **Add `WaitUntilHidden(id, timeout)`:** Useful for waiting out loading spinners or processing dialogue boxes.

**Why:** SAP GUI is timing-sensitive. Retries must be in the library, not copy-pasted into every workflow.

---

## 4. Health Check / Pre-flight

- [ ] Add `SapGuiClient.HealthCheck()` returning a `HealthCheckResult` (IsHealthy, list of findings)
- [ ] Check: SAP GUI process is running
- [ ] Check: Scripting is enabled (attempt ROT access, surface clear error if not)
- [ ] Check: At least one active session exists
- [ ] Check: Current user and system name are readable (confirms session is logged in)
- [ ] Return structured result — do not throw — so callers can decide how to handle
- [ ] Add `SapGuiClient.EnsureHealthy()` throwing variant for workflows that prefer fail-fast

**Why:** Robots failing silently mid-run because scripting was disabled after a patch is a common and avoidable pain.

---

## 5. NuGet Package Hardening

- [ ] Enable deterministic builds (`<Deterministic>true</Deterministic>`)
- [ ] Add Source Link (`Microsoft.SourceLink.GitHub`) so stack traces resolve to exact source lines
- [ ] Sign the package with a code-signing certificate (self-signed acceptable for now; document the thumbprint)
- [ ] Generate a basic SBOM on pack (`dotnet build` + `CycloneDX` MSBuild task)
- [ ] Add `<PackageReadmeFile>README.md</PackageReadmeFile>` to surface docs on NuGet.org
- [ ] Pin all transitive dependencies to minimum secure versions in the `.csproj`

**Why:** Most enterprise artifact repositories (Artifactory, Azure Artifacts with policy) reject unsigned or non-deterministic packages.
