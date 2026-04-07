namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Immutable snapshot of a single input field (text field, combo box, check box, radio button, label)
/// captured during a screen scan.
/// </summary>
public sealed class SapFieldSnapshot
{
    /// <summary>Full COM path, e.g. <c>wnd[0]/usr/txtS_WERKS-LOW</c>.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable label resolved from adjacent <c>GuiLabel</c> siblings.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Current field value.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// SAP field type string: <c>TextField</c>, <c>ComboBox</c>, <c>CheckBox</c>,
    /// <c>RadioButton</c>, <c>Label</c>, <c>PasswordField</c>.
    /// </summary>
    public string FieldType { get; init; } = string.Empty;

    /// <summary>Whether the field is read-only (not changeable).</summary>
    public bool IsReadOnly { get; init; }

    /// <summary>Whether the field is marked mandatory in the SAP screen.</summary>
    public bool IsRequired { get; init; }

    /// <summary>Whether the field is currently visible.</summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>Maximum allowed character length (0 = unknown).</summary>
    public int MaxLength { get; init; }

    /// <summary>Available options for <c>ComboBox</c> fields; empty for other types.</summary>
    public IReadOnlyList<string> ComboOptions { get; init; } = Array.Empty<string>();

    /// <inheritdoc/>
    public override string ToString() => $"{FieldType} \"{Label}\" = \"{Value}\"";
}
