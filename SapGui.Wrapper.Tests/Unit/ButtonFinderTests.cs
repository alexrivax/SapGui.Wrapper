using SapGui.Wrapper.Agent.Actions;
using SapGui.Wrapper.Agent.Observation;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="ButtonFinder"/>.
/// Pure .NET — no SAP connection required.
/// </summary>
public sealed class ButtonFinderTests
{
    // ── Test fixture ──────────────────────────────────────────────────────────

    private static SapScreenSnapshot BuildSnapshot() => new()
    {
        Transaction = "MM60",
        Buttons = new[]
        {
            new SapButtonSnapshot
            {
                Id           = "wnd[0]/tbar[1]/btn[8]",
                Text         = "Execute",
                Tooltip      = "Execute (F8)",
                FunctionCode = "ONLI",
                ButtonType   = "ToolbarButton",
            },
            new SapButtonSnapshot
            {
                Id           = "wnd[0]/tbar[0]/btn[3]",
                Text         = "Back",
                Tooltip      = "Back (F3)",
                FunctionCode = "BACK",
                ButtonType   = "ToolbarButton",
            },
            new SapButtonSnapshot
            {
                Id           = "wnd[0]/usr/btnSAVE",
                Text         = "Save",
                Tooltip      = "Save document",
                FunctionCode = string.Empty,
                ButtonType   = "GuiButton",
            },
            new SapButtonSnapshot
            {
                Id           = "wnd[0]/usr/btnCANCEL",
                Text         = "Cancel",
                Tooltip      = "Cancel and discard changes",
                FunctionCode = "CANC",
                ButtonType   = "GuiButton",
            },
        },
    };

    // ── Exact ID passthrough ──────────────────────────────────────────────────

    [Fact]
    public void Resolve_ExactId_ReturnsButton()
    {
        var btn = ButtonFinder.Resolve("wnd[0]/tbar[1]/btn[8]", BuildSnapshot());
        Assert.Equal("wnd[0]/tbar[1]/btn[8]", btn.Id);
    }

    // ── Exact text match ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("Execute")]
    [InlineData("Back")]
    [InlineData("Save")]
    [InlineData("Cancel")]
    public void Resolve_ExactText_ReturnsButton(string text)
    {
        var btn = ButtonFinder.Resolve(text, BuildSnapshot());
        Assert.Equal(text, btn.Text);
    }

    // ── Case-insensitive text match ───────────────────────────────────────────

    [Theory]
    [InlineData("execute", "Execute")]
    [InlineData("BACK", "Back")]
    [InlineData("save", "Save")]
    public void Resolve_CaseInsensitiveText_ReturnsButton(string input, string expectedText)
    {
        var btn = ButtonFinder.Resolve(input, BuildSnapshot());
        Assert.Equal(expectedText, btn.Text);
    }

    // ── Exact tooltip match ───────────────────────────────────────────────────

    [Fact]
    public void Resolve_ExactTooltip_ReturnsButton()
    {
        var btn = ButtonFinder.Resolve("Execute (F8)", BuildSnapshot());
        Assert.Equal("Execute", btn.Text);
    }

    // ── Function code match ───────────────────────────────────────────────────

    [Theory]
    [InlineData("BACK", "Back")]
    [InlineData("CANC", "Cancel")]
    [InlineData("ONLI", "Execute")]
    public void Resolve_FunctionCode_ReturnsButton(string code, string expectedText)
    {
        var btn = ButtonFinder.Resolve(code, BuildSnapshot());
        Assert.Equal(expectedText, btn.Text);
    }

    // ── Partial contains match ────────────────────────────────────────────────

    [Theory]
    [InlineData("Exec", "Execute")]
    [InlineData("discard", "Cancel")]  // matches tooltip "Cancel and discard changes"
    public void Resolve_PartialMatch_ReturnsButton(string partial, string expectedText)
    {
        var btn = ButtonFinder.Resolve(partial, BuildSnapshot());
        Assert.Equal(expectedText, btn.Text);
    }

    // ── Not found → exception ─────────────────────────────────────────────────

    [Fact]
    public void Resolve_NoMatch_ThrowsResolutionException()
    {
        var ex = Assert.Throws<SapAgentResolutionException>(
            () => ButtonFinder.Resolve("NonExistentButtonXYZ", BuildSnapshot()));

        Assert.Equal("NonExistentButtonXYZ", ex.Target);
        Assert.Equal("button", ex.ElementType);
        Assert.NotEmpty(ex.Candidates);
    }

    // ── Popup buttons are included ────────────────────────────────────────────

    [Fact]
    public void Resolve_ButtonInPopup_ReturnsButton()
    {
        var snapshot = new SapScreenSnapshot
        {
            Transaction = "MM60",
            Buttons = Array.Empty<SapButtonSnapshot>(),
            Popups = new[]
            {
                new SapPopupSnapshot
                {
                    WindowId = "wnd[1]",
                    Title    = "Warning",
                    Buttons  = new[]
                    {
                        new SapButtonSnapshot
                        {
                            Id      = "wnd[1]/usr/btnOK",
                            Text    = "OK",
                            Tooltip = "Confirm",
                        },
                    },
                },
            },
        };

        var btn = ButtonFinder.Resolve("OK", snapshot);
        Assert.Equal("wnd[1]/usr/btnOK", btn.Id);
    }
}
