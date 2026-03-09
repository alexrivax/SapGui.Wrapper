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

    /// <summary>Returns <see langword="true"/> if the combo box does not allow user input.</summary>
    public bool IsReadOnly => !Changeable;

    /// <summary>
    /// Returns <see langword="true"/> if the combo box displays the key rather than
    /// the description text in its visible area.
    /// </summary>
    public bool ShowKey => GetBool("ShowKey");

    /// <inheritdoc/>
    public override string ToString() => $"ComboBox [{Id}] Key=\"{Key}\" Value=\"{Value}\"";
}
