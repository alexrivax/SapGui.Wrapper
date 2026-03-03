using SapGui.Wrapper.Tests.Helpers;

namespace SapGui.Wrapper.Tests.Integration;

/// <summary>
/// Live integration tests that run against a real SAP GUI session.
///
/// All tests in this class are decorated with <see cref="SapAvailableFactAttribute"/>
/// and are automatically skipped when SAP GUI is not running.
///
/// To run these tests:
///   1. Open SAP GUI and log on to a system.
///   2. Enable scripting: <b>Alt+F12 → Accessibility &amp; Scripting → Scripting → Enable</b>.
///   3. Run: <c>dotnet test --filter Category=Live</c>
///      or run all tests in Visual Studio / Rider (skipped tests show as "Skipped").
/// </summary>
[Trait("Category", "Live")]
public sealed class SapLiveTests : IDisposable
{
    private readonly SapGuiClient _sap;
    private readonly GuiSession   _session;

    public SapLiveTests()
    {
        _sap     = SapGuiClient.Attach();
        _session = _sap.Session;
    }

    public void Dispose() => _sap.Dispose();

    // ── Connection / session ──────────────────────────────────────────────────

    [SapAvailableFact]
    public void Attach_ReturnsNonNullApplication()
    {
        Assert.NotNull(_sap.Application);
    }

    [SapAvailableFact]
    public void GetConnections_ReturnsAtLeastOne()
    {
        var connections = _sap.GetConnections();
        Assert.NotEmpty(connections);
    }

    [SapAvailableFact]
    public void Session_IsNotNull()
    {
        Assert.NotNull(_session);
    }

    // ── Session info ──────────────────────────────────────────────────────────

    [SapAvailableFact]
    public void SessionInfo_SystemName_IsNotEmpty()
    {
        Assert.NotEmpty(_session.Info.SystemName);
    }

    [SapAvailableFact]
    public void SessionInfo_Client_IsNumericString()
    {
        var client = _session.Info.Client;
        Assert.NotEmpty(client);
        Assert.True(client.All(char.IsDigit), $"Client '{client}' should be numeric.");
    }

    [SapAvailableFact]
    public void SessionInfo_User_IsNotEmpty()
    {
        Assert.NotEmpty(_session.Info.User);
    }

    [SapAvailableFact]
    public void SessionInfo_ToString_ContainsSystemAndUser()
    {
        var str = _session.Info.ToString();
        Assert.Contains(_session.Info.SystemName, str);
        Assert.Contains(_session.Info.User,       str);
    }

    // ── Main window ───────────────────────────────────────────────────────────

    [SapAvailableFact]
    public void MainWindow_Title_IsNotEmpty()
    {
        var win = _session.MainWindow();
        Assert.NotNull(win);
        Assert.NotEmpty(win.Title);
    }

    [SapAvailableFact]
    public void MainWindow_TypeName_IsGuiMainWindow()
    {
        var win = _session.MainWindow();
        Assert.Equal("GuiMainWindow", win.TypeName);
    }

    // ── Status bar ────────────────────────────────────────────────────────────

    [SapAvailableFact]
    public void Statusbar_IsAccessible()
    {
        var sb = _session.Statusbar();
        Assert.NotNull(sb);
        // MessageType is "" when there is no message – that is valid
        Assert.NotNull(sb.MessageType);
    }

    // ── FindById ─────────────────────────────────────────────────────────────

    [SapAvailableFact]
    public void FindById_MainWindow_ReturnsGuiMainWindow()
    {
        var component = _session.FindById("wnd[0]");
        Assert.IsType<GuiMainWindow>(component);
    }

    [SapAvailableFact]
    public void FindById_NonExistent_ThrowsSapComponentNotFoundException()
    {
        Assert.Throws<SapComponentNotFoundException>(() =>
            _session.FindById("wnd[0]/usr/THIS_DOES_NOT_EXIST_XYZ"));
    }

    [SapAvailableFact]
    public void FindByIdDynamic_MainWindow_ReturnsNonNullDynamic()
    {
        dynamic wnd = _session.FindByIdDynamic("wnd[0]");
        Assert.NotNull(wnd);
    }

    [SapAvailableFact]
    public void findById_CamelCase_ReturnsDynamic()
    {
        // Recorder-compatible alias
        dynamic wnd = _session.findById("wnd[0]");
        Assert.NotNull(wnd);
    }

    // ── Transaction navigation ────────────────────────────────────────────────

    [SapAvailableFact]
    public void StartTransaction_SE16_DoesNotThrow()
    {
        // Navigate to SE16 and immediately cancel to leave the session clean
        _session.StartTransaction("SE16");
        _session.WaitReady(timeoutMs: 5_000);

        // Cancel out without saving
        _session.PressBack();
        _session.WaitReady(timeoutMs: 5_000);
    }

    // ── IsBusy / WaitReady ────────────────────────────────────────────────────

    [SapAvailableFact]
    public void IsBusy_IsInitiallyFalse()
    {
        // SAP should not be busy when no action is pending
        Assert.False(_session.IsBusy);
    }

    [SapAvailableFact]
    public void WaitReady_CompletesWithinTimeout_WhenNotBusy()
    {
        // Should return immediately since SAP is idle
        var ex = Record.Exception(() => _session.WaitReady(timeoutMs: 3_000));
        Assert.Null(ex);
    }
}
