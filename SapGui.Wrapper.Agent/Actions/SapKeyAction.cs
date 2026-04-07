namespace SapGui.Wrapper.Agent.Actions;

/// <summary>
/// Named keyboard shortcuts that <see cref="SapAgentSession.PressKey"/> understands.
/// Each value maps to a SAP GUI virtual key (VKey) code.
/// </summary>
public enum SapKeyAction
{
    /// <summary>Enter — VKey 0.</summary>
    Enter,

    /// <summary>F3 — Back (one screen back within the transaction) — VKey 3.</summary>
    Back,

    /// <summary>F8 — Execute (runs the current report/selection screen) — VKey 8.</summary>
    Execute,

    /// <summary>Ctrl+S — Save — VKey 11.</summary>
    Save,

    /// <summary>F12 — Cancel (discard changes and close screen) — VKey 12.</summary>
    Cancel,

    /// <summary>Shift+F3 — Exit (return to previous menu level) — VKey 15.</summary>
    Exit,

    /// <summary>F4 — Input Help / Possible Values — VKey 4.</summary>
    F4,

    /// <summary>Scroll to the top of the current list — VKey 71.</summary>
    ScrollTop,

    /// <summary>Scroll to the bottom of the current list — VKey 82.</summary>
    ScrollBottom,

    /// <summary>Ctrl+Home — jump to first record — VKey 70.</summary>
    CtrlHome,

    /// <summary>Ctrl+End — jump to last record — VKey 83.</summary>
    CtrlEnd,
}
