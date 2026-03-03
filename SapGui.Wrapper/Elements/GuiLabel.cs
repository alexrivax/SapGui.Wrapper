namespace SapGui.Wrapper;

/// <summary>Wraps a SAP GUI label / display field (GuiLabel).</summary>
public class GuiLabel : GuiComponent
{
    internal GuiLabel(object raw) : base(raw) { }

    // Text is inherited from GuiComponent (read access works; write is a no-op on labels).

    /// <inheritdoc/>
    public override string ToString() => $"Label [{Id}] \"{Text}\"";
}
