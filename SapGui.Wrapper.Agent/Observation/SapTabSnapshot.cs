namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Immutable snapshot of a single tab page inside a <see cref="SapTabStripSnapshot"/>.
/// </summary>
public sealed class SapTabSnapshot
{
    /// <summary>Full COM path of the tab.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Tab label text.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Whether this tab is currently selected/active.</summary>
    public bool IsSelected { get; init; }

    /// <inheritdoc/>
    public override string ToString() => $"Tab \"{Text}\" (selected={IsSelected})";
}
