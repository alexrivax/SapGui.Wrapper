namespace SapGui.Wrapper;

/// <summary>
/// Wraps a SAP GUI HTML viewer control (<c>GuiHTMLViewer</c>).
/// These controls embed an HTML page (or web application) inside a SAP
/// screen and are used for, e.g., web-based selection helps or BI content.
/// </summary>
public class GuiHTMLViewer : GuiComponent
{
    internal GuiHTMLViewer(object raw) : base(raw) { }

    /// <summary>
    /// The Win32 window handle of the embedded browser control.
    /// Useful for low-level UI automation via Win32 or UI Automation APIs
    /// when the SAP scripting layer alone is insufficient.
    /// </summary>
    public int BrowserHandle => GetInt("BrowserHandle");

    /// <summary>
    /// Fires a SAP event defined inside the embedded HTML page.
    /// HTML pages in GuiHTMLViewer communicate with ABAP by raising named
    /// SAP events via JavaScript; this method triggers the same mechanism
    /// from the scripting layer.
    /// </summary>
    /// <param name="eventName">The SAP event name to fire (e.g. <c>"BUTTON_CLICK"</c>).</param>
    /// <param name="param1">First optional parameter passed with the event.</param>
    /// <param name="param2">Second optional parameter passed with the event.</param>
    public void FireSapEvent(string eventName, string param1 = "", string param2 = "")
        => Invoke("SapEvent", eventName, param1, param2);

    /// <inheritdoc/>
    public override string ToString() => $"HtmlViewer [{Id}] BrowserHandle={BrowserHandle}";
}
