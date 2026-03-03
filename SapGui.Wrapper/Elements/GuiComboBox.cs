namespace SapGui.Wrapper;

/// <summary>Wraps a SAP GUI drop-down/combo-box (GuiComboBox).</summary>
public class GuiComboBox : GuiComponent
{
    internal GuiComboBox(object raw) : base(raw) { }

    /// <summary>Gets or sets the selected key (not the display text).</summary>
    public string Key
    {
        get => GetString("Key");
        set => SetProperty("Key", value);
    }

    /// <summary>The displayed text of the currently selected entry.</summary>
    public string Value => GetString("Value");

    /// <summary>Whether the combo box is read-only.</summary>
    /// <summary>Returns <see langword="true"/> if the combo box does not allow user input.</summary>
    public bool IsReadOnly => !Changeable;

    /// <inheritdoc/>
    public override string ToString() => $"ComboBox [{Id}] Key=\"{Key}\" Value=\"{Value}\"";
}
