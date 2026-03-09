using SapGui.Wrapper.Tests.Helpers;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="GuiSession.DismissPopup"/> heuristic routing logic.
/// Pure .NET – no SAP required.
/// </summary>
public sealed class DismissPopupTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="GuiMessageWindow"/> backed by a <see cref="FakeComObject"/>
    /// whose <c>Text</c> property is used for both <c>Title</c> and body <c>Text</c>
    /// (FakeComObject does not implement <c>FindById</c>, so both fall back to
    /// <c>GetString("Text")</c>).
    /// </summary>
    private static (GuiMessageWindow popup, FakeComObject raw) MakePopup(
        string titleAndText,
        params string[] buttonLabels)
    {
        var raw = new FakeComObject { Type = "GuiMessageWindow", Text = titleAndText };
        foreach (var label in buttonLabels)
            raw.WithButton(label);

        var popup = (GuiMessageWindow)GuiSession.WrapComponent(raw);
        return (popup, raw);
    }

    // ── Multiple Logon ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("MULTIPLE LOGON")]
    [InlineData("User already logged on - MULTIPLE LOGON")]
    [InlineData("ALREADY LOGGED ON")]
    public void DismissPopup_MultipleLogon_ClicksContinueAndReturnsTrue(string titleText)
    {
        var (popup, _) = MakePopup(titleText, "Terminate Other Logon", "Continue", "Cancel");

        bool result = GuiSession.DismissPopup(popup);

        Assert.True(result);
        // Verify the "Continue" button was pressed and not any other
        var buttons = popup.GetButtons();
        Assert.False(((FakeButtonObj)buttons[0].RawObject).WasPressed, "Should NOT press 'Terminate'");
        Assert.True( ((FakeButtonObj)buttons[1].RawObject).WasPressed, "Should press 'Continue'");
        Assert.False(((FakeButtonObj)buttons[2].RawObject).WasPressed, "Should NOT press 'Cancel'");
    }

    // ── License expiration ────────────────────────────────────────────────────

    [Theory]
    [InlineData("LICENSE EXPIRATION WARNING")]      // title matches LICENSE EXPIR
    [InlineData("LICENSE EXPIRED")]                 // title matches LICENSE EXPIR
    [InlineData("LICENSE WILL EXPIRE IN 5 DAYS")]  // text matches LICENSE WILL EXPIRE
    public void DismissPopup_LicenseExpiry_ClicksOkAndReturnsTrue(string titleText)
    {
        var (popup, raw) = MakePopup(titleText, "OK");

        bool result = GuiSession.DismissPopup(popup);

        Assert.True(result);
        Assert.True(((FakeButtonObj)popup.GetButtons()[0].RawObject).WasPressed);
    }

    [Fact]
    public void DismissPopup_LicenseTitleAlone_DoesNotMatchLicenseHeuristic()
    {
        // "LICENSE" alone in the title (e.g. "Software License Agreement") must NOT be
        // auto-dismissed — only "LICENSE EXPIR" in the title is matched.
        var (popup, raw) = MakePopup("SOFTWARE LICENSE AGREEMENT", "Agree", "Disagree");

        bool result = GuiSession.DismissPopup(popup);

        // Two unrecognised buttons → falls through all heuristics → returns false
        Assert.False(result);
        Assert.False(((FakeButtonObj)popup.GetButtons()[0].RawObject).WasPressed);
        Assert.False(((FakeButtonObj)popup.GetButtons()[1].RawObject).WasPressed);
    }

    // ── System message ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("SYSTEM MESSAGE")]
    [InlineData("MESSAGE OF THE DAY")]
    public void DismissPopup_SystemMessage_ClicksOkAndReturnsTrue(string titleText)
    {
        var (popup, _) = MakePopup(titleText, "OK");

        bool result = GuiSession.DismissPopup(popup);

        Assert.True(result);
        Assert.True(((FakeButtonObj)popup.GetButtons()[0].RawObject).WasPressed);
    }

    // ── Single-button fallback ────────────────────────────────────────────────

    [Fact]
    public void DismissPopup_UnrecognisedSingleButton_PressesThatButtonAndReturnsTrue()
    {
        var (popup, _) = MakePopup("SOME UNKNOWN NOTICE", "Acknowledge");

        bool result = GuiSession.DismissPopup(popup);

        Assert.True(result);
        Assert.True(((FakeButtonObj)popup.GetButtons()[0].RawObject).WasPressed);
    }

    // ── Multi-button fallback ─────────────────────────────────────────────────

    [Fact]
    public void DismissPopup_UnrecognisedMultiButton_LeavesUntouchedAndReturnsFalse()
    {
        // An unrecognised dialog with two non-standard buttons must NOT be auto-dismissed.
        var (popup, _) = MakePopup("CONFIRM ACTION", "Yes", "No");

        bool result = GuiSession.DismissPopup(popup);

        Assert.False(result);
        Assert.False(((FakeButtonObj)popup.GetButtons()[0].RawObject).WasPressed);
        Assert.False(((FakeButtonObj)popup.GetButtons()[1].RawObject).WasPressed);
    }

    // ── Generic OK fallback ───────────────────────────────────────────────────

    [Fact]
    public void DismissPopup_UnrecognisedDialogWithOkButton_ClicksOkAndReturnsTrue()
    {
        // An unrecognised dialog that has an "OK" button should be dismissed via the
        // generic OK fallback at the bottom of DismissPopup.
        var (popup, _) = MakePopup("SOME NOTICE", "OK", "Details");

        bool result = GuiSession.DismissPopup(popup);

        Assert.True(result);
        Assert.True(((FakeButtonObj)popup.GetButtons()[0].RawObject).WasPressed);
    }
}
