namespace SapGui.Wrapper.Agent.Actions;

/// <summary>
/// How to respond to an active SAP popup / modal dialog.
/// </summary>
public enum PopupAction
{
    /// <summary>Click "OK" / "Continue" (VKey 0).</summary>
    Ok,

    /// <summary>Click "Cancel" (VKey 12).</summary>
    Cancel,

    /// <summary>Click the "Yes" button (searched by text).</summary>
    Yes,

    /// <summary>Click the "No" button (searched by text).</summary>
    No,

    /// <summary>Click a button whose text matches <c>SapAction.Value</c>.</summary>
    ByButtonText,
}
