using SapGui.Wrapper.Mcp;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Verifies the STA thread infrastructure without requiring a SAP COM environment.
/// These tests run on Windows only — COM STA apartment state is not supported on Linux/macOS.
/// </summary>
[Trait("Platform", "Windows")]
public sealed class SapStaThreadTests : IDisposable
{
    private readonly SapStaThread? _sta;

    public SapStaThreadTests()
    {
        if (!OperatingSystem.IsWindows()) return;
        _sta = new SapStaThread();
    }

    public void Dispose() => _sta?.Dispose();

    [Fact]
    public async Task RunAsync_ExecutesOnStaApartment()
    {
        if (!OperatingSystem.IsWindows()) return;

        var apartment = await _sta!.RunAsync(() => Thread.CurrentThread.GetApartmentState());

        Assert.Equal(ApartmentState.STA, apartment);
    }

    [Fact]
    public async Task RunAsync_AllWorkItemsRunOnTheSameThread()
    {
        if (!OperatingSystem.IsWindows()) return;

        var threadIds = new List<int>();

        for (var i = 0; i < 5; i++)
        {
            var id = await _sta!.RunAsync(() => Environment.CurrentManagedThreadId);
            threadIds.Add(id);
        }

        Assert.True(threadIds.Distinct().Count() == 1,
            "All work items must execute on the same dedicated STA thread.");
    }

    [Fact]
    public async Task RunAsync_SurfacesExceptionToCallerThread()
    {
        if (!OperatingSystem.IsWindows()) return;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sta!.RunAsync<string>(() => throw new InvalidOperationException("test error")));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        if (!OperatingSystem.IsWindows()) return;

        var sta = new SapStaThread();
        sta.Dispose();

        // Must not throw.
        var ex = Record.Exception(() => sta.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public async Task RunAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        if (!OperatingSystem.IsWindows()) return;

        var sta = new SapStaThread();
        sta.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => sta.RunAsync(() => 42));
    }
}
