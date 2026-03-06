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

    /// <summary>
    /// The formatted display value of the field as shown to the user.
    /// May differ from <see cref="GuiComponent.Text"/> on amount or date fields
    /// where SAP applies locale-specific formatting.
    /// </summary>
    public string DisplayedText => GetString("DisplayedText");

    /// <summary>
    /// Returns <see langword="true"/> if the field is mandatory
    /// (marked with a <c>?</c> in SAP GUI).
    /// </summary>
    public bool IsRequired => GetBool("Required");

    /// <summary>
    /// Returns <see langword="true"/> if the field is an output-only field
    /// (type <c>O</c> in the ABAP Dictionary screen field definition).
    /// </summary>
    public bool IsOField => GetBool("IsOField");

    /// <summary>Sets the caret position within the field.</summary>
    public void CaretPosition(int pos) => SetProperty("CaretPosition", pos);

    /// <inheritdoc/>
    public override string ToString() => $"TextField [{Id}] = \"{Text}\"";
}
