namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Extends <see cref="GuiSession"/> with the primary observation method for AI agents
/// and diagnostics: <see cref="ScanScreen"/>.
/// </summary>
public static class GuiSessionScanExtensions
{
    /// <summary>
    /// Returns a complete snapshot of the current SAP screen by walking the
    /// entire COM component tree.
    /// <para>
    /// This is the primary observation method for AI agents and diagnostics.
    /// The returned <see cref="SapScreenSnapshot"/> is fully immutable and
    /// can be safely passed between threads, serialised to JSON
    /// (<see cref="SapScreenSnapshot.ToJson"/>), or converted to a compact
    /// plain-text context block for LLM consumption
    /// (<see cref="SapScreenSnapshot.ToAgentContext"/>).
    /// </para>
    /// </summary>
    /// <param name="session">The active SAP GUI session to scan.</param>
    /// <param name="withScreenshot">
    /// When <see langword="true"/>, captures a HardCopy screenshot of the
    /// main window and embeds it as a base64 PNG string in
    /// <see cref="SapScreenSnapshot.ScreenshotBase64"/>.
    /// This adds approximately 300 ms.
    /// Use only when passing the image to a vision-capable LLM.
    /// </param>
    /// <returns>An immutable <see cref="SapScreenSnapshot"/>.</returns>
    /// <example>
    /// <code>
    /// // Basic scan — for text-only LLMs or diagnostics
    /// var snapshot = session.ScanScreen();
    /// Console.WriteLine(snapshot.ToAgentContext());
    ///
    /// // With screenshot — for vision-capable LLMs (e.g. GPT-4o, Claude 3)
    /// var snapshot = session.ScanScreen(withScreenshot: true);
    /// string base64 = snapshot.ScreenshotBase64!;
    /// </code>
    /// </example>
    public static SapScreenSnapshot ScanScreen(
        this GuiSession session,
        bool withScreenshot = false)
        => new SapScreenScanner(session).Scan(withScreenshot);
}
