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

    /// <summary>The message text displayed in the popup.</summary>
    public override string Text     => GetString("Text");

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

    /// <summary>Title bar text of the popup window.</summary>
    public string Title             => GetString("Text");

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
}
