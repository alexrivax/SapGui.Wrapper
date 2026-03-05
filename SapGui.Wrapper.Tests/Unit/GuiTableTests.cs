using SapGui.Wrapper.Tests.Helpers;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Tests for the new Priority-1 members added to <see cref="GuiTable"/>.
/// Pure .NET – no SAP required.
/// </summary>
public sealed class GuiTableTests
{
    [Fact]
    public void FirstVisibleRow_ReflectsVerticalScrollbarPosition()
    {
        // GuiTable.FirstVisibleRow reads VerticalScrollbar.Position via GetInt.
        // FakeComObject uses a flat property bag, so the dotted path will not
        // resolve and GetInt returns 0 – verifying the safe fallback.
        var fake = new FakeComObject { Type = "GuiTable" };
        var table = (GuiTable)GuiSession.WrapComponent(fake);

        Assert.Equal(0, table.FirstVisibleRow);
    }

    [Fact]
    public void VisibleRowCount_OnMissingComProperty_ReturnsZero()
    {
        var fake  = new FakeComObject { Type = "GuiTable" };
        var table = (GuiTable)GuiSession.WrapComponent(fake);

        Assert.Equal(0, table.VisibleRowCount);
    }

    [Fact]
    public void CurrentCellRow_OnMissingComMethod_ReturnsMinusOne()
    {
        var fake  = new FakeComObject { Type = "GuiTable" };
        var table = (GuiTable)GuiSession.WrapComponent(fake);

        // Invoke("CurrentCell") throws on FakeComObject → safe fallback -1
        Assert.Equal(-1, table.CurrentCellRow);
    }

    [Fact]
    public void CurrentCellColumn_OnMissingComMethod_ReturnsEmptyString()
    {
        var fake  = new FakeComObject { Type = "GuiTable" };
        var table = (GuiTable)GuiSession.WrapComponent(fake);

        Assert.Equal(string.Empty, table.CurrentCellColumn);
    }

    [Fact]
    public void VerticalScrollbarPosition_IsObsolete_AndMatchesFirstVisibleRow()
    {
        var fake  = new FakeComObject { Type = "GuiTable" };
        var table = (GuiTable)GuiSession.WrapComponent(fake);

        // Both should delegate to VerticalScrollbar.Position → 0 on FakeComObject
#pragma warning disable CS0618
        Assert.Equal(table.FirstVisibleRow, table.VerticalScrollbarPosition);
#pragma warning restore CS0618
    }
}
