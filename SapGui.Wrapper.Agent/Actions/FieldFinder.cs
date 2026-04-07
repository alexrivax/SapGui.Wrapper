using SapGui.Wrapper.Agent.Observation;

namespace SapGui.Wrapper.Agent.Actions;

/// <summary>
/// Resolves a semantic field target (label text, partial label, or full element ID) to
/// a <see cref="SapFieldSnapshot"/> from a screen snapshot.
/// <para>
/// Resolution order:
/// <list type="number">
///   <item>Exact element-ID match (used when the caller passes a COM path such as <c>wnd[0]/usr/txtS_WERKS-LOW</c>).</item>
///   <item>Exact label match (case-insensitive).</item>
///   <item>Partial ID suffix match (e.g. <c>txtS_WERKS-LOW</c>).</item>
///   <item>Fuzzy label match — Levenshtein distance ≤ 2.</item>
/// </list>
/// Throws <see cref="SapAgentResolutionException"/> when the target cannot be resolved.
/// </para>
/// </summary>
internal static class FieldFinder
{
    /// <summary>
    /// Resolves <paramref name="labelOrId"/> to a field in <paramref name="snapshot"/>.
    /// </summary>
    /// <param name="labelOrId">Label text, partial text, or full COM element ID.</param>
    /// <param name="snapshot">Snapshot of the current screen to search.</param>
    /// <returns>The best-matching <see cref="SapFieldSnapshot"/>.</returns>
    /// <exception cref="SapAgentResolutionException">
    /// Thrown when no matching field can be found.
    /// </exception>
    public static SapFieldSnapshot Resolve(string labelOrId, SapScreenSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(labelOrId))
            throw new ArgumentException("Field target must not be empty.", nameof(labelOrId));

        // 1. Exact element-ID match (COM path starts with "wnd[")
        if (labelOrId.StartsWith("wnd[", StringComparison.OrdinalIgnoreCase))
        {
            var byId = snapshot.Fields.FirstOrDefault(
                f => f.Id.Equals(labelOrId, StringComparison.OrdinalIgnoreCase));
            if (byId is not null) return byId;
        }

        // 2. Exact label match (case-insensitive)
        var exactLabel = snapshot.Fields.FirstOrDefault(
            f => f.Label.Equals(labelOrId, StringComparison.OrdinalIgnoreCase));
        if (exactLabel is not null) return exactLabel;

        // 3. Partial ID suffix match (e.g. caller passes "txtS_WERKS-LOW" or field name only)
        var bySuffix = snapshot.Fields.FirstOrDefault(
            f => f.Id.EndsWith(labelOrId, StringComparison.OrdinalIgnoreCase));
        if (bySuffix is not null) return bySuffix;

        // 4. Fuzzy label match — Levenshtein distance ≤ 2
        SapFieldSnapshot? best = null;
        int bestDist = int.MaxValue;
        var lowerTarget = labelOrId.ToLowerInvariant();

        foreach (var field in snapshot.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Label)) continue;
            int dist = Levenshtein(lowerTarget, field.Label.ToLowerInvariant());
            if (dist < bestDist)
            {
                bestDist = dist;
                best = field;
            }
        }

        if (best is not null && bestDist <= 2) return best;

        throw new SapAgentResolutionException(
            labelOrId,
            "field",
            snapshot.Fields
                    .Select(f => string.IsNullOrWhiteSpace(f.Label) ? f.Id : f.Label)
                    .Distinct()
                    .ToArray());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Computes the Levenshtein edit distance between two strings.</summary>
    internal static int Levenshtein(string a, string b)
    {
        int la = a.Length, lb = b.Length;
        if (la == 0) return lb;
        if (lb == 0) return la;

        var dp = new int[la + 1, lb + 1];
        for (int i = 0; i <= la; i++) dp[i, 0] = i;
        for (int j = 0; j <= lb; j++) dp[0, j] = j;

        for (int i = 1; i <= la; i++)
        {
            for (int j = 1; j <= lb; j++)
            {
                dp[i, j] = a[i - 1] == b[j - 1]
                    ? dp[i - 1, j - 1]
                    : 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));
            }
        }

        return dp[la, lb];
    }
}
