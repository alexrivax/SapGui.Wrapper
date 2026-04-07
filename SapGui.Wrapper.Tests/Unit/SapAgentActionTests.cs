using SapGui.Wrapper.Agent.Actions;
using SapGui.Wrapper.Agent.Observation;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SapActionResult"/>, <see cref="SapAction"/>,
/// and the agent exceptions.
/// Pure .NET — no SAP connection required.
/// </summary>
public sealed class SapAgentActionTests
{
    // ── SapActionResult.Ok ────────────────────────────────────────────────────

    [Fact]
    public void ActionResult_Ok_IsSuccess()
    {
        var before = new SapScreenSnapshot { Transaction = "MM60" };
        var after = new SapScreenSnapshot { Transaction = "MM60" };

        var result = SapActionResult.Ok(before, after, "wnd[0]/usr/txtS_WERKS-LOW");

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Same(before, result.SnapshotBefore);
        Assert.Same(after, result.SnapshotAfter);
        Assert.Equal("wnd[0]/usr/txtS_WERKS-LOW", result.ResolvedId);
    }

    // ── SapActionResult.OkReadOnly ────────────────────────────────────────────

    [Fact]
    public void ActionResult_OkReadOnly_HasNoAfterSnapshot()
    {
        var before = new SapScreenSnapshot { Transaction = "MM60" };

        var result = SapActionResult.OkReadOnly(before, "wnd[0]/usr/txtS_MATNR-LOW");

        Assert.True(result.Success);
        Assert.Null(result.SnapshotAfter);
        Assert.Same(before, result.SnapshotBefore);
    }

    // ── SapActionResult.Fail ──────────────────────────────────────────────────

    [Fact]
    public void ActionResult_Fail_IsNotSuccess()
    {
        var result = SapActionResult.Fail("Field 'Plant' not found.");

        Assert.False(result.Success);
        Assert.Equal("Field 'Plant' not found.", result.ErrorMessage);
        Assert.Null(result.SnapshotAfter);
    }

    [Fact]
    public void ActionResult_Fail_WithBefore_PreservesSnapshot()
    {
        var before = new SapScreenSnapshot { Transaction = "ME21N" };
        var result = SapActionResult.Fail("error", before);

        Assert.False(result.Success);
        Assert.Same(before, result.SnapshotBefore);
    }

    // ── SapAction ─────────────────────────────────────────────────────────────

    [Fact]
    public void SapAction_ToString_UsesDescription_WhenSet()
    {
        var action = new SapAction
        {
            ActionType = SapActionType.SetField,
            Target = "Plant",
            Value = "1000",
            Description = "Set Plant to Hamburg",
        };

        Assert.Equal("Set Plant to Hamburg", action.ToString());
    }

    [Fact]
    public void SapAction_ToString_AutoGenerates_WhenDescriptionIsNull()
    {
        var action = new SapAction
        {
            ActionType = SapActionType.ClickButton,
            Target = "Execute",
        };

        var str = action.ToString();
        Assert.Contains("ClickButton", str);
        Assert.Contains("Execute", str);
    }

    // ── SapAgentResolutionException ───────────────────────────────────────────

    [Fact]
    public void ResolutionException_ContainsTarget()
    {
        var ex = new SapAgentResolutionException("Plant", "field", new[] { "Material", "Vendor" });

        Assert.Equal("Plant", ex.Target);
        Assert.Equal("field", ex.ElementType);
        Assert.Contains("Material", ex.Candidates);
        Assert.Contains("Vendor", ex.Candidates);
        Assert.Contains("Plant", ex.Message);
    }

    [Fact]
    public void ResolutionException_NoCandidates_StillCreates()
    {
        var ex = new SapAgentResolutionException("Mystery", "button");

        Assert.Equal("Mystery", ex.Target);
        Assert.Empty(ex.Candidates);
    }

    // ── SapAgentBlockedException ──────────────────────────────────────────────

    [Fact]
    public void BlockedException_ContainsReason()
    {
        var ex = new SapAgentBlockedException("ReadOnlyMode is enabled");

        Assert.Equal("ReadOnlyMode is enabled", ex.Reason);
        Assert.Contains("ReadOnlyMode is enabled", ex.Message);
    }

    // ── SapKeyAction enum completeness ────────────────────────────────────────

    [Theory]
    [InlineData(SapKeyAction.Enter)]
    [InlineData(SapKeyAction.Back)]
    [InlineData(SapKeyAction.Execute)]
    [InlineData(SapKeyAction.Save)]
    [InlineData(SapKeyAction.Cancel)]
    [InlineData(SapKeyAction.Exit)]
    [InlineData(SapKeyAction.F4)]
    [InlineData(SapKeyAction.ScrollTop)]
    [InlineData(SapKeyAction.ScrollBottom)]
    [InlineData(SapKeyAction.CtrlHome)]
    [InlineData(SapKeyAction.CtrlEnd)]
    public void SapKeyAction_IsDefined(SapKeyAction key)
    {
        Assert.True(Enum.IsDefined(key));
    }

    // ── PopupAction enum completeness ─────────────────────────────────────────

    [Theory]
    [InlineData(PopupAction.Ok)]
    [InlineData(PopupAction.Cancel)]
    [InlineData(PopupAction.Yes)]
    [InlineData(PopupAction.No)]
    [InlineData(PopupAction.ByButtonText)]
    public void PopupAction_IsDefined(PopupAction action)
    {
        Assert.True(Enum.IsDefined(action));
    }
}
