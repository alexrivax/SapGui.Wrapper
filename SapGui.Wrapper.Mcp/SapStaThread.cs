using System.Threading.Channels;

namespace SapGui.Wrapper.Mcp;

/// <summary>
/// Encapsulates a dedicated STA thread that serialises all SAP COM calls.
/// <para>
/// SAP GUI scripting objects are COM STA objects: every method call must execute
/// on the thread that created the object. This class ensures all work items
/// dispatched via <see cref="RunAsync{T}"/> execute on that single STA thread.
/// </para>
/// </summary>
public sealed class SapStaThread : IDisposable
{
    private readonly Channel<Func<Task>> _queue =
        Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions { SingleReader = true });

    private readonly Thread _thread;
    private bool _disposed;

    /// <summary>Starts the dedicated STA thread.</summary>
    public SapStaThread()
    {
        _thread = new Thread(RunLoop)
        {
            Name = "SapGui-STA",
            IsBackground = true,
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    /// <summary>
    /// Marshals <paramref name="work"/> onto the STA thread and returns its result asynchronously.
    /// </summary>
    /// <typeparam name="T">Return type of the work item.</typeparam>
    /// <param name="work">Synchronous function to execute on the STA thread.</param>
    /// <returns>A task that completes with the result of <paramref name="work"/>.</returns>
    public Task<T> RunAsync<T>(Func<T> work)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        bool enqueued = _queue.Writer.TryWrite(async () =>
        {
            try
            {
                tcs.SetResult(work());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            await Task.CompletedTask;
        });

        if (!enqueued)
            tcs.SetException(new ObjectDisposedException(nameof(SapStaThread)));

        return tcs.Task;
    }

    /// <summary>
    /// Marshals a void action onto the STA thread.
    /// </summary>
    public Task RunAsync(Action work) =>
        RunAsync<bool>(() => { work(); return true; });

    // ── Private ───────────────────────────────────────────────────────────────

    private void RunLoop()
    {
        while (_queue.Reader.TryRead(out var item) || WaitForItem(out item))
        {
            item().GetAwaiter().GetResult();
        }
    }

    private bool WaitForItem(out Func<Task> item)
    {
        var task = _queue.Reader.ReadAsync().AsTask();
        try
        {
            task.Wait();
            item = task.Result;
            return true;
        }
        catch
        {
            item = null!;
            return false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _queue.Writer.Complete();

        // Wait for the queue to drain before letting the thread exit.
        _thread.Join(TimeSpan.FromSeconds(5));
    }
}
