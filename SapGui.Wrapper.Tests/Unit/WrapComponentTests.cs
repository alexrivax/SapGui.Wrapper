using SapGui.Wrapper.Tests.Helpers;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Tests that <see cref="GuiSession.WrapComponent"/> returns the correct
/// typed wrapper for every recognised SAP component type string.
/// Pure .NET – no SAP required.
/// </summary>
public sealed class WrapComponentTests
{
    // ── Window types ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("GuiMainWindow")]
    [InlineData("GuiFrameWindow")]
    [InlineData("GuiModalWindow")]
    public void WrapComponent_WindowTypes_ReturnGuiMainWindow(string typeName)
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType(typeName));
        Assert.IsType<GuiMainWindow>(result);
    }

    // ── Input element types ───────────────────────────────────────────────────

    [Theory]
    [InlineData("GuiTextField")]
    [InlineData("GuiCTextField")]
    [InlineData("GuiPasswordField")]
    [InlineData("GuiOkCodeField")]
    public void WrapComponent_TextFieldTypes_ReturnGuiTextField(string typeName)
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType(typeName));
        Assert.IsType<GuiTextField>(result);
    }

    [Fact]
    public void WrapComponent_GuiButton_ReturnsGuiButton()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiButton"));
        Assert.IsType<GuiButton>(result);
    }

    [Fact]
    public void WrapComponent_GuiCheckBox_ReturnsGuiCheckBox()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiCheckBox"));
        Assert.IsType<GuiCheckBox>(result);
    }

    [Fact]
    public void WrapComponent_GuiRadioButton_ReturnsGuiRadioButton()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiRadioButton"));
        Assert.IsType<GuiRadioButton>(result);
    }

    [Fact]
    public void WrapComponent_GuiComboBox_ReturnsGuiComboBox()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiComboBox"));
        Assert.IsType<GuiComboBox>(result);
    }

    [Fact]
    public void WrapComponent_GuiLabel_ReturnsGuiLabel()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiLabel"));
        Assert.IsType<GuiLabel>(result);
    }

    [Fact]
    public void WrapComponent_GuiStatusbar_ReturnsGuiStatusbar()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiStatusbar"));
        Assert.IsType<GuiStatusbar>(result);
    }

    // ── Grid / table / tree ───────────────────────────────────────────────────

    [Fact]
    public void WrapComponent_GuiTable_ReturnsGuiTable()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiTable"));
        Assert.IsType<GuiTable>(result);
    }

    [Fact]
    public void WrapComponent_GuiGridView_ReturnsGuiGridView()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiGridView"));
        Assert.IsType<GuiGridView>(result);
    }

    [Fact]
    public void WrapComponent_GuiTree_ReturnsGuiTree()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiTree"));
        Assert.IsType<GuiTree>(result);
    }

    // ── Tab strip ─────────────────────────────────────────────────────────────

    [Fact]
    public void WrapComponent_GuiTabStrip_ReturnsGuiTabStrip()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiTabStrip"));
        Assert.IsType<GuiTabStrip>(result);
    }

    [Fact]
    public void WrapComponent_GuiTab_ReturnsGuiTab()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiTab"));
        Assert.IsType<GuiTab>(result);
    }

    // ── Toolbar / menu ────────────────────────────────────────────────────────

    [Fact]
    public void WrapComponent_GuiToolbar_ReturnsGuiToolbar()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiToolbar"));
        Assert.IsType<GuiToolbar>(result);
    }

    [Fact]
    public void WrapComponent_GuiMenubar_ReturnsGuiMenubar()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiMenubar"));
        Assert.IsType<GuiMenubar>(result);
    }

    [Theory]
    [InlineData("GuiMenu")]
    [InlineData("GuiSubMenu")]
    public void WrapComponent_MenuTypes_ReturnGuiMenu(string typeName)
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType(typeName));
        Assert.IsType<GuiMenu>(result);
    }

    [Fact]
    public void WrapComponent_GuiContextMenu_ReturnsGuiContextMenu()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiContextMenu"));
        Assert.IsType<GuiContextMenu>(result);
    }

    [Fact]
    public void WrapComponent_GuiMessageWindow_ReturnsGuiMessageWindow()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiMessageWindow"));
        Assert.IsType<GuiMessageWindow>(result);
    }

    // ── Unknown / unimplemented types ─────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("SomeFutureControl")]
    [InlineData("GuiOfficeIntegration")]
    public void WrapComponent_Unknown_FallsBackToGuiComponent(string typeName)
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType(typeName));
        // Exact type must be GuiComponent, not a subclass
        Assert.Equal(typeof(GuiComponent), result.GetType());
    }

    // ── Typed wrapper retains RawObject identity ──────────────────────────────

    [Fact]
    public void WrapComponent_ResultRawObject_IsSameInstanceAsInput()
    {
        var fake   = FakeComObject.OfType("GuiTextField");
        var result = GuiSession.WrapComponent(fake);
        Assert.Same(fake, result.RawObject);
    }

    // ── Priority 3 – new wrappers ─────────────────────────────────────────────

    [Fact]
    public void WrapComponent_GuiScrollContainer_ReturnsGuiScrollContainer()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiScrollContainer"));
        Assert.IsType<GuiScrollContainer>(result);
    }

    [Fact]
    public void WrapComponent_GuiUserArea_ReturnsGuiUserArea()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiUserArea"));
        Assert.IsType<GuiUserArea>(result);
    }

    [Fact]
    public void WrapComponent_GuiCalendar_ReturnsGuiCalendar()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiCalendar"));
        Assert.IsType<GuiCalendar>(result);
    }

    [Fact]
    public void WrapComponent_GuiHTMLViewer_ReturnsGuiHTMLViewer()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiHTMLViewer"));
        Assert.IsType<GuiHTMLViewer>(result);
    }

    [Fact]
    public void WrapComponent_GuiShell_ReturnsGuiShell()
    {
        var result = GuiSession.WrapComponent(FakeComObject.OfType("GuiShell"));
        Assert.IsType<GuiShell>(result);
    }
}
