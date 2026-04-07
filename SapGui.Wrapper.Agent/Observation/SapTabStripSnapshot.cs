namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Immutable snapshot of a SAP tab strip control captured during a screen scan.
/// </summary>
public sealed class SapTabStripSnapshot
{
    /// <summary>Full COM path of the tab strip.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>All tabs in this strip (in display order).</summary>
    public IReadOnlyList<SapTabSnapshot> Tabs { get; init; } = Array.Empty<SapTabSnapshot>();

    /// <summary>The currently active tab, or <see langword="null"/> if none is selected.</summary>
    public SapTabSnapshot? ActiveTab => Tabs.FirstOrDefault(t => t.IsSelected);

    /// <inheritdoc/>
    public override string ToString() =>
        $"TabStrip [{Id}] {Tabs.Count} tab(s), active=\"{ActiveTab?.Text ?? "none"}\"";
}
