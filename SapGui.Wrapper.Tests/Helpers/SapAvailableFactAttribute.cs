namespace SapGui.Wrapper.Tests.Helpers;

/// <summary>
/// xUnit <c>[Fact]</c> attribute that auto-skips the test when
/// SAP GUI is not running on the current machine.
///
/// Usage:
/// <code>
/// [SapAvailableFact]
/// public void My_LiveTest()
/// {
///     using var sap = SapGuiClient.Attach();
///     // …
/// }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SapAvailableFactAttribute : FactAttribute
{
    private const string SkipMessage =
        "SAP GUI is not running on this machine. " +
        "Start SAP GUI, log on, and enable scripting (Alt+F12 → Scripting) to run live tests.";

    public SapAvailableFactAttribute()
    {
        if (!IsSapRunning())
            Skip = SkipMessage;
    }

    private static bool IsSapRunning()
    {
        try
        {
            using var client = SapGuiClient.Attach();
            _ = client.Session; // ensure at least one session is available
            return true;
        }
        catch
        {
            return false;
        }
    }
}
