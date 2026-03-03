namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Tests for <see cref="SapGuiNotFoundException"/> and
/// <see cref="SapComponentNotFoundException"/>.
/// Pure .NET – no SAP required.
/// </summary>
public sealed class ExceptionTests
{
    // ── SapGuiNotFoundException ───────────────────────────────────────────────

    [Fact]
    public void SapGuiNotFoundException_IsException() =>
        Assert.IsAssignableFrom<Exception>(new SapGuiNotFoundException("test"));

    [Fact]
    public void SapGuiNotFoundException_MessageIsPreserved()
    {
        const string msg = "SAP is not running";
        var ex = new SapGuiNotFoundException(msg);
        Assert.Equal(msg, ex.Message);
    }

    [Fact]
    public void SapGuiNotFoundException_InnerExceptionIsPreserved()
    {
        var inner = new InvalidOperationException("inner");
        var ex    = new SapGuiNotFoundException("outer", inner);

        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void SapGuiNotFoundException_CanBeCaughtAsException()
    {
        bool caught = false;
        try   { throw new SapGuiNotFoundException("x"); }
        catch (Exception) { caught = true; }
        Assert.True(caught);
    }

    // ── SapComponentNotFoundException ─────────────────────────────────────────

    [Fact]
    public void SapComponentNotFoundException_IsException() =>
        Assert.IsAssignableFrom<Exception>(new SapComponentNotFoundException("id"));

    [Fact]
    public void SapComponentNotFoundException_MessageContainsComponentId()
    {
        const string id = "wnd[0]/usr/txtRSYST-BNAME";
        var ex = new SapComponentNotFoundException(id);
        Assert.Contains(id, ex.Message);
    }

    [Fact]
    public void SapComponentNotFoundException_InnerExceptionIsPreserved()
    {
        var inner = new COMException("COM error");
        var ex    = new SapComponentNotFoundException("wnd[0]/test", inner);

        Assert.Contains("wnd[0]/test", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void SapComponentNotFoundException_CanBeCaughtAsException()
    {
        bool caught = false;
        try   { throw new SapComponentNotFoundException("wnd[0]/test"); }
        catch (Exception) { caught = true; }
        Assert.True(caught);
    }

    // Helper: fake COMException (no COM reference needed)
    private sealed class COMException : Exception
    {
        public COMException(string message) : base(message) { }
    }
}
