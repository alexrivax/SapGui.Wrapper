using SapGui.Wrapper.Agent.Observation;

namespace SapGui.Wrapper.Agent.Actions;

/// <summary>
/// Resolves a semantic button target (button text, tooltip, function code, or full element ID)
/// to a <see cref="SapButtonSnapshot"/> from a screen snapshot.
/// <para>
/// Resolution order:
/// <list type="number">
///   <item>Exact element-ID match.</item>
///   <item>Exact text match (case-insensitive).</item>
///   <item>Exact tooltip match (case-insensitive).</item>
///   <item>Exact function code match (case-insensitive).</item>
///   <item>Contains match on text or tooltip (case-insensitive).</item>
/// </list>
/// Throws <see cref="SapAgentResolutionException"/> when no matching button is found.
/// </para>
/// </summary>
internal static class ButtonFinder
{
    /// <summary>
    /// Resolves <paramref name="textOrId"/> to the best matching button in <paramref name="snapshot"/>.
    /// </summary>
    /// <param name="textOrId">Button text, tooltip, function code, or full COM element ID.</param>
    /// <param name="snapshot">Snapshot of the current screen.</param>
    /// <returns>The best-matching <see cref="SapButtonSnapshot"/>.</returns>
    /// <exception cref="SapAgentResolutionException">Thrown when no button matches.</exception>
    public static SapButtonSnapshot Resolve(string textOrId, SapScreenSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(textOrId))
            throw new ArgumentException("Button target must not be empty.", nameof(textOrId));

        // Also search popup buttons so agents can resolve buttons on active dialogs
        var allButtons = snapshot.Buttons
            .Concat(snapshot.Popups.SelectMany(p => p.Buttons))
            .ToList();

        // 1. Exact element-ID match
        if (textOrId.StartsWith("wnd[", StringComparison.OrdinalIgnoreCase))
        {
            var byId = allButtons.FirstOrDefault(
                b => b.Id.Equals(textOrId, StringComparison.OrdinalIgnoreCase));
            if (byId is not null) return byId;
        }

        // 2. Exact text match
        var exactText = allButtons.FirstOrDefault(
            b => b.Text.Equals(textOrId, StringComparison.OrdinalIgnoreCase));
        if (exactText is not null) return exactText;

        // 3. Exact tooltip match
        var exactTooltip = allButtons.FirstOrDefault(
            b => b.Tooltip.Equals(textOrId, StringComparison.OrdinalIgnoreCase));
        if (exactTooltip is not null) return exactTooltip;

        // 4. Exact function code match
        var byFunctionCode = allButtons.FirstOrDefault(
            b => b.FunctionCode.Equals(textOrId, StringComparison.OrdinalIgnoreCase));
        if (byFunctionCode is not null) return byFunctionCode;

        // 5. Contains match on text or tooltip
        var partial = allButtons.FirstOrDefault(b =>
            b.Text.Contains(textOrId, StringComparison.OrdinalIgnoreCase) ||
            b.Tooltip.Contains(textOrId, StringComparison.OrdinalIgnoreCase));
        if (partial is not null) return partial;

        throw new SapAgentResolutionException(
            textOrId,
            "button",
            allButtons
                .Select(b => string.IsNullOrWhiteSpace(b.Text) ? b.Tooltip : b.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToArray());
    }
}
