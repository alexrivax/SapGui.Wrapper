namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Categorises the active SAP screen so agents can adapt their strategy.
/// Detected by <see cref="SapScreenScanner.DetectScreenType"/>.
/// </summary>
public enum SapScreenType
{
    /// <summary>Type could not be determined.</summary>
    Unknown,

    /// <summary>Main SAP Easy Access menu.</summary>
    EasyAccess,

    /// <summary>Report/transaction input screen — most /n* transactions start here.</summary>
    SelectionScreen,

    /// <summary>ALV grid result screen.</summary>
    AlvGrid,

    /// <summary>ALV tree result.</summary>
    AlvTree,

    /// <summary>Old-style ABAP table control.</summary>
    ClassicTable,

    /// <summary>Single-record create/change/display form (VA01, MM01, etc.).</summary>
    EntryForm,

    /// <summary>Read-only version of an entry form.</summary>
    DisplayForm,

    /// <summary>GuiModalWindow — form dialog.</summary>
    Dialog,

    /// <summary>GuiMessageWindow — simple ok/cancel popup.</summary>
    MessageDialog,

    /// <summary>Left-side tree navigation (e.g. IMG).</summary>
    TreeNavigation,

    /// <summary>Transaction result in an embedded browser.</summary>
    HtmlViewer,

    /// <summary>GuiCalendar control.</summary>
    Calendar,

    /// <summary>SAP logon screen.</summary>
    Login,
}
