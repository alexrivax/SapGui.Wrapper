using SapGui.Wrapper.Tests.Helpers;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Tests for <see cref="SapComponentTypeHelper.FromString"/> and the
/// <see cref="SapComponentType"/> enum values.
/// These are pure .NET tests – no SAP required.
/// </summary>
public sealed class SapComponentTypeTests
{
    // ── Round-trip: known SAP type strings → correct enum value ──────────────

    [Theory]
    [InlineData("GuiApplication",       SapComponentType.GuiApplication)]
    [InlineData("GuiConnection",        SapComponentType.GuiConnection)]
    [InlineData("GuiSession",           SapComponentType.GuiSession)]
    [InlineData("GuiMainWindow",        SapComponentType.GuiMainWindow)]
    [InlineData("GuiFrameWindow",       SapComponentType.GuiFrameWindow)]
    [InlineData("GuiModalWindow",       SapComponentType.GuiModalWindow)]
    [InlineData("GuiUserArea",          SapComponentType.GuiUserArea)]
    [InlineData("GuiTabStrip",          SapComponentType.GuiTabStrip)]
    [InlineData("GuiTab",               SapComponentType.GuiTab)]
    [InlineData("GuiScrollContainer",   SapComponentType.GuiScrollContainer)]
    [InlineData("GuiSplitterContainer", SapComponentType.GuiSplitterContainer)]
    [InlineData("GuiContainerShell",    SapComponentType.GuiContainerShell)]
    [InlineData("GuiCustomControl",     SapComponentType.GuiCustomControl)]
    [InlineData("GuiToolbar",           SapComponentType.GuiToolbar)]
    [InlineData("GuiMenubar",           SapComponentType.GuiMenubar)]
    [InlineData("GuiSubMenu",           SapComponentType.GuiSubMenu)]
    [InlineData("GuiContextMenu",       SapComponentType.GuiContextMenu)]
    [InlineData("GuiStatusbar",         SapComponentType.GuiStatusbar)]
    [InlineData("GuiStatusPane",        SapComponentType.GuiStatusPane)]
    [InlineData("GuiTitlebar",          SapComponentType.GuiTitlebar)]
    [InlineData("GuiTextField",         SapComponentType.GuiTextField)]
    [InlineData("GuiCTextField",        SapComponentType.GuiCTextField)]
    [InlineData("GuiPasswordField",     SapComponentType.GuiPasswordField)]
    [InlineData("GuiOkCodeField",       SapComponentType.GuiOkCodeField)]
    [InlineData("GuiLabel",             SapComponentType.GuiLabel)]
    [InlineData("GuiButton",            SapComponentType.GuiButton)]
    [InlineData("GuiRadioButton",       SapComponentType.GuiRadioButton)]
    [InlineData("GuiCheckBox",          SapComponentType.GuiCheckBox)]
    [InlineData("GuiComboBox",          SapComponentType.GuiComboBox)]
    [InlineData("GuiMessageWindow",     SapComponentType.GuiMessageWindow)]
    [InlineData("GuiTable",             SapComponentType.GuiTable)]
    [InlineData("GuiTableControl",      SapComponentType.GuiTableControl)]
    [InlineData("GuiGridView",          SapComponentType.GuiGridView)]
    [InlineData("GuiTree",              SapComponentType.GuiTree)]
    [InlineData("GuiApoGrid",           SapComponentType.GuiApoGrid)]
    [InlineData("GuiShell",             SapComponentType.GuiShell)]
    [InlineData("GuiChart",             SapComponentType.GuiChart)]
    [InlineData("GuiNetChart",          SapComponentType.GuiNetChart)]
    [InlineData("GuiBarChart",          SapComponentType.GuiBarChart)]
    [InlineData("GuiGanttChart",        SapComponentType.GuiGanttChart)]
    [InlineData("GuiMap",               SapComponentType.GuiMap)]
    [InlineData("GuiHTMLViewer",        SapComponentType.GuiHTMLViewer)]
    [InlineData("GuiActiveXCtrl",       SapComponentType.GuiActiveXCtrl)]
    [InlineData("GuiCalendar",          SapComponentType.GuiCalendar)]
    [InlineData("GuiOfficeIntegration", SapComponentType.GuiOfficeIntegration)]
    public void FromString_KnownType_ReturnsCorrectEnum(string typeName, SapComponentType expected)
    {
        var result = SapComponentTypeHelper.FromString(typeName);
        Assert.Equal(expected, result);
    }

    // ── Fallback cases ────────────────────────────────────────────────────────

    [Fact]
    public void FromString_NullInput_ReturnsUnknown()
    {
        Assert.Equal(SapComponentType.Unknown, SapComponentTypeHelper.FromString(null));
    }

    [Fact]
    public void FromString_EmptyString_ReturnsUnknown()
    {
        Assert.Equal(SapComponentType.Unknown, SapComponentTypeHelper.FromString(""));
    }

    [Fact]
    public void FromString_UnrecognisedString_ReturnsUnknown()
    {
        Assert.Equal(SapComponentType.Unknown, SapComponentTypeHelper.FromString("NotARealType"));
    }

    [Fact]
    public void FromString_IsCaseSensitive_LowercaseReturnsUnknown()
    {
        // SAP type strings are PascalCase; lower-case should not match
        Assert.Equal(SapComponentType.Unknown, SapComponentTypeHelper.FromString("guitextfield"));
        Assert.Equal(SapComponentType.Unknown, SapComponentTypeHelper.FromString("GUIBUTTON"));
    }

    // ── Enum completeness ─────────────────────────────────────────────────────

    [Fact]
    public void Enum_DoesNotContainZeroValueOtherThanUnknown()
    {
        // Ensure no other member accidentally has the value 0
        var zeroMembers = Enum.GetValues<SapComponentType>()
                              .Where(v => (int)v == 0)
                              .ToList();

        Assert.Single(zeroMembers);
        Assert.Equal(SapComponentType.Unknown, zeroMembers[0]);
    }

    [Fact]
    public void Enum_AllValuesAreUnique()
    {
        var values = Enum.GetValues<SapComponentType>().ToList();
        Assert.Equal(values.Count, values.Distinct().Count());
    }
}
