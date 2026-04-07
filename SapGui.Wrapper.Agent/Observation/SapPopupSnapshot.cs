namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Immutable snapshot of an active SAP popup window (modal dialog or message window)
/// captured during a screen scan.
/// </summary>
public sealed class SapPopupSnapshot
{
    /// <summary>Window ID, e.g. <c>wnd[1]</c>, <c>wnd[2]</c>.</summary>
    public string WindowId { get; init; } = string.Empty;

    /// <summary>Popup window title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Main message text shown in the popup.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Message severity code.
    /// <c>I</c>=Info, <c>W</c>=Warning, <c>E</c>=Error, <c>S</c>=Success, <c>A</c>=Abort.
    /// Empty string for generic form dialogs.
    /// </summary>
    public string MessageType { get; init; } = string.Empty;

    /// <summary>Buttons available in the popup.</summary>
    public IReadOnlyList<SapButtonSnapshot> Buttons { get; init; } =
        Array.Empty<SapButtonSnapshot>();

    /// <summary>Whether the popup is an error or abort dialog.</summary>
    public bool IsError   => MessageType == "E" || MessageType == "A";

    /// <summary>Whether the popup is a warning dialog.</summary>
    public bool IsWarning => MessageType == "W";

    /// <inheritdoc/>
    public override string ToString() =>
        $"Popup [{WindowId}] \"{Title}\" type={MessageType}";
}
