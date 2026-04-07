using SapGui.Wrapper.Agent.Actions;

namespace SapGui.Wrapper.Agent.Actions;

/// <summary>
/// Extends <see cref="GuiSession"/> with the semantic agent façade.
/// </summary>
public static class GuiSessionAgentExtensions
{
    /// <summary>
    /// Returns a <see cref="SapAgentSession"/> wrapping this session.
    /// <para>
    /// The agent session provides semantic operations — set field by label,
    /// click button by text, navigate tabs, handle popups — without needing
    /// to know SAP element IDs.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// using var client = SapGuiClient.Attach();
    /// var agent = client.Session.Agent();
    ///
    /// agent.StartTransaction("MM60");
    /// agent.SetField("Plant", "1000");
    /// agent.PressKey(SapKeyAction.Execute);
    ///
    /// var result = agent.ReadGrid();
    /// Console.WriteLine(result.SnapshotAfter?.ToAgentContext());
    /// </code>
    /// </example>
    public static SapAgentSession Agent(this GuiSession session) =>
        new SapAgentSession(session);
}
