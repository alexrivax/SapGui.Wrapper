namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Immutable snapshot of a single row in a <see cref="SapGridSnapshot"/>.
/// </summary>
public sealed class SapGridRowSnapshot
{
    /// <summary>Zero-based row index within the grid.</summary>
    public int RowIndex { get; init; }

    /// <summary>Mapping from column name/key to cell value string.</summary>
    public IReadOnlyDictionary<string, string> Cells { get; init; } =
        new Dictionary<string, string>();

    /// <inheritdoc/>
    public override string ToString() => $"Row[{RowIndex}] ({Cells.Count} cells)";
}
