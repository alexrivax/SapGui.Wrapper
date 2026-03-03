namespace SapGui.Wrapper;

/// <summary>Wraps a SAP GUI push button (GuiButton).</summary>
public class GuiButton : GuiComponent
{
    internal GuiButton(object raw) : base(raw) { }

    // Text, Press, SetFocus are inherited from GuiComponent.

    /// <inheritdoc/>
    public override string ToString() => $"Button [{Id}] \"{Text}\"";
}
