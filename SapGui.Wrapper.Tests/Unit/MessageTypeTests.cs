using SapGui.Wrapper.Tests.Helpers;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Tests for the message-type classification logic in
/// <see cref="GuiStatusbar"/> and <see cref="GuiMessageWindow"/>.
/// Pure .NET – no SAP required.
/// </summary>
public sealed class MessageTypeTests
{
    // ── GuiStatusbar ──────────────────────────────────────────────────────────

    private static GuiStatusbar MakeStatusbar(string messageType, string text = "") =>
        (GuiStatusbar)GuiSession.WrapComponent(
            new FakeComObject { Type = "GuiStatusbar", MessageType = messageType, Text = text });

    [Theory]
    [InlineData("S", true,  false, false)]
    [InlineData("W", false, true,  false)]
    [InlineData("E", false, false, true)]
    [InlineData("A", false, false, true)]   // Abort is treated as error
    [InlineData("I", false, false, false)]
    [InlineData("",  false, false, false)]
    public void GuiStatusbar_MessageTypeFlags(
        string type, bool isSuccess, bool isWarning, bool isError)
    {
        var sb = MakeStatusbar(type);
        Assert.Equal(isSuccess, sb.IsSuccess);
        Assert.Equal(isWarning, sb.IsWarning);
        Assert.Equal(isError,   sb.IsError);
    }

    [Fact]
    public void GuiStatusbar_Text_ReturnsMessageText()
    {
        var sb = MakeStatusbar("S", "Record saved.");
        Assert.Equal("Record saved.", sb.Text);
    }

    [Fact]
    public void GuiStatusbar_MessageType_ReturnsRawType()
    {
        var sb = MakeStatusbar("W");
        Assert.Equal("W", sb.MessageType);
    }

    // ── GuiMessageWindow ──────────────────────────────────────────────────────

    private static GuiMessageWindow MakePopup(string messageType, string text = "") =>
        (GuiMessageWindow)GuiSession.WrapComponent(
            new FakeComObject { Type = "GuiMessageWindow", MessageType = messageType, Text = text });

    [Theory]
    [InlineData("S", true,  false, false, false)]
    [InlineData("W", false, true,  false, false)]
    [InlineData("E", false, false, true,  false)]
    [InlineData("A", false, false, true,  false)]
    [InlineData("I", false, false, false, true)]
    [InlineData("",  false, false, false, false)]
    public void GuiMessageWindow_MessageTypeFlags(
        string type, bool isSuccess, bool isWarning, bool isError, bool isInfo)
    {
        var popup = MakePopup(type);
        Assert.Equal(isSuccess, popup.IsSuccess);
        Assert.Equal(isWarning, popup.IsWarning);
        Assert.Equal(isError,   popup.IsError);
        Assert.Equal(isInfo,    popup.IsInfo);
    }

    [Fact]
    public void GuiMessageWindow_Text_ReturnsPopupText()
    {
        var popup = MakePopup("E", "An error occurred.");
        Assert.Equal("An error occurred.", popup.Text);
    }
}
