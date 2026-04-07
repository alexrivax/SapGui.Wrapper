using SapGui.Wrapper.Mcp;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Verifies the MCP server guardrail rules defined in <see cref="McpServerConfiguration"/>.
/// No SAP COM environment required.
/// </summary>
public sealed class McpGuardrailTests
{
    // ── Blocked transactions ───────────────────────────────────────────────────

    [Theory]
    [InlineData("SE38")]
    [InlineData("se38")]          // case-insensitive
    [InlineData("SM49")]
    [InlineData("SU01")]
    [InlineData("RZ10")]
    public void ApplyGuardrails_BlockedTransaction_Throws(string tCode)
    {
        var config = new McpServerConfiguration();

        Assert.Throws<SapAgentBlockedException>(
            () => McpServerConfiguration.ApplyGuardrails(tCode, isMutating: true, config));
    }

    [Theory]
    [InlineData("MM60")]
    [InlineData("VA01")]
    [InlineData("ME21N")]
    [InlineData("FB50")]
    public void ApplyGuardrails_AllowedTransaction_DoesNotThrow(string tCode)
    {
        var config = new McpServerConfiguration();

        var ex = Record.Exception(
            () => McpServerConfiguration.ApplyGuardrails(tCode, isMutating: true, config));

        Assert.Null(ex);
    }

    // ── Read-only mode ─────────────────────────────────────────────────────────

    [Fact]
    public void ApplyGuardrails_ReadOnlyMode_BlocksMutatingTool()
    {
        var config = new McpServerConfiguration { ReadOnlyMode = true };

        Assert.Throws<SapAgentBlockedException>(
            () => McpServerConfiguration.ApplyGuardrails(null, isMutating: true, config));
    }

    [Fact]
    public void ApplyGuardrails_ReadOnlyMode_AllowsReadTool()
    {
        var config = new McpServerConfiguration { ReadOnlyMode = true };

        // sap_scan_screen, sap_get_field, sap_read_grid are read-only (isMutating=false)
        var ex = Record.Exception(
            () => McpServerConfiguration.ApplyGuardrails(null, isMutating: false, config));

        Assert.Null(ex);
    }

    [Fact]
    public void ApplyGuardrails_ReadOnlyMode_ReadToolWithBlockedTcode_DoesNotThrow()
    {
        // A read-only scan on a blocked tCode should still be allowed
        // (the tCode parameter is only checked for sap_start_transaction).
        var config = new McpServerConfiguration { ReadOnlyMode = true };

        var ex = Record.Exception(
            () => McpServerConfiguration.ApplyGuardrails("SE38", isMutating: false, config));

        Assert.Null(ex);
    }

    // ── Custom block list ──────────────────────────────────────────────────────

    [Fact]
    public void ApplyGuardrails_CustomBlockedTransaction_Throws()
    {
        var config = new McpServerConfiguration
        {
            BlockedTransactions = ["MM60", "VA01"],
        };

        Assert.Throws<SapAgentBlockedException>(
            () => McpServerConfiguration.ApplyGuardrails("MM60", isMutating: true, config));
    }

    [Fact]
    public void ApplyGuardrails_EmptyBlockList_AllowsAllTransactions()
    {
        var config = new McpServerConfiguration
        {
            BlockedTransactions = [],
        };

        var ex = Record.Exception(
            () => McpServerConfiguration.ApplyGuardrails("SE38", isMutating: true, config));

        Assert.Null(ex);
    }
}
