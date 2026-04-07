namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Immutable snapshot of a single node inside a <see cref="SapTreeSnapshot"/>.
/// </summary>
public sealed class SapTreeNodeSnapshot
{
    /// <summary>Node key as returned by the SAP tree control.</summary>
    public string NodeKey { get; init; } = string.Empty;

    /// <summary>Display text of the node.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Whether the node has child nodes.</summary>
    public bool HasChildren { get; init; }

    /// <summary>Whether the node is currently expanded.</summary>
    public bool IsExpanded { get; init; }

    /// <summary>Whether the node is currently selected.</summary>
    public bool IsSelected { get; init; }

    /// <summary>Nesting level (0 = root).</summary>
    public int Level { get; init; }

    /// <inheritdoc/>
    public override string ToString() => $"TreeNode \"{Text}\" (key={NodeKey}, level={Level})";
}
