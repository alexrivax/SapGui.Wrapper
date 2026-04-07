using SapGui.Wrapper.Agent.Observation;

namespace SapGui.Wrapper.Agent.Actions;

/// <summary>
/// Outcome of a single semantic action executed via <see cref="SapAgentSession"/>.
/// Contains the before/after screen snapshots and a compact diff string for LLM consumption.
/// </summary>
public sealed class SapActionResult
{
    /// <summary>Whether the action completed without error.</summary>
    public bool Success { get; init; }

    /// <summary>Error description when <see cref="Success"/> is <see langword="false"/>.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Screen state captured immediately before the action was executed.</summary>
    public SapScreenSnapshot? SnapshotBefore { get; init; }

    /// <summary>Screen state captured immediately after the action was executed.</summary>
    public SapScreenSnapshot? SnapshotAfter { get; init; }

    /// <summary>
    /// Compact diff summary produced by <see cref="SapScreenSnapshot.DiffFrom"/>.
    /// <see langword="null"/> when no after-snapshot was taken (e.g. read-only actions).
    /// </summary>
    public string? Diff { get; init; }

    /// <summary>
    /// The exact SAP component ID that was resolved from the semantic target.
    /// Useful for debugging and audit trails.
    /// </summary>
    public string? ResolvedId { get; init; }

    // ── Factory helpers ───────────────────────────────────────────────────────

    /// <summary>Creates a successful result with before/after snapshots and a diff.</summary>
    public static SapActionResult Ok(
        SapScreenSnapshot before,
        SapScreenSnapshot after,
        string? resolvedId = null) => new()
        {
            Success = true,
            SnapshotBefore = before,
            SnapshotAfter = after,
            Diff = after.DiffFrom(before),
            ResolvedId = resolvedId,
        };

    /// <summary>Creates a successful read-only result (no after-snapshot required).</summary>
    public static SapActionResult OkReadOnly(
        SapScreenSnapshot before,
        string? resolvedId = null) => new()
        {
            Success = true,
            SnapshotBefore = before,
            ResolvedId = resolvedId,
        };

    /// <summary>Creates a failed result with an error message.</summary>
    public static SapActionResult Fail(string message, SapScreenSnapshot? before = null) => new()
    {
        Success = false,
        ErrorMessage = message,
        SnapshotBefore = before,
    };

    /// <inheritdoc/>
    public override string ToString() =>
        Success
            ? $"OK  resolved={ResolvedId}"
            : $"ERR {ErrorMessage}";
}
