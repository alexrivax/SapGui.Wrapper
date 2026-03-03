namespace SapGui.Wrapper;

/// <summary>
/// Wraps a SAP GUI text field (GuiTextField, GuiCTextField,
/// GuiPasswordField, GuiOkCodeField).
/// </summary>
public class GuiTextField : GuiComponent
{
    internal GuiTextField(object raw) : base(raw) { }

    // Text is inherited from GuiComponent; override only to add XML doc.
    /// <inheritdoc cref="GuiComponent.Text"/>
    public override string Text
    {
        get => GetString("Text");
        set => SetProperty("Text", value);
    }

    /// <summary>Maximum allowed input length.</summary>
    public int MaxLength => GetInt("MaxLength");

    /// <summary>Whether the field is read-only.</summary>
    public bool IsReadOnly => !Changeable;

    /// <summary>Selects all text in the field.</summary>
    public void CaretPosition(int pos) => SetProperty("CaretPosition", pos);

    /// <inheritdoc/>
    public override string ToString() => $"TextField [{Id}] = \"{Text}\"";
}
