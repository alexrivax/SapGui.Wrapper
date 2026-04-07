namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Immutable snapshot of a SAP tree control captured during a screen scan.
/// Contains only the nodes that are currently visible (expanded).
/// </summary>
public sealed class SapTreeSnapshot
{
    /// <summary>Full COM path of the tree control.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Visible / expanded nodes at the time of the scan.</summary>
    public IReadOnlyList<SapTreeNodeSnapshot> VisibleNodes { get; init; } =
        Array.Empty<SapTreeNodeSnapshot>();

    /// <inheritdoc/>
    public override string ToString() =>
        $"Tree [{Id}] {VisibleNodes.Count} visible node(s)";
}
