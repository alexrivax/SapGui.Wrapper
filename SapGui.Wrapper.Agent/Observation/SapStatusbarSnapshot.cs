namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Immutable snapshot of the SAP status bar at the time of a screen scan.
/// </summary>
public sealed class SapStatusbarSnapshot
{
    /// <summary>Status bar message text.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Message type code.
    /// <c>S</c>=Success, <c>W</c>=Warning, <c>E</c>=Error, <c>A</c>=Abort, <c>I</c>=Info, <c>""</c>=None.
    /// </summary>
    public string MessageType { get; init; } = string.Empty;

    /// <summary>Whether the status bar shows an error or abort message.</summary>
    public bool IsError   => MessageType == "E" || MessageType == "A";

    /// <summary>Whether the status bar shows a warning message.</summary>
    public bool IsWarning => MessageType == "W";

    /// <summary>Whether the status bar shows a success message.</summary>
    public bool IsSuccess => MessageType == "S";

    /// <inheritdoc/>
    public override string ToString() =>
        string.IsNullOrEmpty(MessageType) ? Text : $"[{MessageType}] {Text}";
}
