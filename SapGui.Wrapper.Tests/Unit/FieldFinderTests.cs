using SapGui.Wrapper.Agent.Actions;
using SapGui.Wrapper.Agent.Observation;

namespace SapGui.Wrapper.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="FieldFinder"/>.
/// Pure .NET — no SAP connection required.
/// </summary>
public sealed class FieldFinderTests
{
    // ── Test fixture ──────────────────────────────────────────────────────────

    private static SapScreenSnapshot BuildSnapshot() => new()
    {
        Transaction = "MM60",
        Fields = new[]
        {
            new SapFieldSnapshot
            {
                Id        = "wnd[0]/usr/txtS_WERKS-LOW",
                Label     = "Plant",
                FieldType = "TextField",
                Value     = string.Empty,
            },
            new SapFieldSnapshot
            {
                Id        = "wnd[0]/usr/txtS_MATNR-LOW",
                Label     = "Material",
                FieldType = "TextField",
                Value     = string.Empty,
            },
            new SapFieldSnapshot
            {
                Id        = "wnd[0]/usr/cmbP_MTART",
                Label     = "Material Type",
                FieldType = "ComboBox",
                Value     = string.Empty,
                ComboOptions = new[] { "ROH", "HALB", "FERT" },
            },
            new SapFieldSnapshot
            {
                Id        = "wnd[0]/usr/chkP_ACTIVE",
                Label     = "Active",
                FieldType = "CheckBox",
                Value     = string.Empty,
            },
        },
    };

    // ── Exact ID passthrough ──────────────────────────────────────────────────

    [Fact]
    public void Resolve_ExactId_ReturnsField()
    {
        var snapshot = BuildSnapshot();
        var field = FieldFinder.Resolve("wnd[0]/usr/txtS_WERKS-LOW", snapshot);
        Assert.Equal("wnd[0]/usr/txtS_WERKS-LOW", field.Id);
    }

    // ── Exact label match ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("Plant")]
    [InlineData("Material")]
    [InlineData("Material Type")]
    [InlineData("Active")]
    public void Resolve_ExactLabel_ReturnsField(string label)
    {
        var field = FieldFinder.Resolve(label, BuildSnapshot());
        Assert.Equal(label, field.Label);
    }

    // ── Case-insensitive label match ──────────────────────────────────────────

    [Theory]
    [InlineData("plant", "Plant")]
    [InlineData("PLANT", "Plant")]
    [InlineData("material type", "Material Type")]
    [InlineData("ACTIVE", "Active")]
    public void Resolve_CaseInsensitiveLabel_ReturnsField(string input, string expectedLabel)
    {
        var field = FieldFinder.Resolve(input, BuildSnapshot());
        Assert.Equal(expectedLabel, field.Label);
    }

    // ── Fuzzy label match (Levenshtein ≤ 2) ──────────────────────────────────

    [Theory]
    [InlineData("Plnt", "Plant")]    // 1 deletion
    [InlineData("Pant", "Plant")]    // 1 deletion
    [InlineData("Plaant", "Plant")]    // 1 insertion
    [InlineData("Materil", "Material")] // 1 deletion
    public void Resolve_FuzzyLabel_ReturnsClosestField(string typo, string expectedLabel)
    {
        var field = FieldFinder.Resolve(typo, BuildSnapshot());
        Assert.Equal(expectedLabel, field.Label);
    }

    // ── ID suffix match ───────────────────────────────────────────────────────

    [Fact]
    public void Resolve_IdSuffix_ReturnsField()
    {
        var field = FieldFinder.Resolve("txtS_WERKS-LOW", BuildSnapshot());
        Assert.Equal("wnd[0]/usr/txtS_WERKS-LOW", field.Id);
    }

    // ── Not found → exception ─────────────────────────────────────────────────

    [Fact]
    public void Resolve_NoMatch_ThrowsResolutionException()
    {
        var ex = Assert.Throws<SapAgentResolutionException>(
            () => FieldFinder.Resolve("NonExistentFieldXYZ123", BuildSnapshot()));

        Assert.Equal("NonExistentFieldXYZ123", ex.Target);
        Assert.Equal("field", ex.ElementType);
        Assert.NotEmpty(ex.Candidates);
    }

    // ── Levenshtein helper ────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("a", "", 1)]
    [InlineData("", "a", 1)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("plant", "plant", 0)]
    [InlineData("Plant", "plant", 1)] // FieldFinder lowercases before calling
    public void Levenshtein_ReturnsCorrectDistance(string a, string b, int expected)
    {
        Assert.Equal(expected, FieldFinder.Levenshtein(a, b));
    }
}
