namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Immutable snapshot of a top-level menu bar entry and its direct children
/// captured during a screen scan.
/// </summary>
public sealed class SapMenuSnapshot
{
    /// <summary>Full COM path of the menu item.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Menu item label text.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Direct child menu items (one level deep).</summary>
    public IReadOnlyList<SapMenuSnapshot> Children { get; init; } =
        Array.Empty<SapMenuSnapshot>();

    /// <inheritdoc/>
    public override string ToString() => $"Menu \"{Text}\" ({Children.Count} children)";
}
