namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Tests for <see cref="HealthCheckResult"/>.
/// Pure .NET – no SAP required.
/// </summary>
public sealed class HealthCheckResultTests
{
    // ── Null-guard on Findings ────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullFindings_FindingsIsEmptyList()
    {
        var result = new HealthCheckResult(true, null!);
        Assert.NotNull(result.Findings);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void FailureSummary_NullFindings_DoesNotThrow()
    {
        var result = new HealthCheckResult(true, null!);
        var summary = result.FailureSummary;   // must not throw
        Assert.Equal(string.Empty, summary);
    }

    [Fact]
    public void ToString_NullFindings_DoesNotThrow()
    {
        var result = new HealthCheckResult(true, null!);
        var str = result.ToString();           // must not throw
        Assert.Equal(string.Empty, str);
    }

    // ── Normal behaviour ─────────────────────────────────────────────────────

    [Fact]
    public void FailureSummary_OnlyFailLines_ReturnsFailLines()
    {
        var findings = new[] { "OK: SAP found", "FAIL: no session", "WARN: slow" };
        var result = new HealthCheckResult(false, findings);
        Assert.Equal("FAIL: no session", result.FailureSummary);
    }

    [Fact]
    public void FailureSummary_NoFailLines_ReturnsEmpty()
    {
        var findings = new[] { "OK: SAP found", "OK: session ready" };
        var result = new HealthCheckResult(true, findings);
        Assert.Equal(string.Empty, result.FailureSummary);
    }

    [Fact]
    public void ToString_ReturnsAllFindingsJoinedByNewline()
    {
        var findings = new[] { "OK: SAP found", "FAIL: no session" };
        var result = new HealthCheckResult(false, findings);
        Assert.Equal($"OK: SAP found{Environment.NewLine}FAIL: no session", result.ToString());
    }

    [Fact]
    public void Constructor_EmptyFindings_FindingsIsEmpty()
    {
        var result = new HealthCheckResult(true, Array.Empty<string>());
        Assert.Empty(result.Findings);
    }
}
