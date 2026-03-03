using SapGui.Wrapper.Tests.Helpers;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Tests for <see cref="GuiSessionInfo"/> property mapping.
/// Uses a <see cref="FakeComObject"/> in place of the real SAP COM Info object.
/// Pure .NET – no SAP required.
/// </summary>
public sealed class GuiSessionInfoTests
{
    private static GuiSessionInfo BuildInfo(Action<FakeComObject>? configure = null)
    {
        var fake = new FakeComObject();
        configure?.Invoke(fake);
        return new GuiSessionInfo(fake);
    }

    [Fact]
    public void SystemName_ReturnsFakeValue()
    {
        var info = BuildInfo(f => f.SystemName = "GP1");
        Assert.Equal("GP1", info.SystemName);
    }

    [Fact]
    public void Client_ReturnsFakeValue()
    {
        var info = BuildInfo(f => f.Client = "450");
        Assert.Equal("450", info.Client);
    }

    [Fact]
    public void User_ReturnsFakeValue()
    {
        var info = BuildInfo(f => f.User = "ALRIV");
        Assert.Equal("ALRIV", info.User);
    }

    [Fact]
    public void Language_ReturnsFakeValue()
    {
        var info = BuildInfo(f => f.Language = "EN");
        Assert.Equal("EN", info.Language);
    }

    [Fact]
    public void Transaction_ReturnsFakeValue()
    {
        var info = BuildInfo(f => f.Transaction = "CAT2");
        Assert.Equal("CAT2", info.Transaction);
    }

    [Fact]
    public void Program_ReturnsFakeValue()
    {
        var info = BuildInfo(f => f.Program = "RCATSTAL");
        Assert.Equal("RCATSTAL", info.Program);
    }

    [Fact]
    public void ScreenNumber_ReturnsFakeValue()
    {
        var info = BuildInfo(f => f.ScreenNumber = "0200");
        Assert.Equal("0200", info.ScreenNumber);
    }

    [Fact]
    public void ApplicationServer_ReturnsFakeValue()
    {
        var info = BuildInfo(f => f.ApplicationServer = "sapserver.corp.local");
        Assert.Equal("sapserver.corp.local", info.ApplicationServer);
    }

    [Fact]
    public void ToString_ContainsKeyFields()
    {
        var info = BuildInfo(f =>
        {
            f.SystemName  = "TST";
            f.Client      = "800";
            f.User        = "DEVELOPER";
            f.Transaction = "SE80";
        });

        var str = info.ToString();
        Assert.Contains("TST",       str);
        Assert.Contains("800",       str);
        Assert.Contains("DEVELOPER", str);
        Assert.Contains("SE80",      str);
    }
}
