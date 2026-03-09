namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Tests for <see cref="RetryPolicy"/> and <see cref="GuiSession.WithRetry"/>.
/// Pure .NET – no SAP required.
/// </summary>
public sealed class RetryPolicyTests
{
    // ── Constructor validation ────────────────────────────────────────────────

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        var policy = new RetryPolicy();
        Assert.Equal(3, policy.MaxAttempts);
        Assert.Equal(500, policy.DelayMs);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Constructor_ValidMaxAttempts_Accepted(int attempts)
    {
        var policy = new RetryPolicy(maxAttempts: attempts, delayMs: 0);
        Assert.Equal(attempts, policy.MaxAttempts);
    }

    [Fact]
    public void Constructor_MaxAttemptsZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy(maxAttempts: 0));
    }

    [Fact]
    public void Constructor_MaxAttemptsNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy(maxAttempts: -1));
    }

    [Fact]
    public void Constructor_NegativeDelayMs_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy(maxAttempts: 1, delayMs: -1));
    }

    [Fact]
    public void Constructor_ZeroDelayMs_Accepted()
    {
        var policy = new RetryPolicy(maxAttempts: 1, delayMs: 0);
        Assert.Equal(0, policy.DelayMs);
    }

    // ── Run(Action) – success path ────────────────────────────────────────────

    [Fact]
    public void Run_ActionSucceedsFirstAttempt_InvokedOnce()
    {
        int callCount = 0;
        var policy = new RetryPolicy(maxAttempts: 3, delayMs: 0);

        policy.Run(() => callCount++);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Run_ActionSucceedsOnSecondAttempt_InvokedTwice()
    {
        int callCount = 0;
        var policy = new RetryPolicy(maxAttempts: 3, delayMs: 0);

        policy.Run(() =>
        {
            callCount++;
            if (callCount < 2)
                throw new SapComponentNotFoundException("wnd[0]/usr/txtFoo");
        });

        Assert.Equal(2, callCount);
    }

    [Fact]
    public void Run_TimeoutExceptionRetried_SucceedsOnThirdAttempt()
    {
        int callCount = 0;
        var policy = new RetryPolicy(maxAttempts: 3, delayMs: 0);

        policy.Run(() =>
        {
            callCount++;
            if (callCount < 3)
                throw new TimeoutException("SAP busy");
        });

        Assert.Equal(3, callCount);
    }

    // ── Run(Action) – exhausted retries ──────────────────────────────────────

    [Fact]
    public void Run_AlwaysFails_ThrowsAfterMaxAttempts()
    {
        int callCount = 0;
        var policy = new RetryPolicy(maxAttempts: 3, delayMs: 0);

        var ex = Assert.Throws<SapComponentNotFoundException>(() =>
            policy.Run(() =>
            {
                callCount++;
                throw new SapComponentNotFoundException("wnd[0]/usr/txtFoo");
            }));

        Assert.Equal(3, callCount);
        Assert.Contains("wnd[0]/usr/txtFoo", ex.Message);
    }

    [Fact]
    public void Run_TimeoutAlwaysFails_ThrowsAfterMaxAttempts()
    {
        int callCount = 0;
        var policy = new RetryPolicy(maxAttempts: 2, delayMs: 0);

        Assert.Throws<TimeoutException>(() =>
            policy.Run(() =>
            {
                callCount++;
                throw new TimeoutException("SAP busy");
            }));

        Assert.Equal(2, callCount);
    }

    // ── Run(Action) – fatal exception is never retried ────────────────────────

    [Fact]
    public void Run_SapGuiNotFoundException_RethrowsImmediately()
    {
        int callCount = 0;
        var policy = new RetryPolicy(maxAttempts: 3, delayMs: 0);

        Assert.Throws<SapGuiNotFoundException>(() =>
            policy.Run(() =>
            {
                callCount++;
                throw new SapGuiNotFoundException("SAP is not running");
            }));

        // Must not have been retried — fatal on first attempt
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Run_UnrecognisedException_RethrowsImmediately()
    {
        int callCount = 0;
        var policy = new RetryPolicy(maxAttempts: 3, delayMs: 0);

        Assert.Throws<InvalidOperationException>(() =>
            policy.Run(() =>
            {
                callCount++;
                throw new InvalidOperationException("unexpected");
            }));

        Assert.Equal(1, callCount);
    }

    // ── Run<T>(Func<T>) – value return ────────────────────────────────────────

    [Fact]
    public void RunT_SucceedsFirstAttempt_ReturnsValue()
    {
        var policy = new RetryPolicy(maxAttempts: 3, delayMs: 0);
        string result = policy.Run(() => "hello");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void RunT_SucceedsOnSecondAttempt_ReturnsValue()
    {
        int callCount = 0;
        var policy = new RetryPolicy(maxAttempts: 3, delayMs: 0);

        string result = policy.Run(() =>
        {
            callCount++;
            if (callCount < 2)
                throw new SapComponentNotFoundException("wnd[0]/usr/txtBar");
            return "value";
        });

        Assert.Equal("value", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RunT_AlwaysFails_ThrowsAfterMaxAttempts()
    {
        int callCount = 0;
        var policy = new RetryPolicy(maxAttempts: 2, delayMs: 0);

        Assert.Throws<SapComponentNotFoundException>(() =>
            policy.Run<string>(() =>
            {
                callCount++;
                throw new SapComponentNotFoundException("wnd[0]/usr/txtBar");
            }));

        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RunT_SapGuiNotFoundException_RethrowsImmediately()
    {
        int callCount = 0;
        var policy = new RetryPolicy(maxAttempts: 3, delayMs: 0);

        Assert.Throws<SapGuiNotFoundException>(() =>
            policy.Run<string>(() =>
            {
                callCount++;
                throw new SapGuiNotFoundException("SAP is not running");
            }));

        Assert.Equal(1, callCount);
    }

    // ── MaxAttempts = 1 (no retries) ─────────────────────────────────────────

    [Fact]
    public void Run_MaxAttemptsOne_DoesNotRetry()
    {
        int callCount = 0;
        var policy = new RetryPolicy(maxAttempts: 1, delayMs: 0);

        Assert.Throws<SapComponentNotFoundException>(() =>
            policy.Run(() =>
            {
                callCount++;
                throw new SapComponentNotFoundException("wnd[0]/usr/txtFoo");
            }));

        Assert.Equal(1, callCount);
    }
}
