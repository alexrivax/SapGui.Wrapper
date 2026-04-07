namespace SapGui.Wrapper.Agent.Actions;

/// <summary>
/// Identifies the kind of action executed by <see cref="SapAgentSession"/>.
/// </summary>
public enum SapActionType
{
    /// <summary>Set an input field value by label or ID.</summary>
    SetField,

    /// <summary>Clear an input field to empty.</summary>
    ClearField,

    /// <summary>Click a button or toolbar button by visible text or tooltip.</summary>
    ClickButton,

    /// <summary>Press a keyboard shortcut (VKey).</summary>
    PressKey,

    /// <summary>Enter a SAP transaction code.</summary>
    StartTransaction,

    /// <summary>Read rows from an ALV grid.</summary>
    ReadGrid,

    /// <summary>Select a row in an ALV grid.</summary>
    SelectGridRow,

    /// <summary>Double-click a row in an ALV grid to open its detail.</summary>
    OpenGridRow,

    /// <summary>Select a tab by visible label.</summary>
    SelectTab,

    /// <summary>Navigate a menu by slash-separated path.</summary>
    SelectMenu,

    /// <summary>Respond to an active popup dialog.</summary>
    HandlePopup,

    /// <summary>Expand a tree node by its display text.</summary>
    ExpandTreeNode,

    /// <summary>Select (and optionally double-click) a tree node by display text.</summary>
    SelectTreeNode,

    /// <summary>Wait for SAP to finish processing, then return a fresh snapshot.</summary>
    WaitAndScan,
}
