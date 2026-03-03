namespace SapGui.Wrapper;

/// <summary>
/// Maps the SAP GUI scripting <c>Type</c> string to an enum for
/// convenient switch/pattern matching in .NET code.
/// Only the most common component types are listed here; the
/// string value is always available via <see cref="GuiComponent.TypeName"/>.
/// </summary>
#pragma warning disable CS1591  // enum member names are self-documenting SAP type strings
public enum SapComponentType
{
    Unknown = 0,

    // Top-level administrative objects
    GuiApplication,
    GuiConnection,
    GuiSession,

    // Window types
    GuiMainWindow,
    GuiFrameWindow,
    GuiModalWindow,

    // Container / layout types
    GuiUserArea,
    GuiTabStrip,
    GuiTab,
    GuiScrollContainer,
    GuiSplitterContainer,
    GuiContainerShell,
    GuiCustomControl,

    // Toolbars, menus, status
    GuiToolbar,
    GuiMenubar,
    GuiMenu,
    GuiSubMenu,
    GuiContextMenu,
    GuiStatusbar,
    GuiStatusPane,
    GuiTitlebar,

    // Simple input elements
    GuiTextField,
    GuiCTextField,      // command field / OK-code field
    GuiPasswordField,
    GuiOkCodeField,     // transaction input box (alias for GuiCTextField in most systems)
    GuiLabel,
    GuiButton,
    GuiRadioButton,
    GuiCheckBox,
    GuiComboBox,
    GuiMessageWindow,

    // Classic / complex table controls
    GuiTable,           // classic ABAP table control (GuiTableControl in older docs)
    GuiTableControl,    // explicit alias seen in some system type strings
    GuiGridView,        // ALV grid (shell-based)
    GuiTree,            // tree control (shell-based)
    GuiApoGrid,         // APO-specific ALV grid

    // Shell-based controls
    GuiShell,
    GuiChart,
    GuiNetChart,
    GuiBarChart,
    GuiGanttChart,
    GuiMap,
    GuiHTMLViewer,
    GuiActiveXCtrl,

    // Misc / platform-specific
    GuiCalendar,
    GuiOfficeIntegration,
}
#pragma warning restore CS1591

internal static class SapComponentTypeHelper
{
    internal static SapComponentType FromString(string? typeName) =>
        typeName switch
        {
            "GuiApplication"        => SapComponentType.GuiApplication,
            "GuiConnection"         => SapComponentType.GuiConnection,
            "GuiSession"            => SapComponentType.GuiSession,
            "GuiMainWindow"         => SapComponentType.GuiMainWindow,
            "GuiFrameWindow"        => SapComponentType.GuiFrameWindow,
            "GuiModalWindow"        => SapComponentType.GuiModalWindow,
            "GuiUserArea"           => SapComponentType.GuiUserArea,
            "GuiTabStrip"           => SapComponentType.GuiTabStrip,
            "GuiTab"                => SapComponentType.GuiTab,
            "GuiScrollContainer"    => SapComponentType.GuiScrollContainer,
            "GuiSplitterContainer"  => SapComponentType.GuiSplitterContainer,
            "GuiToolbar"            => SapComponentType.GuiToolbar,
            "GuiMenubar"            => SapComponentType.GuiMenubar,
            "GuiMenu"               => SapComponentType.GuiMenu,
            "GuiSubMenu"            => SapComponentType.GuiSubMenu,
            "GuiContextMenu"        => SapComponentType.GuiContextMenu,
            "GuiTextField"          => SapComponentType.GuiTextField,
            "GuiCTextField"         => SapComponentType.GuiCTextField,
            "GuiPasswordField"      => SapComponentType.GuiPasswordField,
            "GuiLabel"              => SapComponentType.GuiLabel,
            "GuiButton"             => SapComponentType.GuiButton,
            "GuiRadioButton"        => SapComponentType.GuiRadioButton,
            "GuiCheckBox"           => SapComponentType.GuiCheckBox,
            "GuiComboBox"           => SapComponentType.GuiComboBox,
            "GuiStatusbar"          => SapComponentType.GuiStatusbar,
            "GuiStatusPane"         => SapComponentType.GuiStatusPane,
            "GuiTitlebar"           => SapComponentType.GuiTitlebar,
            "GuiTable"              => SapComponentType.GuiTable,
            "GuiTableControl"       => SapComponentType.GuiTableControl,
            "GuiGridView"           => SapComponentType.GuiGridView,
            "GuiTree"               => SapComponentType.GuiTree,
            "GuiApoGrid"            => SapComponentType.GuiApoGrid,
            "GuiShell"              => SapComponentType.GuiShell,
            "GuiHTMLViewer"         => SapComponentType.GuiHTMLViewer,
            "GuiCustomControl"      => SapComponentType.GuiCustomControl,
            "GuiContainerShell"     => SapComponentType.GuiContainerShell,
            "GuiActiveXCtrl"        => SapComponentType.GuiActiveXCtrl,
            "GuiChart"              => SapComponentType.GuiChart,
            "GuiNetChart"           => SapComponentType.GuiNetChart,
            "GuiBarChart"           => SapComponentType.GuiBarChart,
            "GuiGanttChart"         => SapComponentType.GuiGanttChart,
            "GuiMap"                => SapComponentType.GuiMap,
            "GuiOkCodeField"        => SapComponentType.GuiOkCodeField,
            "GuiMessageWindow"      => SapComponentType.GuiMessageWindow,
            "GuiCalendar"           => SapComponentType.GuiCalendar,
            "GuiOfficeIntegration"  => SapComponentType.GuiOfficeIntegration,
            _                       => SapComponentType.Unknown
        };
}
