namespace SapGui.Wrapper;

/// <summary>Wraps a SAP GUI check box (GuiCheckBox).</summary>
public class GuiCheckBox : GuiComponent
{
    internal GuiCheckBox(object raw) : base(raw) { }

    /// <summary>Gets or sets the checked state.</summary>
    public bool Selected
    {
        get => GetBool("Selected");
        set => SetProperty("Selected", value);
    }

    /// <summary>Label text next to the check box.</summary>
    public override string Text => GetString("Text");

    /// <inheritdoc/>
    public override void SetFocus() => Invoke("SetFocus");

    /// <inheritdoc/>
    public override string ToString() => $"CheckBox [{Id}] Selected={Selected}";
}
