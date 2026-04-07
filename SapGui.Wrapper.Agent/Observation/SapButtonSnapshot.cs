namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Immutable snapshot of a SAP button or toolbar button captured during a screen scan.
/// </summary>
public sealed class SapButtonSnapshot
{
    /// <summary>Full COM path of the button.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Button label text.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Tooltip / accessibility description.</summary>
    public string Tooltip { get; init; } = string.Empty;

    /// <summary>Whether the button can be pressed.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Either <c>GuiButton</c> (standard push button) or <c>ToolbarButton</c>.
    /// </summary>
    public string ButtonType { get; init; } = string.Empty;

    /// <summary>Function code for toolbar buttons (e.g. <c>BACK</c>, <c>CANC</c>).</summary>
    public string FunctionCode { get; init; } = string.Empty;

    /// <inheritdoc/>
    public override string ToString() => $"Button \"{Text}\" [{Id}]";
}
