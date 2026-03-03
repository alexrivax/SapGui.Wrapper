namespace SapGui.Wrapper;

/// <summary>Wraps a SAP GUI radio button (GuiRadioButton).</summary>
public class GuiRadioButton : GuiComponent
{
    internal GuiRadioButton(object raw) : base(raw) { }

    /// <summary>Gets or sets the selected state.</summary>
    public bool Selected
    {
        get => GetBool("Selected");
        set => SetProperty("Selected", value);
    }

    /// <summary>Label text.</summary>
    public override string Text => GetString("Text");

    /// <inheritdoc/>
    public override void SetFocus() => Invoke("SetFocus");

    /// <inheritdoc/>
    public override string ToString() => $"RadioButton [{Id}] Selected={Selected}";
}
