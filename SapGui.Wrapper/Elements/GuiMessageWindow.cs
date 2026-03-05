namespace SapGui.Wrapper;

/// <summary>
/// Wraps a SAP GUI modal message / popup dialog (GuiMessageWindow).
/// These appear when SAP shows a system message that requires user confirmation,
/// e.g. "Document saved", "Are you sure?", error dialogs.
/// </summary>
public class GuiMessageWindow : GuiComponent
{
    internal GuiMessageWindow(object raw) : base(raw) { }

    // ── Content ───────────────────────────────────────────────────────────────

    /// <summary>
    /// The message body text displayed inside the popup.
    /// Reads SAP's inner text fields (<c>usr/txtMESSTXT1</c> … <c>usr/txtMESSTXT4</c>)
    /// and joins non-empty lines with a space.
    /// Falls back to the window title text if no inner fields are found
    /// (e.g. when the popup is a <c>GuiModalWindow</c> without standard message fields).
    /// </summary>
    public override string Text
    {
        get
        {
            var lines = new List<string>();
            for (int i = 1; i <= 4; i++)
            {
                var line = FindChildText($"usr/txtMESSTXT{i}");
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line.Trim());
            }
            return lines.Count > 0 ? string.Join(" ", lines) : GetString("Text");
        }
    }

    /// <summary>Title bar text of the popup window.</summary>
    public string Title             => GetString("Text");

    /// <summary>
    /// Message type: "S"=Success, "W"=Warning, "E"=Error, "A"=Abort, "I"=Info.
    /// </summary>
    public string MessageType       => GetString("MessageType");

    /// <summary>Returns <see langword="true"/> if the message type is Success (S).</summary>
    public bool IsSuccess           => MessageType == "S";

    /// <summary>Returns <see langword="true"/> if the message type is Warning (W).</summary>
    public bool IsWarning           => MessageType == "W";

    /// <summary>Returns <see langword="true"/> if the message type is Error (E) or Abort (A).</summary>
    public bool IsError             => MessageType is "E" or "A";

    /// <summary>Returns <see langword="true"/> if the message type is Information (I).</summary>
    public bool IsInfo              => MessageType == "I";

    // ── Standard button actions ───────────────────────────────────────────────

    /// <summary>
    /// Clicks the "OK" / "Continue" / "Enter" button (VKey 0).
    /// Use this to confirm or dismiss most message popups.
    /// </summary>
    public void ClickOk()       => SendVKey(0);

    /// <summary>Clicks "Cancel" / "No" (VKey 12).</summary>
    public void ClickCancel()   => SendVKey(12);

    /// <summary>Sends an arbitrary virtual key to the popup window.</summary>
    public void SendVKey(int vKey) => Invoke("SendVKey", vKey);

    // ── Button discovery ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns typed <see cref="GuiButton"/> wrappers for all buttons in the popup.
    /// Useful when the popup has non-standard buttons (Yes/No, etc.).
    /// </summary>
    public IReadOnlyList<GuiButton> GetButtons()
    {
        var children = Invoke("Children");
        if (children is null) return Array.Empty<GuiButton>();

        var ct    = children.GetType();
        int count = (int)(ct.InvokeMember("Count",
            BindingFlags.GetProperty, null, children, null) ?? 0);

        var result = new List<GuiButton>();
        for (int i = 0; i < count; i++)
        {
            var raw = ct.InvokeMember("Item",
                BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                null, children, new object[] { i });
            if (raw is null) continue;

            var typeName = (string?)raw.GetType().InvokeMember("Type",
                BindingFlags.GetProperty, null, raw, null) ?? string.Empty;

            if (typeName == "GuiButton")
                result.Add(new GuiButton(raw));
        }
        return result;
    }

    /// <summary>
    /// Clicks the first button whose text contains <paramref name="textFragment"/>
    /// (case-insensitive). Useful for "Yes" / "No" / "OK" / "Cancel" buttons.
    /// </summary>
    public void ClickButton(string textFragment)
    {
        var btn = GetButtons().FirstOrDefault(b =>
            b.Text.IndexOf(textFragment, StringComparison.OrdinalIgnoreCase) >= 0);

        if (btn is null)
            throw new InvalidOperationException(
                $"No button with text containing '{textFragment}' found in popup.");

        btn.Press();
    }

    /// <inheritdoc/>
    public override string ToString() => $"[{MessageType}] {Text}";

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Invokes <c>FindById</c> on the window COM object using a relative path,
    /// then reads the child's <c>Text</c> property.
    /// Returns <see cref="string.Empty"/> if the child does not exist.
    /// </summary>
    private string FindChildText(string relativePath)
    {
        try
        {
            var child = RawObject.GetType()
                                 .InvokeMember("FindById",
                                               BindingFlags.InvokeMethod,
                                               null, RawObject,
                                               new object[] { relativePath });
            if (child is null) return string.Empty;

            return (string?)child.GetType()
                                 .InvokeMember("Text",
                                               BindingFlags.GetProperty,
                                               null, child, null) ?? string.Empty;
        }
        catch { return string.Empty; }
    }
}
