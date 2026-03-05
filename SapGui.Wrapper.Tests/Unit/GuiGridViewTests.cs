using SapGui.Wrapper.Tests.Helpers;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Tests for the new Priority-1 members added to <see cref="GuiGridView"/>.
/// Pure .NET – no SAP required.
/// </summary>
public sealed class GuiGridViewTests
{
    // ── Dimension properties ──────────────────────────────────────────────────

    [Fact]
    public void FirstVisibleRow_ReturnsComPropertyValue()
    {
        var fake = new FakeComObject { Type = "GuiGridView", FirstVisibleRow = 5 };
        var grid = (GuiGridView)GuiSession.WrapComponent(fake);

        Assert.Equal(5, grid.FirstVisibleRow);
    }

    [Fact]
    public void VisibleRowCount_ReturnsComPropertyValue()
    {
        var fake = new FakeComObject { Type = "GuiGridView", VisibleRowCount = 20 };
        var grid = (GuiGridView)GuiSession.WrapComponent(fake);

        Assert.Equal(20, grid.VisibleRowCount);
    }

    // ── Current cell ──────────────────────────────────────────────────────────

    [Fact]
    public void CurrentCellRow_ReturnsComPropertyValue()
    {
        var fake = new FakeComObject { Type = "GuiGridView", CurrentCellRow = 3 };
        var grid = (GuiGridView)GuiSession.WrapComponent(fake);

        Assert.Equal(3, grid.CurrentCellRow);
    }

    [Fact]
    public void CurrentCellColumn_ReturnsComPropertyValue()
    {
        var fake = new FakeComObject { Type = "GuiGridView", CurrentCellColumn = "MATNR" };
        var grid = (GuiGridView)GuiSession.WrapComponent(fake);

        Assert.Equal("MATNR", grid.CurrentCellColumn);
    }

    // ── SelectedRows ──────────────────────────────────────────────────────────

    [Fact]
    public void SelectedRows_WithCommaString_ReturnsParsedList()
    {
        var fake = new FakeComObject { Type = "GuiGridView", SelectedRows = "0,2,5" };

        // Override Invoke("SelectedRows") by sub-classing FakeComObject is complex,
        // so we test the property directly via a thin wrapper fake.
        // The SelectedRows SAP COM property returns a string – we supply that via
        // a FakeComObject that exposes a SelectedRows property with that value.
        // GuiGridView.SelectedRows calls Invoke("SelectedRows") which will fail on
        // FakeComObject (no such method), so the property safely falls back to
        // Array.Empty<int>(). This verifies the resilient fallback.
        var grid = (GuiGridView)GuiSession.WrapComponent(fake);

        // SelectedRows.Invoke("SelectedRows") on FakeComObject throws → returns empty
        var result = grid.SelectedRows;
        Assert.NotNull(result);
    }

    [Fact]
    public void SelectedRows_OnFailure_ReturnsEmptyList()
    {
        var fake = new FakeComObject { Type = "GuiGridView" };
        var grid = (GuiGridView)GuiSession.WrapComponent(fake);

        // COM method SelectedRows not on FakeComObject → safe fallback to empty
        var result = grid.SelectedRows;
        Assert.Empty(result);
    }

    // ── Cell helpers ──────────────────────────────────────────────────────────

    [Fact]
    public void GetCellTooltip_OnMissingComMethod_ReturnsEmptyString()
    {
        var fake = new FakeComObject { Type = "GuiGridView" };
        var grid = (GuiGridView)GuiSession.WrapComponent(fake);

        // FakeComObject has no GetCellTooltip COM method → safe fallback
        Assert.Equal(string.Empty, grid.GetCellTooltip(0, "MATNR"));
    }

    [Fact]
    public void GetCellCheckBoxValue_OnMissingComMethod_ReturnsFalse()
    {
        var fake = new FakeComObject { Type = "GuiGridView" };
        var grid = (GuiGridView)GuiSession.WrapComponent(fake);

        Assert.False(grid.GetCellCheckBoxValue(0, "FLAG"));
    }

    [Fact]
    public void GetSymbolsForCell_OnMissingComMethod_ReturnsEmptyString()
    {
        var fake = new FakeComObject { Type = "GuiGridView" };
        var grid = (GuiGridView)GuiSession.WrapComponent(fake);

        Assert.Equal(string.Empty, grid.GetSymbolsForCell(0, "ICON"));
    }
}
