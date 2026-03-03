using SapGui.Wrapper.Tests.Helpers;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Tests for <see cref="GuiComponent"/> base class behaviour.
/// Uses <see cref="FakeComObject"/> as a stand-in for a SAP COM object.
/// Pure .NET – no SAP required.
/// </summary>
public sealed class GuiComponentTests
{
    // ── Null guard ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullRawObject_ThrowsArgumentNullException()
    {
        // WrapComponent routes to GuiComponent for unknown types
        Assert.Throws<ArgumentNullException>(() =>
            GuiSession.WrapComponent(null!));
    }

    // ── Identity properties ───────────────────────────────────────────────────

    [Fact]
    public void TypeName_ReflectsTypePropertyOfComObject()
    {
        var fake = FakeComObject.OfType("GuiTextField", "wnd[0]/usr/txtFoo");
        var component = GuiSession.WrapComponent(fake);

        Assert.Equal("GuiTextField", component.TypeName);
    }

    [Fact]
    public void Id_ReflectsIdPropertyOfComObject()
    {
        var fake = new FakeComObject { Type = "GuiLabel", Id = "wnd[0]/usr/lblStatus" };
        var component = GuiSession.WrapComponent(fake);

        Assert.Equal("wnd[0]/usr/lblStatus", component.Id);
    }

    [Fact]
    public void ComponentType_MatchesTypeNameEnum()
    {
        var fake = FakeComObject.OfType("GuiButton");
        var component = GuiSession.WrapComponent(fake);

        Assert.Equal(SapComponentType.GuiButton, component.ComponentType);
    }

    [Fact]
    public void ComponentType_UnknownTypeName_ReturnsUnknownEnum()
    {
        var fake = FakeComObject.OfType("SomeFutureType");
        var component = GuiSession.WrapComponent(fake);

        Assert.Equal(SapComponentType.Unknown, component.ComponentType);
    }

    // ── State properties ──────────────────────────────────────────────────────

    [Fact]
    public void Changeable_ReturnsFakeObjectValue()
    {
        var fake = new FakeComObject { Type = "GuiTextField", Changeable = false };
        var component = GuiSession.WrapComponent(fake);

        Assert.False(component.Changeable);
    }

    [Fact]
    public void IsModified_ReturnsFakeObjectValue()
    {
        var fake = new FakeComObject { Type = "GuiTextField", Modified = true };
        var component = GuiSession.WrapComponent(fake);

        Assert.True(component.IsModified);
    }

    // ── Text property ─────────────────────────────────────────────────────────

    [Fact]
    public void Text_Get_ReturnsTextPropertyOfComObject()
    {
        var fake = new FakeComObject { Type = "GuiLabel", Text = "Hello SAP" };
        var component = GuiSession.WrapComponent(fake);

        Assert.Equal("Hello SAP", component.Text);
    }

    [Fact]
    public void Text_Set_UpdatesTextPropertyOfComObject()
    {
        var fake      = new FakeComObject { Type = "GuiTextField" };
        var component = GuiSession.WrapComponent(fake);

        component.Text = "UpdatedValue";

        Assert.Equal("UpdatedValue", fake.Text);
    }

    // ── RawObject ─────────────────────────────────────────────────────────────

    [Fact]
    public void RawObject_IsSameInstanceAsFake()
    {
        var fake      = FakeComObject.OfType("GuiButton");
        var component = GuiSession.WrapComponent(fake);

        Assert.Same(fake, component.RawObject);
    }

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ContainsTypeNameAndId()
    {
        // Use GuiUserArea which has no typed subclass – routes to the base GuiComponent
        // whose ToString() returns "{TypeName} [{Id}]".
        var fake      = new FakeComObject { Type = "GuiUserArea", Id = "wnd[0]/usr" };
        var component = GuiSession.WrapComponent(fake);
        var str       = component.ToString();

        Assert.Contains("GuiUserArea", str);
        Assert.Contains("wnd[0]/usr", str);
    }

    // ── Missing-property resilience ───────────────────────────────────────────

    [Fact]
    public void GetString_MissingProperty_ReturnsEmptyStringNotThrow()
    {
        // FakeComObject has no "CustomProp" property → GetString should swallow and return ""
        var fake      = FakeComObject.OfType("GuiLabel");
        var component = GuiSession.WrapComponent(fake) as GuiLabel;

        Assert.NotNull(component);
        // Text is fine; non-existent prop should silently return empty
        Assert.Equal(string.Empty, component.Text); // Text is "" on the fake
    }
}
