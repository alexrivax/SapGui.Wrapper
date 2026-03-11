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

    /// <summary>
    /// Returns all entries in the combo box as (Key, Value) pairs.
    /// </summary>
    public IReadOnlyList<(string Key, string Value)> Entries
    {
        get
        {
            var entries = RawObject.GetType()
                                   .InvokeMember("Entries",
                                                 BindingFlags.GetProperty,
                                                 null, RawObject, null);
            if (entries is null) return Array.Empty<(string, string)>();

            var et    = entries.GetType();
            int count = (int)(et.InvokeMember("Count",
                                              BindingFlags.GetProperty,
                                              null, entries, null) ?? 0);

            var result = new List<(string Key, string Value)>(count);
            for (int i = 0; i < count; i++)
            {
                var entry = et.InvokeMember("Item",
                                            BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                                            null, entries,
                                            new object[] { i });
                if (entry is null) continue;
                var etype = entry.GetType();
                var key   = (string?)etype.InvokeMember("Key",   BindingFlags.GetProperty, null, entry, null) ?? string.Empty;
                var value = (string?)etype.InvokeMember("Value", BindingFlags.GetProperty, null, entry, null) ?? string.Empty;
                result.Add((key, value));
            }
            return result;
        }
    }

    /// <inheritdoc/>
    public override string ToString() => $"ComboBox [{Id}] Key=\"{Key}\" Value=\"{Value}\"";
}
