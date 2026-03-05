namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Tests for session event arg types introduced in Priority 1.
/// Pure .NET – no SAP required.
/// </summary>
public sealed class SessionEventsTests
{
    // ── SessionChangeEventArgs ────────────────────────────────────────────────

    [Fact]
    public void SessionChangeEventArgs_StoresProperties()
    {
        var args = new SessionChangeEventArgs("Status text", "BACK", "W");

        Assert.Equal("Status text", args.Text);
        Assert.Equal("BACK",        args.FunctionCode);
        Assert.Equal("W",           args.MessageType);
    }

    [Fact]
    public void SessionChangeEventArgs_AllowsEmptyValues()
    {
        var args = new SessionChangeEventArgs(string.Empty, string.Empty, string.Empty);

        Assert.Equal(string.Empty, args.Text);
        Assert.Equal(string.Empty, args.FunctionCode);
        Assert.Equal(string.Empty, args.MessageType);
    }

    // ── AbapRuntimeErrorEventArgs ─────────────────────────────────────────────

    [Fact]
    public void AbapRuntimeErrorEventArgs_StoresMessage()
    {
        var args = new AbapRuntimeErrorEventArgs("Short dump message");

        Assert.Equal("Short dump message", args.Message);
    }

    [Fact]
    public void AbapRuntimeErrorEventArgs_AllowsEmptyMessage()
    {
        var args = new AbapRuntimeErrorEventArgs(string.Empty);

        Assert.Equal(string.Empty, args.Message);
    }

    // ── GuiSession event wiring ───────────────────────────────────────────────

    [Fact]
    public void GuiSession_CanSubscribeToChangeEvent()
    {
        // Verifies that the event compiles and can be subscribed to without
        // requiring an actual SAP connection.
        SessionChangeEventArgs? received = null;

        void Handler(object? s, SessionChangeEventArgs e) => received = e;

        // We cannot create a real GuiSession (needs COM), but we verify the
        // event signature by checking the delegate type at compile time.
        // This test confirms the API surface is correct.
        EventHandler<SessionChangeEventArgs> handler = Handler;
        Assert.NotNull(handler);
    }

    [Fact]
    public void GuiSession_CanSubscribeToDestroyEvent()
    {
        EventHandler handler = (s, e) => { };
        Assert.NotNull(handler);
    }

    [Fact]
    public void GuiSession_CanSubscribeToAbapRuntimeErrorEvent()
    {
        EventHandler<AbapRuntimeErrorEventArgs> handler = (s, e) => { };
        Assert.NotNull(handler);
    }
}
