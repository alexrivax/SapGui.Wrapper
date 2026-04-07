using System.Text.RegularExpressions;

namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Maps SAP GUI input fields to their adjacent human-readable label text by
/// walking the COM component sibling list.
/// </summary>
internal static class LabelResolver
{
    // Regex: extract the ABAP field name portion from a COM path segment,
    // e.g. "txtS_WERKS-LOW" → "S_WERKS-LOW",  "ctxtKAEK-MATNR" → "KAEK-MATNR"
    private static readonly Regex _abapNamePattern =
        new(@"(?:txt|ctxt|chk|rad|cmb|lbl)[A-Z_][-\w]*$", RegexOptions.Compiled);

    /// <summary>
    /// Returns the best human-readable label for <paramref name="fieldId"/> by
    /// inspecting the sibling COM components inside <paramref name="container"/>.
    /// </summary>
    /// <param name="container">The direct COM container that owns the field (e.g. <c>wnd[0]/usr</c>).</param>
    /// <param name="field">The raw COM object of the field itself.</param>
    /// <param name="fieldId">The full COM ID of the field, used for ABAP-name fallback extraction.</param>
    /// <returns>A non-empty human-readable label string.</returns>
    public static string Resolve(object container, object field, string fieldId)
    {
        // Strategy 1: field's own Text is non-empty (self-labelled, e.g. a label element itself)
        try
        {
            var selfText = GetStringProp(field, "Text");
            if (!string.IsNullOrWhiteSpace(selfText))
                return selfText.TrimEnd(':').Trim();
        }
        catch { /* ignore */ }

        // Strategy 2: walk backward through siblings to find the nearest GuiLabel
        try
        {
            var children = container.GetType()
                                    .InvokeMember("Children",
                                                  BindingFlags.GetProperty,
                                                  null, container, null);
            if (children is not null)
            {
                var ct    = children.GetType();
                int count = Convert.ToInt32(ct.InvokeMember("Count",
                    BindingFlags.GetProperty, null, children, null) ?? 0);

                // Build a list of (type, text, id) tuples for all siblings
                var siblings = new List<(string type, string text, string id)>(count);
                for (int i = 0; i < count; i++)
                {
                    var child = ct.InvokeMember("Item",
                        BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                        null, children, new object[] { i });
                    if (child is null) continue;

                    var ctype = GetStringProp(child, "Type");
                    var ctext = GetStringProp(child, "Text");
                    var cid   = GetStringProp(child, "Id");
                    siblings.Add((ctype, ctext, cid));
                }

                // Find the index of our field in the sibling list
                int fieldIndex = siblings.FindIndex(s =>
                    s.id.Equals(fieldId, StringComparison.OrdinalIgnoreCase));

                if (fieldIndex > 0)
                {
                    // Walk backward from fieldIndex - 1 to find the nearest GuiLabel
                    for (int j = fieldIndex - 1; j >= 0; j--)
                    {
                        var (stype, stext, _) = siblings[j];
                        if (stype == "GuiLabel" && !string.IsNullOrWhiteSpace(stext))
                            return stext.TrimEnd(':').Trim();

                        // Stop looking backward at other input elements
                        if (IsInputType(stype))
                            break;
                    }
                }
            }
        }
        catch { /* ignore COM errors */ }

        // Strategy 3: extract ABAP field name from the COM ID path segment
        var lastSegment = fieldId.Contains('/')
            ? fieldId.Substring(fieldId.LastIndexOf('/') + 1)
            : fieldId;

        var abapMatch = _abapNamePattern.Match(lastSegment.ToUpperInvariant());
        if (!abapMatch.Success && lastSegment.Length > 3)
        {
            // Remove common type prefixes (txt, ctxt, chk, rad, cmb, lbl + first char)
            var stripped = Regex.Replace(lastSegment, @"^(txt|ctxt|chk|rad|cmb|lbl)", "",
                RegexOptions.IgnoreCase).Trim();
            if (!string.IsNullOrEmpty(stripped))
                return HumaniseName(stripped);
        }

        if (abapMatch.Success)
            return HumaniseName(abapMatch.Value);

        // Strategy 4: last-resort — component type + path-based suffix
        var typePart = GetStringProp(field, "Type");
        return $"{typePart}[{lastSegment}]";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsInputType(string type) => type switch
    {
        "GuiTextField" or "GuiCTextField" or "GuiPasswordField" or
        "GuiOkCodeField" or "GuiCheckBox" or "GuiRadioButton" or
        "GuiComboBox" => true,
        _ => false
    };

    private static string HumaniseName(string abapName)
    {
        // "S_WERKS-LOW" → "S WERKS LOW"  — strip leading "S_" range suffix
        var s = abapName
            .Replace('-', ' ')
            .Replace('_', ' ');

        // Remove trailing LOW / HIGH / FROM / TO (selection screen range suffixes)
        s = Regex.Replace(s, @"\s+(LOW|HIGH|FROM|TO)\s*$", "", RegexOptions.IgnoreCase).Trim();

        return System.Globalization.CultureInfo.InvariantCulture.TextInfo
            .ToTitleCase(s.ToLowerInvariant());
    }

    private static string GetStringProp(object obj, string prop)
    {
        try
        {
            return (string?)obj.GetType()
                               .InvokeMember(prop,
                                             BindingFlags.GetProperty,
                                             null, obj, null) ?? string.Empty;
        }
        catch { return string.Empty; }
    }
}
