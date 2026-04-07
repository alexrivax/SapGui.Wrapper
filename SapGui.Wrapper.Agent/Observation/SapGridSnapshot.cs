namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Immutable snapshot of an ALV grid view captured during a screen scan.
/// </summary>
public sealed class SapGridSnapshot
{
    /// <summary>Full COM path of the grid.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Ordered list of column name/key strings.</summary>
    public IReadOnlyList<string> ColumnNames { get; init; } = Array.Empty<string>();

    /// <summary>Rows currently visible in the grid viewport.</summary>
    public IReadOnlyList<SapGridRowSnapshot> VisibleRows { get; init; } =
        Array.Empty<SapGridRowSnapshot>();

    /// <summary>Total number of data rows (including those scrolled off-screen).</summary>
    public int TotalRowCount { get; init; }

    /// <summary>Number of rows currently visible in the viewport.</summary>
    public int VisibleRowCount { get; init; }

    /// <summary>Zero-based index of the first visible row.</summary>
    public int FirstVisibleRow { get; init; }

    /// <summary>Whether the grid contains rows beyond what is currently visible.</summary>
    public bool HasMoreRows => TotalRowCount > VisibleRowCount;

    /// <inheritdoc/>
    public override string ToString() =>
        $"Grid [{Id}] {VisibleRowCount}/{TotalRowCount} rows, {ColumnNames.Count} cols";
}
