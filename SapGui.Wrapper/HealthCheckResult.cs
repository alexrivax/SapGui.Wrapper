namespace SapGui.Wrapper;

/// <summary>
/// Returned by <see cref="SapGuiClient.HealthCheck"/>.
/// </summary>
/// <param name="IsHealthy">
///   <c>true</c> when all checks passed and SAP GUI is ready for automation.
/// </param>
/// <param name="Findings">
///   Ordered list of human-readable findings. Each entry is prefixed with
///   <c>OK:</c>, <c>WARN:</c>, or <c>FAIL:</c> so callers can filter by severity.
/// </param>
public sealed record HealthCheckResult(bool IsHealthy, IReadOnlyList<string> Findings)
{
    /// <summary>
    /// Normalises a <c>null</c> findings list to an empty list so that
    /// <see cref="FailureSummary"/> and <see cref="ToString"/> never throw.
    /// </summary>
    public IReadOnlyList<string> Findings { get; init; } =
        Findings ?? Array.Empty<string>();

    /// <summary>
    /// Returns only the <c>FAIL:</c> lines, one per line.
    /// Useful for building a compact exception message.
    /// </summary>
    public string FailureSummary =>
        string.Join(Environment.NewLine,
                    Findings.Where(f => f.StartsWith("FAIL:", StringComparison.Ordinal)));

    /// <summary>Returns all findings joined by newlines — handy for logging.</summary>
    public override string ToString() => string.Join(Environment.NewLine, Findings);
}
