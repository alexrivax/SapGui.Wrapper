namespace SapGui.Wrapper;

/// <summary>
/// Wraps the SAP GUI status bar (GuiStatusbar).
/// </summary>
public class GuiStatusbar : GuiComponent
{
    internal GuiStatusbar(object raw) : base(raw) { }

    /// <summary>The full status bar message text.</summary>
    public override string Text => GetString("Text");

    /// <summary>Message type: "S"=Success, "W"=Warning, "E"=Error, "A"=Abort, "I"=Info, ""=None.</summary>
    public string MessageType => GetString("MessageType");

    /// <summary>Returns <see langword="true"/> if the last message was a success notification (type S).</summary>
    public bool IsSuccess => MessageType == "S";

    /// <summary>Returns <see langword="true"/> if the last message was a warning (type W).</summary>
    public bool IsWarning => MessageType == "W";

    /// <summary>Returns <see langword="true"/> if the last message was an error or abort (type E or A).</summary>
    public bool IsError   => MessageType == "E" || MessageType == "A";

    /// <inheritdoc/>
    public override string ToString() => $"[{MessageType}] {Text}";
}
